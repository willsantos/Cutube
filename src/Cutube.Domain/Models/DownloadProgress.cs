namespace Cutube.Domain.Models;

/// <summary>
/// Progress information for a download operation
/// </summary>
public record DownloadProgress
{
    /// <summary>
    /// Current download state (e.g., "downloading", "processing", "finished")
    /// </summary>
    public required string State { get; init; }

    /// <summary>
    /// Download progress percentage (0-100)
    /// </summary>
    public required float Percentage { get; init; }

    /// <summary>
    /// Number of bytes downloaded
    /// </summary>
    public required long DownloadedBytes { get; init; }

    /// <summary>
    /// Total file size in bytes
    /// </summary>
    public required long TotalBytes { get; init; }

    /// <summary>
    /// Current download speed in bytes per second
    /// </summary>
    public required float Speed { get; init; }

    /// <summary>
    /// Error message if download failed
    /// </summary>
    public required string? ErrorMessage { get; init; }
}
