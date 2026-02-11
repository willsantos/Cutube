namespace Cutube.Worker.Configuration;

/// <summary>
/// Configuration options for the download worker service.
/// </summary>
public class WorkerOptions
{
    public const string SectionName = "Worker";

    /// <summary>
    /// Maximum number of concurrent downloads.
    /// Default: 3
    /// </summary>
    public int MaxConcurrentDownloads { get; set; } = 3;

    /// <summary>
    /// Interval in milliseconds between progress updates to the API.
    /// Default: 2000ms (2 seconds)
    /// </summary>
    public int ProgressUpdateInterval { get; set; } = 2000;

    /// <summary>
    /// Base URL of the Cutube API for status callbacks.
    /// </summary>
    public required string ApiBaseUrl { get; set; } = "http://localhost:5000";

    /// <summary>
    /// Maximum timeout for a download in minutes.
    /// Default: 30 minutes
    /// </summary>
    public int DownloadTimeoutMinutes { get; set; } = 30;

    /// <summary>
    /// Enable graceful shutdown (finishes downloads in progress).
    /// </summary>
    public bool EnableGracefulShutdown { get; set; } = true;

    /// <summary>
    /// Timeout in seconds for graceful shutdown.
    /// Default: 30 seconds
    /// </summary>
    public int GracefulShutdownTimeoutSeconds { get; set; } = 30;
}
