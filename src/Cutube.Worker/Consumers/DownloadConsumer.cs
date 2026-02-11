using Cutube.Api.Queuing.Messages;
using Cutube.Worker.Configuration;
using Cutube.Worker.Services;
using MassTransit;
using Microsoft.Extensions.Options;

namespace Cutube.Worker.Consumers;

/// <summary>
/// MassTransit consumer for processing download messages from RabbitMQ queue.
/// </summary>
public class DownloadConsumer : IConsumer<DownloadMessage>
{
    private readonly IDownloadProcessingService _processingService;
    private readonly ILogger<DownloadConsumer> _logger;
    private readonly WorkerOptions _options;

    public DownloadConsumer(
        IDownloadProcessingService processingService,
        ILogger<DownloadConsumer> logger,
        IOptions<WorkerOptions> options)
    {
        _processingService = processingService;
        _logger = logger;
        _options = options.Value;
    }

    public async Task Consume(ConsumeContext<DownloadMessage> context)
    {
        var message = context.Message;
        var correlationId = message.CorrelationId;

        _logger.LogInformation(
            "Received download message: {CorrelationId}, URL: {Url}",
            correlationId,
            message.Url);

        try
        {
            // Validate message
            ValidateMessage(message);

            // Process download with timeout
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(_options.DownloadTimeoutMinutes));

            // Link cancellation of the context with the CTS
            await using var registration = context.CancellationToken.Register(() => cts.Cancel());

            await _processingService.ProcessDownloadAsync(message, cts.Token);

            _logger.LogInformation(
                "Download completed successfully: {CorrelationId}",
                correlationId);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning(
                "Download cancelled: {CorrelationId}",
                correlationId);
            throw; // Re-throw for MassTransit to handle (retry or DLQ)
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error processing download: {CorrelationId}. Error: {Error}",
                correlationId,
                ex.Message);

            // Re-throw for MassTransit retry policy
            throw;
        }
    }

    private void ValidateMessage(DownloadMessage message)
    {
        if (string.IsNullOrWhiteSpace(message.Url))
        {
            throw new ArgumentException("URL is required", nameof(message));
        }

        if (!Uri.TryCreate(message.Url, UriKind.Absolute, out _))
        {
            throw new ArgumentException("Invalid URL format", nameof(message));
        }

        if (string.IsNullOrWhiteSpace(message.OutputPath))
        {
            throw new ArgumentException("Output path is required", nameof(message));
        }
    }
}
