namespace Cutube.Domain.Models;

/// <summary>
/// Represents metadata for a video
/// </summary>
public record VideoMetadata
{
    /// <summary>
    /// Video URL
    /// </summary>
    public required string Url { get; init; }

    /// <summary>
    /// Video title (sanitized for filename usage)
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Video uploader/channel name
    /// </summary>
    public required string? Uploader { get; init; }

    /// <summary>
    /// Video duration
    /// </summary>
    public required TimeSpan Duration { get; init; }

    /// <summary>
    /// Video upload date
    /// </summary>
    public required DateTime UploadDate { get; init; }

    /// <summary>
    /// Thumbnail image URL
    /// </summary>
    public required string ThumbnailUrl { get; init; }
}
