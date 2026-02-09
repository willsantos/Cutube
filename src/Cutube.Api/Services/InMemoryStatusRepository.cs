using System.Collections.Concurrent;
using Cutube.Api.Models;
using Microsoft.Extensions.Logging;

namespace Cutube.Api.Services;

/// <summary>
/// In-memory download status repository
/// </summary>
public class InMemoryStatusRepository : IDownloadStatusRepository
{
    private readonly ConcurrentDictionary<string, DownloadStatusRecord> _downloads = new();
    private readonly ILogger<InMemoryStatusRepository> _logger;

    public InMemoryStatusRepository(ILogger<InMemoryStatusRepository> logger)
    {
        _logger = logger;
    }

    public Task AddAsync(string id, DownloadStatusRecord status)
    {
        _downloads[id] = status;
        _logger.LogInformation("Download {DownloadId} added to repository", id);
        return Task.CompletedTask;
    }

    public Task<DownloadStatusRecord?> GetByIdAsync(string id, CancellationToken ct)
    {
        _downloads.TryGetValue(id, out var status);
        return Task.FromResult(status);
    }

    public Task<IEnumerable<DownloadStatusRecord>> GetAllAsync(CancellationToken ct)
    {
        return Task.FromResult<IEnumerable<DownloadStatusRecord>>(_downloads.Values.ToList());
    }

    public Task UpdateProgressAsync(string id, Domain.Models.DownloadProgress progress)
    {
        if (_downloads.TryGetValue(id, out var status))
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
                CompletedAt = downloadStatus == DownloadStatus.Completed ? DateTime.UtcNow : status.CompletedAt
            };

            _downloads[id] = updated;
        }

        return Task.CompletedTask;
    }

    public Task DeleteAsync(string id, CancellationToken ct)
    {
        _downloads.TryRemove(id, out _);
        _logger.LogInformation("Download {DownloadId} removed from repository", id);
        return Task.CompletedTask;
    }
}
