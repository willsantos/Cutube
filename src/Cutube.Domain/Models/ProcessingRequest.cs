namespace Cutube.Domain.Models;

/// <summary>
/// Request parameters for processing a video
/// </summary>
public record ProcessingRequest
{
    /// <summary>
    /// Input video file path
    /// </summary>
    public required string InputPath { get; init; }

    /// <summary>
    /// Output file path for the processed video
    /// </summary>
    public required string OutputPath { get; init; }

    /// <summary>
    /// Time range to extract
    /// </summary>
    public required TimeRange TimeRange { get; init; }

    /// <summary>
    /// Whether to extract audio only
    /// </summary>
    public bool AudioOnly { get; init; }
}
