using Cutube.Domain.Models;

namespace Cutube.Domain.Interfaces;

/// <summary>
/// Downloads videos from external sources
/// </summary>
public interface IVideoDownloader
{
    /// <summary>
    /// Downloads a video based on the request parameters
    /// </summary>
    /// <param name="request">Download request with URL, output path, and options</param>
    /// <param name="progress">Optional progress reporter for download status</param>
    /// <param name="ct">Cancellation token for async operation</param>
    /// <returns>Download result with success status, output path, and file size</returns>
    /// <exception cref="ArgumentException">Thrown when request parameters are invalid</exception>
    /// <exception cref="OperationCanceledException">Thrown when operation is cancelled</exception>
    Task<DownloadResult> DownloadAsync(
        DownloadRequest request,
        IProgress<DownloadProgress>? progress = null,
        CancellationToken ct = default);
}
