using Cutube.Api.DTOs;
using Cutube.Api.Hubs;
using Cutube.Api.Models;
using Cutube.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace Cutube.Api.Endpoints;

/// <summary>
/// Status tracking endpoints for monitor dashboard
/// </summary>
public static class StatusEndpoints
{
    public static void MapStatusEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/downloads")
            .WithTags("Status");

        // GET /api/downloads/queue/active - Active downloads (queued + processing)
        group.MapGet("/queue/active", async (
            IDownloadStatusRepository repository,
            CancellationToken ct) =>
        {
            var downloads = await repository.GetByStatesAsync(
                new[] { DownloadStatus.Queued, DownloadStatus.Downloading, DownloadStatus.Processing },
                cancellationToken: ct);

            return Results.Ok(new DownloadListResponse
            {
                Downloads = downloads.Select(MapToResponse).ToList(),
                TotalCount = downloads.Count
            });
        })
        .WithName("GetActiveDownloads")
        .WithSummary("Get active downloads (queued and processing)");

        // GET /api/downloads/queue/failed - Failed downloads
        group.MapGet("/queue/failed", async (
            IDownloadStatusRepository repository,
            CancellationToken ct) =>
        {
            var downloads = await repository.GetByStatesAsync(
                new[] { DownloadStatus.Failed, DownloadStatus.DeadLetter },
                cancellationToken: ct);

            return Results.Ok(new DownloadListResponse
            {
                Downloads = downloads.Select(MapToResponse).ToList(),
                TotalCount = downloads.Count
            });
        })
        .WithName("GetFailedDownloads")
        .WithSummary("Get failed and dead letter downloads");

         // PATCH /api/downloads/{correlationId}/status - Worker callback
         group.MapPatch("/{correlationId}/status", async (
             string correlationId,
             [FromBody] UpdateStatusRequest request,
             IDownloadStatusRepository repository,
             IHubContext<DownloadHub, IDownloadHubClient> hub,
             CancellationToken ct) =>
         {
             if (!Enum.TryParse<DownloadStatus>(request.State, true, out var newState))
             {
                 return Results.BadRequest(new { error = "Invalid state" });
             }

             var download = await repository.GetByCorrelationIdAsync(correlationId, ct);
             if (download == null)
             {
                 return Results.NotFound(new { error = "Download not found", correlationId });
             }

             await repository.UpdateAsync(correlationId, s =>
             {
                 s.Status = newState;
                 s.ErrorMessage = request.ErrorMessage;

                 if (request.RetryCount.HasValue)
                 {
                     s.RetryCount = request.RetryCount.Value;
                 }

                 if (newState == DownloadStatus.Downloading && s.StartedAt.HasValue == false)
                 {
                     s.StartedAt = DateTime.UtcNow;
                 }

                 if (newState is DownloadStatus.Completed or DownloadStatus.Failed or DownloadStatus.Cancelled or DownloadStatus.DeadLetter)
                 {
                     s.CompletedAt = DateTime.UtcNow;
                 }
             }, ct);

             // Notify via SignalR
             var groupName = DownloadHub.GetDownloadGroupName(correlationId);
             await hub.Clients.Group(groupName)
                 .DownloadStatusChanged(correlationId, request.State);

             return Results.NoContent();
          })
          .WithName("UpdateDownloadStatus")
          .WithSummary("Update download status (worker callback)");

        // POST /api/downloads/{correlationId}/progress - Worker progress callback
        group.MapPost("/{correlationId}/progress", async (
            string correlationId,
            [FromBody] UpdateProgressRequest request,
            IDownloadStatusRepository repository,
            IHubContext<DownloadHub, IDownloadHubClient> hub,
            CancellationToken ct) =>
        {
            var download = await repository.GetByCorrelationIdAsync(correlationId, ct);
            if (download == null)
            {
                return Results.NotFound(new { error = "Download not found", correlationId });
            }

            await repository.UpdateDetailedProgressAsync(
                correlationId,
                request.Progress,
                request.Speed,
                request.DownloadedBytes,
                ct);

            // Notify via SignalR
            var groupName = DownloadHub.GetDownloadGroupName(correlationId);
            var progressEvent = new Cutube.Api.Hubs.DownloadProgressEvent(
                correlationId,
                request.Progress,
                request.Speed,
                request.Eta,
                request.DownloadedBytes,
                request.TotalBytes ?? 0,
                "downloading"
            );
            await hub.Clients.Group(groupName)
                .DownloadProgress(correlationId, progressEvent);

            return Results.NoContent();
        })
        .WithName("UpdateDownloadProgress")
        .WithSummary("Update download progress (worker callback)");

        // GET /api/metrics - System metrics
        app.MapGet("/api/metrics", async (
            IDownloadStatusRepository repository,
            CancellationToken ct) =>
        {
            var stats = await repository.GetStatsAsync(ct);

            // Calculate throughput (downloads completed in the last hour)
            var recentCompleted = (await repository.GetAllAsync(DownloadStatus.Completed, cancellationToken: ct))
                .Where(d => d.CompletedAt.HasValue && d.CompletedAt.Value > DateTime.UtcNow.AddHours(-1))
                .ToList();

            var throughput = recentCompleted.Any()
                ? recentCompleted.Count / 60.0
                : 0;

            var response = new MetricsResponse
            {
                TotalDownloads = stats.TotalDownloads,
                PendingCount = stats.PendingCount,
                QueuedCount = stats.QueuedCount,
                ProcessingCount = stats.ProcessingCount,
                CompletedCount = stats.CompletedCount,
                FailedCount = stats.FailedCount,
                DeadLetterCount = stats.DeadLetterCount,
                AverageProcessingTimeSeconds = stats.AverageProcessingTimeSeconds,
                ErrorRate = stats.ErrorRate,
                ThroughputPerMinute = throughput
            };

            return Results.Ok(response);
        })
        .WithName("GetMetrics")
        .WithSummary("Get system metrics");
    }

    private static DownloadStatusResponse MapToResponse(DownloadStatusRecord status)
    {
        return new DownloadStatusResponse
        {
            Id = status.Id,
            CorrelationId = string.IsNullOrEmpty(status.CorrelationId) ? status.Id : status.CorrelationId,
            Url = status.Url,
            State = status.Status.ToString().ToLowerInvariant(),
            Progress = (int)status.Progress,
            Speed = status.Speed,
            DownloadedBytes = status.DownloadedBytes,
            TotalBytes = status.TotalBytes,
            Eta = status.Eta?.ToString(),
            OutputPath = status.OutputPath ?? status.FilePath,
            OutputFilename = status.OutputFilename,
            AudioOnly = status.AudioOnly,
            ErrorMessage = status.ErrorMessage,
            RetryCount = status.RetryCount,
            CreatedAt = status.CreatedAt,
            UpdatedAt = status.UpdatedAt,
            StartedAt = status.StartedAt,
            CompletedAt = status.CompletedAt,
            Duration = status.Duration,
            Metadata = status.Metadata == null ? null : new DownloadMetadataResponse
            {
                Title = status.Metadata.Title,
                Duration = status.Metadata.Duration,
                Thumbnail = status.Metadata.Thumbnail,
                Channel = status.Metadata.Channel
            }
        };
    }
}
