namespace Cutube.Api.DTOs;

/// <summary>
/// Detailed information about a download
/// </summary>
public class DownloadDetails
{
    /// <summary>
    /// Unique identifier for the download
    /// </summary>
    public required string DownloadId { get; init; }

    /// <summary>
    /// Video URL
    /// </summary>
    public required string Url { get; init; }

    /// <summary>
    /// Current status
    /// </summary>
    public required string Status { get; init; }

    /// <summary>
    /// Progress percentage (0-100)
    /// </summary>
    public required float Progress { get; init; }

    /// <summary>
    /// Download speed in bytes per second
    /// </summary>
    public required float Speed { get; init; }

    /// <summary>
    /// Estimated time remaining (format: HH:MM:SS)
    /// </summary>
    public string? Eta { get; init; }

    /// <summary>
    /// Downloaded bytes
    /// </summary>
    public required long DownloadedBytes { get; init; }

    /// <summary>
    /// Total bytes
    /// </summary>
    public required long TotalBytes { get; init; }

    /// <summary>
    /// File path if completed
    /// </summary>
    public string? FilePath { get; init; }

    /// <summary>
    /// Error message if failed
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Creation timestamp
    /// </summary>
    public required DateTime CreatedAt { get; init; }

    /// <summary>
    /// Completion timestamp
    /// </summary>
    public DateTime? CompletedAt { get; init; }
}
