using Cutube.Api.Queuing.Messages;

namespace Cutube.Worker.Services;

/// <summary>
/// Service responsible for processing video downloads.
/// </summary>
public interface IDownloadProcessingService
{
    /// <summary>
    /// Process a video download.
    /// </summary>
    /// <param name="message">Download message with parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ProcessDownloadAsync(DownloadMessage message, CancellationToken cancellationToken = default);
}
