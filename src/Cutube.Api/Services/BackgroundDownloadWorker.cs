using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Cutube.Api.Hubs;
using Cutube.Api.Models;
using Cutube.Domain.Models;
using Cutube.Domain.Services;
using Microsoft.AspNetCore.SignalR;
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
    private readonly IHubContext<DownloadHub, IDownloadHubClient> _hub;
    private readonly ILogger<BackgroundDownloadWorker> _logger;

    // Throttle: enviar no máximo 1 update por segundo por download
    private readonly ConcurrentDictionary<string, DateTime> _lastProgressUpdate = new();

    public BackgroundDownloadWorker(
        IDownloadQueue queue,
        IDownloadService downloadService,
        IDownloadStatusRepository statusRepository,
        IHubContext<DownloadHub, IDownloadHubClient> hub,
        ILogger<BackgroundDownloadWorker> logger)
    {
        _queue = queue;
        _downloadService = downloadService;
        _statusRepository = statusRepository;
        _hub = hub;
        _logger = logger;
    }

    /// <summary>
    /// Notifica clientes conectados que download iniciou
    /// </summary>
    private async Task NotifyStartedAsync(string downloadId, Domain.Models.DownloadRequest request)
    {
        var groupName = DownloadHub.GetDownloadGroupName(downloadId);
        var eventData = new DownloadStartedEvent(
            downloadId,
            request.Url,
            DateTime.UtcNow
        );

        await _hub.Clients.Group(groupName)
            .DownloadStarted(downloadId, eventData);

        _logger.LogDebug("Sent DownloadStarted event for {DownloadId} to group {GroupName}",
            downloadId, groupName);
    }

    /// <summary>
    /// Atualiza progresso: repository + SignalR
    /// </summary>
    private async Task UpdateProgressAsync(string downloadId, Domain.Models.DownloadProgress domainProgress)
    {
        // Throttle: enviar no máximo 1 update por segundo
        var now = DateTime.UtcNow;
        if (_lastProgressUpdate.TryGetValue(downloadId, out var lastUpdate))
        {
            var timeSinceLastUpdate = (now - lastUpdate).TotalMilliseconds;
            if (timeSinceLastUpdate < 1000 && domainProgress.Percentage < 100)
            {
                // Skip update (ainda não passou 1s)
                return;
            }
        }

        _lastProgressUpdate[downloadId] = now;

        // 1. Atualizar repository (para REST GET /api/downloads/{id})
        await _statusRepository.UpdateProgressAsync(downloadId, domainProgress);

        // 2. Enviar SignalR event para clients conectados
        var groupName = DownloadHub.GetDownloadGroupName(downloadId);
        var eventData = new DownloadProgressEvent(
            downloadId,
            domainProgress.Percentage,
            domainProgress.Speed,
            null, // Eta - não disponível no Domain.DownloadProgress atualmente
            domainProgress.DownloadedBytes,
            domainProgress.TotalBytes,
            domainProgress.State.ToLowerInvariant()
        );

        await _hub.Clients.Group(groupName)
            .DownloadProgress(downloadId, eventData);

        _logger.LogDebug("Sent DownloadProgress event for {DownloadId}: {Progress}%",
            downloadId, domainProgress.Percentage);
    }

    /// <summary>
    /// Notifica clientes que download completou
    /// </summary>
    private async Task NotifyCompletedAsync(string downloadId, Domain.Models.DomainDownloadResult result)
    {
        var groupName = DownloadHub.GetDownloadGroupName(downloadId);
        var eventData = new DownloadCompletedEvent(
            downloadId,
            result.FilePath,
            result.Size,
            result.Duration,
            DateTime.UtcNow
        );

        await _hub.Clients.Group(groupName)
            .DownloadCompleted(downloadId, eventData);

        _logger.LogInformation("Sent DownloadCompleted event for {DownloadId}", downloadId);
    }

    /// <summary>
    /// Notifica clientes que download falhou
    /// </summary>
    private async Task NotifyFailedAsync(string downloadId, string error)
    {
        var groupName = DownloadHub.GetDownloadGroupName(downloadId);
        var eventData = new DownloadFailedEvent(
            downloadId,
            error,
            DateTime.UtcNow
        );

        await _hub.Clients.Group(groupName)
            .DownloadFailed(downloadId, eventData);

        _logger.LogError("Sent DownloadFailed event for {DownloadId}: {Error}",
            downloadId, error);
    }

    /// <summary>
    /// Notifica clientes que download foi cancelado
    /// </summary>
    private async Task NotifyCancelledAsync(string downloadId)
    {
        var groupName = DownloadHub.GetDownloadGroupName(downloadId);

        await _hub.Clients.Group(groupName)
            .DownloadCancelled(downloadId);

        _logger.LogInformation("Sent DownloadCancelled event for {DownloadId}", downloadId);
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
                // 1. Notificar: Download Started
                await NotifyStartedAsync(downloadId, request);

                // 2. Inicializar status no repository
                await _statusRepository.AddAsync(new DownloadStatusRecord
                {
                    Id = downloadId,
                    CorrelationId = downloadId, // Set both Id and CorrelationId
                    Url = request.Url,
                    Status = DownloadStatus.Queued,
                    Progress = 0,
                    Speed = 0,
                    DownloadedBytes = 0,
                    TotalBytes = 0,
                    CreatedAt = DateTime.UtcNow
                }, stoppingToken);

                // 3. Criar progress reporter que:
                //    a) Atualiza repository
                //    b) Envia SignalR events
                var progress = new Progress<Domain.Models.DownloadProgress>(async p =>
                {
                    await UpdateProgressAsync(downloadId, p);
                });

                // 4. Executar download via Domain Service
                var result = await _downloadService.DownloadAsync(request, progress, stoppingToken);

                // 5. Notificar resultado
                if (result.IsFailed)
                {
                    await NotifyFailedAsync(downloadId, result.Errors.First().Message);
                }
                else
                {
                    await NotifyCompletedAsync(downloadId, result.Value);
                }
            }
            catch (OperationCanceledException)
            {
                await NotifyCancelledAsync(downloadId);
                _logger.LogInformation("Download {DownloadId} was cancelled", downloadId);
            }
            catch (Exception ex)
            {
                await NotifyFailedAsync(downloadId, ex.Message);
                _logger.LogError(ex, "Download {DownloadId} failed with exception", downloadId);
            }
            finally
            {
                // Cleanup throttle dictionary
                _lastProgressUpdate.TryRemove(downloadId, out _);
            }
        }
    }
}
