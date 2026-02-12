using System.Diagnostics.CodeAnalysis;

namespace Cutube.Api.DTOs;

/// <summary>
/// Request to create a new download
/// </summary>
[ExcludeFromCodeCoverage]
public record CreateDownloadRequest
{
    /// <summary>
    /// Video URL to download
    /// </summary>
    public required string Url { get; init; }

    /// <summary>
    /// Output directory path (optional, defaults to /downloads)
    /// </summary>
    public string? OutputPath { get; init; }

    /// <summary>
    /// Optional start time for video clip (format: HH:MM:SS)
    /// </summary>
    public string? StartTime { get; init; }

    /// <summary>
    /// Optional end time for video clip (format: HH:MM:SS)
    /// </summary>
    public string? EndTime { get; init; }

    /// <summary>
    /// Whether to download audio only
    /// </summary>
    public bool? AudioOnly { get; init; }

    /// <summary>
    /// Optional custom filename for output
    /// </summary>
    public string? OutputFilename { get; init; }

    /// <summary>
    /// Priority for processing (default: "normal")
    /// </summary>
    public string? Priority { get; init; }
}
