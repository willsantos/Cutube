using Cutube.Worker.Configuration;
using Cutube.Worker.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cutube.Worker.Handlers;

/// <summary>
/// Handler responsável por executar operações com retry e backoff exponencial.
/// </summary>
public class RetryHandler
{
    private readonly RetryPolicyOptions _options;
    private readonly ILogger<RetryHandler> _logger;

    public RetryHandler(
        IOptions<RetryPolicyOptions> options,
        ILogger<RetryHandler> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// Executa uma operação com retry automático para exceções transitórias.
    /// </summary>
    public async Task<T> ExecuteAsync<T>(
        string operationName,
        Func<Task<T>> operation,
        CancellationToken cancellationToken = default)
    {
        var lastException = new Exception("Unknown error");

        for (int attempt = 1; attempt <= _options.MaxRetries; attempt++)
        {
            try
            {
                _logger.LogDebug(
                    "Executing {OperationName}, attempt {Attempt}/{MaxRetries}",
                    operationName, attempt, _options.MaxRetries);

                var result = await operation();

                if (attempt > 1)
                {
                    _logger.LogInformation(
                        "{OperationName} succeeded on attempt {Attempt}",
                        operationName, attempt);
                }

                return result;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (TransientException ex)
            {
                lastException = ex;

                if (attempt < _options.MaxRetries)
                {
                    var delay = _options.GetDelayForAttempt(attempt);
                    _logger.LogWarning(
                        ex,
                        "{OperationName} failed with transient error on attempt {Attempt}. " +
                        "Retrying in {DelaySeconds}s...",
                        operationName, attempt, delay.TotalSeconds);

                    await Task.Delay(delay, cancellationToken);
                }
                else
                {
                    _logger.LogError(
                        ex,
                        "{OperationName} failed after {MaxRetries} attempts. " +
                        "Max retries exhausted.",
                        operationName, _options.MaxRetries);
                }
            }
            catch (PermanentException)
            {
                throw;
            }
            catch (Exception ex)
            {
                // Erros não classificados são considerados permanentes
                _logger.LogError(
                    ex,
                    "{OperationName} failed with permanent error on attempt {Attempt}. " +
                    "No retry will be attempted.",
                    operationName, attempt);
                throw new PermanentException(
                    $"Permanent error in {operationName}: {ex.Message}", ex);
            }
        }

        throw new TransientException(
            $"{operationName} failed after {_options.MaxRetries} attempts",
            lastException);
    }

    /// <summary>
    /// Executa uma operação sem retorno com retry.
    /// </summary>
    public async Task ExecuteAsync(
        string operationName,
        Func<Task> operation,
        CancellationToken cancellationToken = default)
    {
        await ExecuteAsync(operationName, async () =>
        {
            await operation();
            return true;
        }, cancellationToken);
    }
}

/// <summary>
/// Extensões para classificar exceções.
/// </summary>
public static class ExceptionClassifier
{
    /// <summary>
    /// Classifica uma exceção de download como transitória ou permanente.
    /// </summary>
    public static Exception ClassifyDownloadException(Exception ex, string url)
    {
        var message = ex.Message.ToLowerInvariant();
        var innerMessage = ex.InnerException?.Message?.ToLowerInvariant() ?? "";

        // Erros transitórios (network, timeout, rate limit)
        var transientPatterns = new[]
        {
            "timeout",
            "connection",
            "network",
            "unreachable",
            "temporarily",
            "rate limit",
            "429",
            "503",
            "502",
            "504",
            "dns",
            "name resolution",
            "connection refused",
            "no route to host"
        };

        // Erros permanentes (lógicos, configuração, permissões)
        var permanentPatterns = new[]
        {
            "permission denied",
            "not found",
            "invalid",
            "forbidden",
            "401",
            "403",
            "404",
            "disk full",
            "no space",
            "unauthorized",
            "bad request",
            "malformed"
        };

        if (transientPatterns.Any(p => message.Contains(p) || innerMessage.Contains(p)))
        {
            return new TransientException($"Transient error processing {url}: {ex.Message}", ex);
        }

        if (permanentPatterns.Any(p => message.Contains(p) || innerMessage.Contains(p)))
        {
            return new PermanentException($"Permanent error processing {url}: {ex.Message}", ex);
        }

        // Por padrão, erros de processamento são transitórios (yt-dlp pode falhar intermitentemente)
        return new TransientException($"Processing error for {url}: {ex.Message}", ex);
    }
}
