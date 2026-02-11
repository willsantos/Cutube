using System.Collections.Concurrent;
using Cutube.Api.Models;
using Microsoft.Extensions.Logging;

namespace Cutube.Api.Services;

/// <summary>
/// In-memory download status repository
/// </summary>
public class InMemoryStatusRepository : IDownloadStatusRepository
{
    private readonly ConcurrentDictionary<string, DownloadStatusRecord> _statuses = new();
    private readonly ILogger<InMemoryStatusRepository> _logger;

    public InMemoryStatusRepository(ILogger<InMemoryStatusRepository> logger)
    {
        _logger = logger;
    }

    public Task AddAsync(DownloadStatusRecord status, CancellationToken cancellationToken = default)
    {
        _statuses[status.CorrelationId] = status;
        _logger.LogDebug("Added download status: {CorrelationId}, State: {State}",
            status.CorrelationId, status.Status);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(string correlationId, Action<DownloadStatusRecord> updateAction, CancellationToken cancellationToken = default)
    {
        if (_statuses.TryGetValue(correlationId, out var status))
        {
            updateAction(status);
            status = status with { UpdatedAt = DateTime.UtcNow };
            _statuses[correlationId] = status;
            _logger.LogDebug("Updated download status: {CorrelationId}, State: {State}",
                correlationId, status.Status);
        }
        else
        {
            _logger.LogWarning("Download status not found for update: {CorrelationId}", correlationId);
        }
        return Task.CompletedTask;
    }

    public Task UpdateStateAsync(string correlationId, DownloadStatus newState, CancellationToken cancellationToken = default)
    {
        return UpdateAsync(correlationId, status =>
        {
            status.Status = newState;

            if (newState == DownloadStatus.Downloading && status.StartedAt.HasValue == false)
            {
                status = status with { StartedAt = DateTime.UtcNow };
            }

            if (newState is DownloadStatus.Completed or DownloadStatus.Failed or DownloadStatus.Cancelled or DownloadStatus.DeadLetter)
            {
                status = status with { CompletedAt = DateTime.UtcNow };
            }
        }, cancellationToken);
    }

    public Task UpdateProgressAsync(string id, Domain.Models.DownloadProgress progress)
    {
        if (_statuses.TryGetValue(id, out var status))
        {
            // Map progress state to DownloadStatus
            var downloadStatus = progress.State.ToLowerInvariant() switch
            {
                "downloading" => DownloadStatus.Downloading,
                "processing" => DownloadStatus.Processing,
                "finished" or "complete" => DownloadStatus.Completed,
                "error" or "failed" => DownloadStatus.Failed,
                _ => status.Status
            };

            var updated = status with
            {
                Status = downloadStatus,
                Progress = progress.Percentage,
                Speed = progress.Speed,
                DownloadedBytes = progress.DownloadedBytes,
                TotalBytes = progress.TotalBytes,
                ErrorMessage = progress.ErrorMessage,
                UpdatedAt = DateTime.UtcNow,
                CompletedAt = downloadStatus == DownloadStatus.Completed ? DateTime.UtcNow : status.CompletedAt
            };

            _statuses[id] = updated;
        }

        return Task.CompletedTask;
    }

    public Task UpdateDetailedProgressAsync(string correlationId, int progress, double speed, long downloadedBytes, CancellationToken cancellationToken = default)
    {
        return UpdateAsync(correlationId, status =>
        {
            status.Progress = Math.Clamp(progress, 0, 100);
            status.Speed = (float)speed;
            status.DownloadedBytes = downloadedBytes;
        }, cancellationToken);
    }

    public Task<DownloadStatusRecord?> GetByCorrelationIdAsync(string correlationId, CancellationToken cancellationToken = default)
    {
        _statuses.TryGetValue(correlationId, out var status);
        return Task.FromResult(status);
    }

    public Task<DownloadStatusRecord?> GetByIdAsync(string id, CancellationToken ct)
    {
        // Try to find by correlationId or by Id
        if (_statuses.TryGetValue(id, out var status))
        {
            return Task.FromResult<DownloadStatusRecord?>(status);
        }

        // Search by Id field
        status = _statuses.Values.FirstOrDefault(s => s.Id == id);
        return Task.FromResult(status);
    }

    public Task<IReadOnlyList<DownloadStatusRecord>> GetAllAsync(DownloadStatus? state = null, int? limit = null, CancellationToken cancellationToken = default)
    {
        var query = _statuses.Values.AsEnumerable();

        if (state.HasValue)
        {
            query = query.Where(s => s.Status == state.Value);
        }

        query = query.OrderByDescending(s => s.CreatedAt);

        if (limit.HasValue)
        {
            query = query.Take(limit.Value);
        }

        return Task.FromResult<IReadOnlyList<DownloadStatusRecord>>(query.ToList());
    }

    // Legacy support for GetAllAsync with CancellationToken only
    public Task<IEnumerable<DownloadStatusRecord>> GetAllAsync(CancellationToken ct)
    {
        return Task.FromResult<IEnumerable<DownloadStatusRecord>>(_statuses.Values.ToList());
    }

    public Task<IReadOnlyList<DownloadStatusRecord>> GetByStatesAsync(IEnumerable<DownloadStatus> states, int? limit = null, CancellationToken cancellationToken = default)
    {
        var stateSet = states.ToHashSet();
        var query = _statuses.Values
            .Where(s => stateSet.Contains(s.Status))
            .OrderByDescending(s => s.CreatedAt);

        if (limit.HasValue)
        {
            return Task.FromResult<IReadOnlyList<DownloadStatusRecord>>(query.Take(limit.Value).ToList());
        }

        return Task.FromResult<IReadOnlyList<DownloadStatusRecord>>(query.ToList());
    }

    public Task DeleteAsync(string id, CancellationToken ct)
    {
        if (_statuses.TryRemove(id, out _))
        {
            _logger.LogInformation("Download {DownloadId} removed from repository", id);
        }
        return Task.CompletedTask;
    }

    public Task<int> CleanupOldRecordsAsync(TimeSpan maxAge, CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow - maxAge;
        var keysToRemove = _statuses
            .Where(kvp => kvp.Value.UpdatedAt < cutoff)
            .Select(kvp => kvp.Key)
            .ToList();

        int removedCount = 0;
        foreach (var key in keysToRemove)
        {
            if (_statuses.TryRemove(key, out _))
            {
                removedCount++;
            }
        }

        if (removedCount > 0)
        {
            _logger.LogInformation("Cleaned up {Count} old download records", removedCount);
        }

        return Task.FromResult(removedCount);
    }

    public Task<Dictionary<DownloadStatus, int>> CountByStateAsync(CancellationToken cancellationToken = default)
    {
        var counts = _statuses.Values
            .GroupBy(s => s.Status)
            .ToDictionary(g => g.Key, g => g.Count());

        return Task.FromResult(counts);
    }

    public Task<DownloadStats> GetStatsAsync(CancellationToken cancellationToken = default)
    {
        var counts = CountByStateAsync(cancellationToken).Result;

        var completed = _statuses.Values
            .Where(s => s.Status == DownloadStatus.Completed && s.Duration.HasValue)
            .ToList();
        var avgProcessingTime = completed.Any()
            ? completed.Average(s => s.Duration!.Value.TotalSeconds)
            : 0;

        var totalCompleted = counts.GetValueOrDefault(DownloadStatus.Completed);
        var totalFailed = counts.GetValueOrDefault(DownloadStatus.Failed) + counts.GetValueOrDefault(DownloadStatus.DeadLetter);
        var errorRate = totalCompleted + totalFailed > 0
            ? (double)totalFailed / (totalCompleted + totalFailed) * 100
            : 0;

        var stats = new DownloadStats
        {
            TotalDownloads = _statuses.Count,
            PendingCount = 0, // Not used in current implementation
            QueuedCount = counts.GetValueOrDefault(DownloadStatus.Queued),
            ProcessingCount = counts.GetValueOrDefault(DownloadStatus.Downloading) + counts.GetValueOrDefault(DownloadStatus.Processing),
            CompletedCount = totalCompleted,
            FailedCount = totalFailed,
            DeadLetterCount = counts.GetValueOrDefault(DownloadStatus.DeadLetter),
            AverageProcessingTimeSeconds = avgProcessingTime,
            ErrorRate = errorRate
        };

        return Task.FromResult(stats);
    }
}
