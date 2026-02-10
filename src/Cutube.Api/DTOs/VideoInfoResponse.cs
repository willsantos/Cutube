using System.Diagnostics.CodeAnalysis;

namespace Cutube.Api.DTOs;

/// <summary>
/// Video metadata response
/// </summary>
[ExcludeFromCodeCoverage]
public class VideoInfoResponse
{
    /// <summary>
    /// Video URL
    /// </summary>
    public required string Url { get; init; }

    /// <summary>
    /// Video title
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Uploader/channel name
    /// </summary>
    public required string? Uploader { get; init; }

    /// <summary>
    /// Video duration (format: HH:MM:SS)
    /// </summary>
    public required string Duration { get; init; }

    /// <summary>
    /// Thumbnail URL
    /// </summary>
    public required string ThumbnailUrl { get; init; }

    /// <summary>
    /// Upload date
    /// </summary>
    public required DateTime UploadDate { get; init; }
}
