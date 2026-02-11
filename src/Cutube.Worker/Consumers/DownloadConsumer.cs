using MassTransit;
using Microsoft.Extensions.Logging;

namespace Cutube.Worker.Consumers;

/// <summary>
/// Consumer for processing download messages from the queue
/// </summary>
public class DownloadConsumer : IConsumer<Messages.DownloadMessage>
{
    private readonly ILogger<DownloadConsumer> _logger;

    public DownloadConsumer(ILogger<DownloadConsumer> logger)
    {
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<Messages.DownloadMessage> context)
    {
        var message = context.Message;

        _logger.LogInformation("Processing download: {DownloadId} for URL: {Url}", message.DownloadId, message.Url);

        // TODO: Implement actual download processing in Phase 3.3
        // For now, just log the message

        await Task.CompletedTask;

        _logger.LogInformation("Download {DownloadId} processed successfully", message.DownloadId);
    }
}
