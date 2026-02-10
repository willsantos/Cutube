using System.Diagnostics.CodeAnalysis;
using Cutube.Api.Models;
using Cutube.Domain.Models;
using Cutube.Domain.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Cutube.Api.Services;

/// <summary>
/// Background service that processes downloads from the queue
/// </summary>
public class BackgroundDownloadWorker : BackgroundService
{
    private readonly IDownloadQueue _queue;
    private readonly IDownloadService _downloadService;
    private readonly IDownloadStatusRepository _statusRepository;
    private readonly ILogger<BackgroundDownloadWorker> _logger;

    public BackgroundDownloadWorker(
        IDownloadQueue queue,
        IDownloadService downloadService,
        IDownloadStatusRepository statusRepository,
        ILogger<BackgroundDownloadWorker> logger)
    {
        _queue = queue;
        _downloadService = downloadService;
        _statusRepository = statusRepository;
        _logger = logger;
    }

    [ExcludeFromCodeCoverage]
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("BackgroundDownloadWorker started");

        await foreach (var (downloadId, request) in _queue.DequeueAllAsync(stoppingToken))
        {
            _logger.LogInformation("Processing download {DownloadId} for URL {Url}", downloadId, request.Url);

            try
            {
                // 1. Initialize status as Queued
                await _statusRepository.AddAsync(downloadId, new DownloadStatusRecord
                {
                    Id = downloadId,
                    Url = request.Url,
                    Status = DownloadStatus.Queued,
                    Progress = 0,
                    Speed = 0,
                    DownloadedBytes = 0,
                    TotalBytes = 0,
                    CreatedAt = DateTime.UtcNow
                });

                // 2. Update to Downloading
                await _statusRepository.UpdateProgressAsync(downloadId, new Domain.Models.DownloadProgress
                {
                    State = "downloading",
                    Percentage = 0,
                    DownloadedBytes = 0,
                    TotalBytes = 0,
                    Speed = 0,
                    ErrorMessage = null
                });

                // 3. Create progress reporter to update status
                var progress = new Progress<Domain.Models.DownloadProgress>(async p =>
                {
                    await _statusRepository.UpdateProgressAsync(downloadId, p);
                    _logger.LogDebug("Download {DownloadId} progress: {Progress}%", downloadId, p.Percentage);
                });

                // 4. Execute download via Domain
                var result = await _downloadService.DownloadAsync(request, progress, stoppingToken);

                if (result.IsFailed)
                {
                    // Failure
                    await _statusRepository.UpdateProgressAsync(downloadId, new Domain.Models.DownloadProgress
                    {
                        State = "error",
                        Percentage = 0,
                        DownloadedBytes = 0,
                        TotalBytes = 0,
                        Speed = 0,
                        ErrorMessage = result.Errors.First().Message
                    });

                    _logger.LogError("Download {DownloadId} failed: {Error}", downloadId, result.Errors.First().Message);
                }
                else
                {
                    // Success
                    var domainResult = result.Value;
                    await _statusRepository.UpdateProgressAsync(downloadId, new Domain.Models.DownloadProgress
                    {
                        State = "finished",
                        Percentage = 100,
                        DownloadedBytes = domainResult.Size,
                        TotalBytes = domainResult.Size,
                        Speed = 0,
                        ErrorMessage = null
                    });

                    _logger.LogInformation("Download {DownloadId} completed: {FilePath}", downloadId, domainResult.FilePath);
                }
            }
            catch (Exception ex)
            {
                // Unexpected error
                await _statusRepository.UpdateProgressAsync(downloadId, new Domain.Models.DownloadProgress
                {
                    State = "error",
                    Percentage = 0,
                    DownloadedBytes = 0,
                    TotalBytes = 0,
                    Speed = 0,
                    ErrorMessage = ex.Message
                });

                _logger.LogError(ex, "Download {DownloadId} failed with exception", downloadId);
            }
        }
    }
}
