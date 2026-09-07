namespace Cutube.Domain.Models;

/// <summary>
/// Result of a download operation
/// </summary>
public record DownloadResult
{
    /// <summary>
    /// Whether the download succeeded
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Path to the downloaded file
    /// </summary>
    public required string OutputPath { get; init; }

    /// <summary>
    /// Size of the downloaded file in bytes
    /// </summary>
    public required long FileSizeBytes { get; init; }

    /// <summary>
    /// Duration of the downloaded video
    /// </summary>
    public required TimeSpan Duration { get; init; }

    /// <summary>
    /// Error message if download failed
    /// </summary>
    public required string? ErrorMessage { get; init; }
}
