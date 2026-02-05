namespace Cutube.Domain.Models;

/// <summary>
/// Request parameters for downloading a video
/// </summary>
public record DownloadRequest
{
    /// <summary>
    /// Video URL to download
    /// </summary>
    public required string Url { get; init; }

    /// <summary>
    /// Output file path for the downloaded video
    /// </summary>
    public required string OutputPath { get; init; }

    /// <summary>
    /// Optional time range to extract from the video
    /// </summary>
    public TimeRange? TimeRange { get; init; }

    /// <summary>
    /// Whether to download audio only
    /// </summary>
    public bool AudioOnly { get; init; }
}
