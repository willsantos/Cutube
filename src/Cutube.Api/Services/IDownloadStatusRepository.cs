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
    Task AddAsync(string id, DownloadStatusRecord status);

    /// <summary>
    /// Get download status by ID
    /// </summary>
    Task<DownloadStatusRecord?> GetByIdAsync(string id, CancellationToken ct);

    /// <summary>
    /// Get all download statuses
    /// </summary>
    Task<IEnumerable<DownloadStatusRecord>> GetAllAsync(CancellationToken ct);

    /// <summary>
    /// Update download progress
    /// </summary>
    Task UpdateProgressAsync(string id, Domain.Models.DownloadProgress progress);

    /// <summary>
    /// Delete a download status
    /// </summary>
    Task DeleteAsync(string id, CancellationToken ct);
}
