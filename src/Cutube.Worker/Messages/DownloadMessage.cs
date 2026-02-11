namespace Cutube.Worker.Messages;

/// <summary>
/// Message contract for download requests
/// </summary>
public class DownloadMessage
{
    public Guid DownloadId { get; set; }
    public string Url { get; set; } = string.Empty;
    public string? Format { get; set; }
    public string? Quality { get; set; }
    public string? OutputPath { get; set; }
    public DateTime CreatedAt { get; set; }
}
