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
    Cancelled
}

/// <summary>
/// Download status record
/// </summary>
public record DownloadStatusRecord
{
    public required string Id { get; init; }
    public required string Url { get; init; }
    public DownloadStatus Status { get; set; }
    public float Progress { get; set; }
    public float Speed { get; set; }
    public long? DownloadedBytes { get; set; }
    public long? TotalBytes { get; set; }
    public string? FilePath { get; set; }
    public string? ErrorMessage { get; set; }
    public required DateTime CreatedAt { get; init; }
    public DateTime? CompletedAt { get; set; }
}
