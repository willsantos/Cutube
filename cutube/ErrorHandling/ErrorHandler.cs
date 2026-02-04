using Cutube.Logging;

namespace Cutube.ErrorHandling;

public class ErrorHandler : IErrorHandler
{
    private readonly ILoggerService _logger;
    private readonly RetryPolicy _retryPolicy;

    private static readonly Dictionary<ErrorType, string> UserMessages = new()
    {
        [ErrorType.Network] = "Erro de conexão. Verifique sua internet.",
        [ErrorType.FileSystem] = "Erro ao acessar arquivo. Verifique permissões e espaço em disco.",
        [ErrorType.Validation] = "Entrada inválida. Verifique os dados informados.",
        [ErrorType.DependencyMissing] = "Dependência não encontrada. Instale yt-dlp e FFmpeg.",
        [ErrorType.Critical] = "Erro fatal. O aplicativo será encerrado."
    };

    public ErrorHandler(ILoggerService logger, RetryPolicy? retryPolicy = null)
    {
        _logger = logger;
        _retryPolicy = retryPolicy ?? new RetryPolicy();
    }

    public async Task<Result<T>> TryExecuteAsync<T>(
        Func<Task<T>> operation,
        ErrorType errorType = ErrorType.Unknown,
        string? context = null)
    {
        try
        {
            if (ShouldRetryForErrorType(errorType))
            {
                var result = await _retryPolicy.ExecuteAsync(operation);
                return Result<T>.Success(result);
            }
            else
            {
                var result = await operation();
                return Result<T>.Success(result);
            }
        }
        catch (Exception ex)
        {
            return HandleException<T>(ex, errorType, context);
        }
    }

    public Result<T> TryExecute<T>(
        Func<T> operation,
        ErrorType errorType = ErrorType.Unknown,
        string? context = null)
    {
        try
        {
            var result = operation();
            return Result<T>.Success(result);
        }
        catch (Exception ex)
        {
            return HandleException<T>(ex, errorType, context);
        }
    }

    public string GetUserFriendlyMessage(Exception exception)
    {
        var errorType = DetectErrorType(exception);
        return UserMessages.TryGetValue(errorType, out var message)
            ? message
            : "Ocorreu um erro inesperado.";
    }

    public bool ShouldRetry(Exception exception)
    {
        return _retryPolicy.ShouldRetryPredicate(exception);
    }

    private Result<T> HandleException<T>(Exception ex, ErrorType errorType, string? context)
    {
        var detectedErrorType = errorType == ErrorType.Unknown ? DetectErrorType(ex) : errorType;
        
        _logger.LogError(ex, $"Error in {context ?? "operation"}: {ex.Message}");

        return Result<T>.Failure(detectedErrorType, GetUserFriendlyMessage(ex), ex);
    }

    private ErrorType DetectErrorType(Exception exception)
    {
        return exception switch
        {
            HttpRequestException => ErrorType.Network,
            TimeoutException => ErrorType.Network,
            FileNotFoundException => ErrorType.DependencyMissing,
            DllNotFoundException => ErrorType.DependencyMissing,
            IOException => ErrorType.FileSystem,
            UnauthorizedAccessException => ErrorType.FileSystem,
            ArgumentException => ErrorType.Validation,
            InvalidOperationException => ErrorType.Validation,
            _ => ErrorType.Critical
        };
    }

    private bool ShouldRetryForErrorType(ErrorType errorType)
    {
        return errorType == ErrorType.Network;
    }
}
