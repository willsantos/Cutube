namespace Cutube.Worker.Services;

/// <summary>
/// Service for notifying the API about download status and progress.
/// </summary>
public interface IDownloadStatusNotificationService
{
    /// <summary>
    /// Notify download status change.
    /// </summary>
    Task NotifyStatusAsync(
        string correlationId,
        string status,
        CancellationToken cancellationToken = default,
        string? errorMessage = null);

    /// <summary>
    /// Notify download progress.
    /// </summary>
    Task NotifyProgressAsync(
        string correlationId,
        int progress,
        double speed,
        long downloadedBytes,
        long? totalBytes = null,
        string? eta = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Notify that a download has been moved to the Dead Letter Queue.
    /// </summary>
    Task NotifyDeadLetterAsync(
        string correlationId,
        string errorMessage,
        int retryCount,
        CancellationToken cancellationToken = default);
}
