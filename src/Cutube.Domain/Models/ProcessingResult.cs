namespace Cutube.Domain.Models;

/// <summary>
/// Result of a video processing operation
/// </summary>
public record ProcessingResult
{
    /// <summary>
    /// Whether the processing succeeded
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Path to the processed file
    /// </summary>
    public required string OutputPath { get; init; }

    /// <summary>
    /// Size of the processed file in bytes
    /// </summary>
    public required long FileSizeBytes { get; init; }

    /// <summary>
    /// Error message if processing failed
    /// </summary>
    public required string? ErrorMessage { get; init; }
}
