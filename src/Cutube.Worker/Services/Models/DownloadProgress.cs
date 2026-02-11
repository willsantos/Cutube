namespace Cutube.Worker.Services.Models;

/// <summary>
/// Download progress reported by yt-dlp.
/// </summary>
public class DownloadProgress
{
    public required string CorrelationId { get; init; }
    public int Progress { get; set; }
    public double Speed { get; set; }
    public long DownloadedBytes { get; set; }
    public long? TotalBytes { get; set; }
    public string? Eta { get; set; }
    public string? Status { get; set; }
}
