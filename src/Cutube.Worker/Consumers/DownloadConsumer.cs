using Cutube.Contracts.Messages;
using Cutube.Worker.Configuration;
using Cutube.Worker.Exceptions;
using Cutube.Worker.Handlers;
using Cutube.Worker.Services;
using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SerilogLogContext = Serilog.Context.LogContext;

namespace Cutube.Worker.Consumers;

/// <summary>
/// Consumer que processa mensagens de download com retry automático.
/// </summary>
public class DownloadConsumer : IConsumer<DownloadMessage>
{
    private readonly IDownloadProcessingService _processingService;
    private readonly IDownloadStatusNotificationService _notificationService;
    private readonly RetryHandler _retryHandler;
    private readonly ILogger<DownloadConsumer> _logger;
    private readonly WorkerOptions _options;

    public DownloadConsumer(
        IDownloadProcessingService processingService,
        IDownloadStatusNotificationService notificationService,
        RetryHandler retryHandler,
        ILogger<DownloadConsumer> logger,
        IOptions<WorkerOptions> options)
    {
        _processingService = processingService;
        _notificationService = notificationService;
        _retryHandler = retryHandler;
        _logger = logger;
        _options = options.Value;
    }

    public async Task Consume(ConsumeContext<DownloadMessage> context)
    {
        var message = context.Message;
        var correlationId = message.CorrelationId;
        using var correlationScope = SerilogLogContext.PushProperty("CorrelationId", correlationId);
        using var messageScope = SerilogLogContext.PushProperty("MessageId", message.MessageId);
        var startedAt = DateTime.UtcNow;

        _logger.LogInformation(
            "Processing download: {CorrelationId}, URL: {Url}, Attempt: {RetryCount}",
            correlationId, message.Url, message.RetryCount);

        try
        {
            // Notificar início do processamento
            await _notificationService.NotifyStatusAsync(correlationId, "processing", context.CancellationToken);

            // Executar download com retry
            await _retryHandler.ExecuteAsync(
                $"Download-{correlationId}",
                async () => await ExecuteDownloadAsync(message, context.CancellationToken),
                context.CancellationToken);

            // Sucesso
            await _notificationService.NotifyStatusAsync(correlationId, "completed", context.CancellationToken);

            _logger.LogInformation(
                "Download completed successfully: {CorrelationId}",
                correlationId);

            _logger.LogInformation(
                "Download pipeline duration: {ElapsedMs}ms",
                (DateTime.UtcNow - startedAt).TotalMilliseconds);
        }
        catch (TransientException ex)
        {
            // Todas as tentativas falharam - enviar para DLQ
            _logger.LogError(
                ex,
                "Download failed after all retry attempts: {CorrelationId}. " +
                "Sending to DLQ...",
                correlationId);

            await _notificationService.NotifyDeadLetterAsync(
                correlationId,
                ex.Message,
                message.RetryCount + 1);

            // Re-lançar para que MassTransit envie para DLQ
            throw;
        }
        catch (PermanentException ex)
        {
            // Erro permanente - enviar direto para DLQ sem retry
            _logger.LogError(
                ex,
                "Download failed with permanent error: {CorrelationId}. " +
                "Sending to DLQ...",
                correlationId);

            await _notificationService.NotifyDeadLetterAsync(
                correlationId,
                ex.Message,
                message.RetryCount);

            throw;
        }
        catch (OperationCanceledException)
        {
            // Download cancelado pelo usuário
            await _notificationService.NotifyStatusAsync(correlationId, "cancelled", context.CancellationToken);
            _logger.LogWarning(
                "Download cancelled: {CorrelationId}",
                correlationId);
            throw;
        }
        catch (Exception ex)
        {
            // Erro não esperado - tentar classificar
            _logger.LogError(
                ex,
                "Unexpected error processing download: {CorrelationId}",
                correlationId);

            var classified = ExceptionClassifier.ClassifyDownloadException(ex, message.Url);

            if (classified is TransientException)
            {
                await _notificationService.NotifyDeadLetterAsync(
                    correlationId,
                    $"Failed after retries: {ex.Message}",
                    message.RetryCount + 1);
            }
            else
            {
                await _notificationService.NotifyDeadLetterAsync(
                    correlationId,
                    ex.Message,
                    message.RetryCount);
            }

            throw;
        }
    }

    private async Task ExecuteDownloadAsync(
        DownloadMessage message,
        CancellationToken cancellationToken)
    {
        // Validar mensagem
        ValidateMessage(message);

        // Processar download com timeout
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(_options.DownloadTimeoutMinutes));

        // Link cancellation
        await using var registration = cancellationToken.Register(() => cts.Cancel());

        try
        {
            await _processingService.ProcessDownloadAsync(message, cts.Token);
        }
        catch (Exception ex)
        {
            // Classificar e relançar
            var classified = ExceptionClassifier.ClassifyDownloadException(ex, message.Url);
            throw classified;
        }
    }

    private void ValidateMessage(DownloadMessage message)
    {
        if (string.IsNullOrWhiteSpace(message.Url))
        {
            throw new PermanentException("URL is required");
        }

        if (!Uri.TryCreate(message.Url, UriKind.Absolute, out _))
        {
            throw new PermanentException("Invalid URL format");
        }

        if (string.IsNullOrWhiteSpace(message.OutputPath))
        {
            throw new PermanentException("Output path is required");
        }
    }
}
