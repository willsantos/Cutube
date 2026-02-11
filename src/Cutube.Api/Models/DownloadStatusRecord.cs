namespace Cutube.Api.Models;

/// <summary>
/// Status of a download
/// </summary>
public enum DownloadStatus
{
    Queued,
    Downloading,
    Processing,
    Completed,
    Failed,
    Cancelled,
    DeadLetter,
    Expired
}

/// <summary>
/// Download metadata (video information)
/// </summary>
public record DownloadMetadata
{
    public string? Title { get; init; }
    public string? Duration { get; init; }
    public string? Thumbnail { get; init; }
    public string? Channel { get; init; }
}

/// <summary>
/// Download status record
/// </summary>
public record DownloadStatusRecord
{
    public required string Id { get; init; }
    public string CorrelationId { get; init; } = string.Empty; // For backward compatibility
    public required string Url { get; init; }
    public DownloadStatus Status { get; set; } = DownloadStatus.Queued;
    public float Progress { get; set; } = 0; // Kept as float for backward compatibility
    public float Speed { get; set; } = 0; // Kept as float for backward compatibility
    public long DownloadedBytes { get; set; } = 0; // Changed from nullable to match old code
    public long? TotalBytes { get; set; }
    public TimeSpan? Eta { get; set; }
    public string? FilePath { get; set; } // Kept for backward compatibility, maps to OutputPath
    public string? OutputPath { get; set; } // New field
    public string? OutputFilename { get; set; }
    public bool AudioOnly { get; set; } = false;
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; } = 0;
    public required DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public TimeSpan? Duration => CompletedAt.HasValue && StartedAt.HasValue
        ? CompletedAt.Value - StartedAt.Value
        : null;
    public DownloadMetadata? Metadata { get; set; }
}
