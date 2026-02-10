namespace Cutube.Api.DTOs;

/// <summary>
/// Summary of a download
/// </summary>
public class DownloadSummary
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
    /// File path if completed
    /// </summary>
    public string? FilePath { get; init; }

    /// <summary>
    /// Creation timestamp
    /// </summary>
    public required DateTime CreatedAt { get; init; }
}

/// <summary>
/// Response for getting all downloads
/// </summary>
public class GetDownloadsResponse
{
    /// <summary>
    /// List of downloads
    /// </summary>
    public required DownloadSummary[] Downloads { get; init; }

    /// <summary>
    /// Total count
    /// </summary>
    public required int TotalCount { get; init; }
}
