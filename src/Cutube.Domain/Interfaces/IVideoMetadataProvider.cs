using Cutube.Domain.Models;

namespace Cutube.Domain.Interfaces;

/// <summary>
/// Provides video metadata from external sources (e.g., yt-dlp)
/// </summary>
public interface IVideoMetadataProvider
{
    /// <summary>
    /// Retrieves metadata for a video URL
    /// </summary>
    /// <param name="url">Video URL to fetch metadata from</param>
    /// <param name="ct">Cancellation token for async operation</param>
    /// <returns>Video metadata including title, duration, uploader, etc.</returns>
    /// <exception cref="ArgumentException">Thrown when URL is invalid</exception>
    /// <exception cref="InvalidOperationException">Thrown when metadata cannot be retrieved</exception>
    Task<VideoMetadata> GetMetadataAsync(string url, CancellationToken ct = default);
}
