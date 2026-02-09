using Cutube.Domain.Models;

namespace Cutube.Api.Services;

/// <summary>
/// Interface for download queue
/// </summary>
public interface IDownloadQueue
{
    /// <summary>
    /// Enqueue a download request
    /// </summary>
    Task<string> EnqueueAsync(DownloadRequest request, CancellationToken ct);

    /// <summary>
    /// Cancel a download
    /// </summary>
    Task CancelAsync(string downloadId, CancellationToken ct);

    /// <summary>
    /// Dequeue all items from the queue (internal use by BackgroundDownloadWorker)
    /// </summary>
    IAsyncEnumerable<(string Id, DownloadRequest Request)> DequeueAllAsync(CancellationToken ct);
}
