namespace Cutube.Domain.Models;

/// <summary>
/// Progress information for a video processing operation
/// </summary>
public record ProcessingProgress
{
    /// <summary>
    /// Processing progress percentage (0-100)
    /// </summary>
    public required int Percentage { get; init; }

    /// <summary>
    /// Current operation being performed (e.g., "cutting", "extracting audio")
    /// </summary>
    public required string CurrentOperation { get; init; }
}
