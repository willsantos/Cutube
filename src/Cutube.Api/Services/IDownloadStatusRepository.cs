using Cutube.Api.Models;

namespace Cutube.Api.Services;

/// <summary>
/// Interface for download status repository
/// </summary>
public interface IDownloadStatusRepository
{
    /// <summary>
    /// Add a download status
    /// </summary>
    Task AddAsync(DownloadStatusRecord status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update download status with custom action
    /// </summary>
    Task UpdateAsync(string correlationId, Action<DownloadStatusRecord> updateAction, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update download state
    /// </summary>
    Task UpdateStateAsync(string correlationId, DownloadStatus newState, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update download progress
    /// </summary>
    Task UpdateProgressAsync(string id, Domain.Models.DownloadProgress progress);

    /// <summary>
    /// Update detailed progress
    /// </summary>
    Task UpdateDetailedProgressAsync(string correlationId, int progress, double speed, long downloadedBytes, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get download status by correlation ID
    /// </summary>
    Task<DownloadStatusRecord?> GetByCorrelationIdAsync(string correlationId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get download status by ID (legacy support)
    /// </summary>
    Task<DownloadStatusRecord?> GetByIdAsync(string id, CancellationToken ct);

    /// <summary>
    /// Get all download statuses
    /// </summary>
    Task<IReadOnlyList<DownloadStatusRecord>> GetAllAsync(DownloadStatus? state = null, int? limit = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all download statuses (legacy signature)
    /// </summary>
    Task<IEnumerable<DownloadStatusRecord>> GetAllAsync(CancellationToken ct);

    /// <summary>
    /// Get downloads by multiple states
    /// </summary>
    Task<IReadOnlyList<DownloadStatusRecord>> GetByStatesAsync(IEnumerable<DownloadStatus> states, int? limit = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a download status
    /// </summary>
    Task DeleteAsync(string id, CancellationToken ct);

    /// <summary>
    /// Cleanup old records
    /// </summary>
    Task<int> CleanupOldRecordsAsync(TimeSpan maxAge, CancellationToken cancellationToken = default);

    /// <summary>
    /// Count downloads by state
    /// </summary>
    Task<Dictionary<DownloadStatus, int>> CountByStateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get download statistics
    /// </summary>
    Task<DownloadStats> GetStatsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Download statistics
/// </summary>
public class DownloadStats
{
    public int TotalDownloads { get; set; }
    public int PendingCount { get; set; }
    public int QueuedCount { get; set; }
    public int ProcessingCount { get; set; }
    public int CompletedCount { get; set; }
    public int FailedCount { get; set; }
    public int DeadLetterCount { get; set; }
    public double AverageProcessingTimeSeconds { get; set; }
    public double ErrorRate { get; set; }
}
