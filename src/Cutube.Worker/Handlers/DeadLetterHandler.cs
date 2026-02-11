using Cutube.Api.Queuing.Messages;
using Cutube.Worker.Services;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Cutube.Worker.Handlers;

/// <summary>
/// Handler para processar mensagens que chegam na Dead Letter Queue.
/// </summary>
public class DeadLetterHandler
{
    private readonly IDownloadStatusNotificationService _notificationService;
    private readonly ILogger<DeadLetterHandler> _logger;

    public DeadLetterHandler(
        IDownloadStatusNotificationService notificationService,
        ILogger<DeadLetterHandler> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    /// <summary>
    /// Processa uma mensagem que foi enviada para a DLQ.
    /// </summary>
    public async Task HandleAsync(
        ConsumeContext<DownloadMessage> context,
        Exception exception)
    {
        var message = context.Message;
        var correlationId = message.CorrelationId;

        _logger.LogError(
            exception,
            "Message moved to DLQ: {CorrelationId}, URL: {Url}, " +
            "RetryCount: {RetryCount}, Error: {ErrorMessage}",
            correlationId,
            message.Url,
            message.RetryCount,
            exception.Message);

        // Extrair informações detalhadas do erro
        var errorDetails = ExtractErrorDetails(exception);

        // Notificar API sobre a mensagem na DLQ
        await _notificationService.NotifyDeadLetterAsync(
            correlationId,
            errorDetails,
            message.RetryCount);

        // Log estruturado para análise
        _logger.LogInformation(
            "DLQ Message Logged: {@DlqMessage}",
            new
            {
                CorrelationId = correlationId,
                MessageId = message.MessageId,
                Url = message.Url,
                RetryCount = message.RetryCount,
                ErrorType = exception.GetType().Name,
                ErrorMessage = exception.Message,
                Timestamp = DateTime.UtcNow
            });
    }

    private string ExtractErrorDetails(Exception exception)
    {
        var details = new List<string>
        {
            $"Type: {exception.GetType().Name}",
            $"Message: {exception.Message}"
        };

        if (exception.InnerException != null)
        {
            details.Add($"Inner: {exception.InnerException.Message}");
        }

        // Extrair stack trace relevante (primeiras 5 linhas)
        if (!string.IsNullOrEmpty(exception.StackTrace))
        {
            var lines = exception.StackTrace
                .Split('\n')
                .Take(5)
                .Select(l => l.Trim());
            details.Add($"Stack: {string.Join(" | ", lines)}");
        }

        return string.Join(" | ", details);
    }
}
