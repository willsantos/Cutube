using Cutube.Api.DTOs;
using Cutube.Api.Models;
using Cutube.Api.Services;
using Cutube.Domain.Models;
using Cutube.Domain.Services;
using FluentResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Cutube.Api.Endpoints;

public static class DownloadsEndpoints
{
    public static void MapDownloadsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/downloads")
            .WithTags("Downloads");

        // POST /api/downloads - Create download
        group.MapPost("/", async (
            [FromBody] CreateDownloadRequest request,
            IDownloadQueue downloadQueue,
            CancellationToken ct) =>
        {
            // 1. Validate request
            var validationResult = ValidateRequest(request);
            if (validationResult.IsFailed)
            {
                return Results.Problem(
                    detail: validationResult.Errors.First().Message,
                    statusCode: 400,
                    title: "Validation Error");
            }

            // 2. Convert to Domain Request
            var domainRequest = MapToDomainRequest(request);

            // 3. Enqueue (background processing)
            var downloadId = await downloadQueue.EnqueueAsync(domainRequest, ct);

            // 4. Return immediately (202 Accepted)
            var response = new CreateDownloadResponse
            {
                DownloadId = downloadId,
                Status = "queued",
                Message = "Download enqueued successfully"
            };

            return Results.Accepted($"/api/downloads/{downloadId}", response);
        })
        .WithName("CreateDownload")
        .Produces<CreateDownloadResponse>(202)
        .Produces<ProblemDetails>(400);

        // GET /api/downloads - List all downloads
        group.MapGet("/", async (
            [FromQuery] string? status,
            [FromQuery] int? limit,
            [FromQuery] int? offset,
            IDownloadStatusRepository statusRepository,
            CancellationToken ct) =>
        {
            // 1. Get all downloads
            var allDownloads = (await statusRepository.GetAllAsync(ct)).ToList();

            // 2. Filter by status (if provided)
            if (!string.IsNullOrEmpty(status))
            {
                allDownloads = allDownloads
                    .Where(d => d.Status.ToString().Equals(status, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            // 3. Pagination (if provided)
            var totalCount = allDownloads.Count;
            if (offset.HasValue)
                allDownloads = allDownloads.Skip(offset.Value).ToList();
            if (limit.HasValue)
                allDownloads = allDownloads.Take(limit.Value).ToList();

            // 4. Convert to DTOs
            var summaries = allDownloads.Select(d => new DownloadSummary
            {
                DownloadId = d.Id,
                Url = d.Url,
                Status = d.Status.ToString().ToLowerInvariant(),
                Progress = d.Progress,
                FilePath = d.FilePath,
                CreatedAt = d.CreatedAt
            }).ToArray();

            var response = new GetDownloadsResponse
            {
                Downloads = summaries,
                TotalCount = totalCount
            };

            return Results.Ok(response);
        })
        .WithName("GetDownloads")
        .Produces<GetDownloadsResponse>(200);

        // GET /api/downloads/{id} - Get download by ID
        group.MapGet("/{id}", async (
            string id,
            IDownloadStatusRepository statusRepository,
            CancellationToken ct) =>
        {
            // 1. Get download by ID
            var download = await statusRepository.GetByIdAsync(id, ct);

            if (download is null)
            {
                return Results.Problem(
                    detail: $"Download with ID '{id}' not found",
                    statusCode: 404,
                    title: "Not Found");
            }

            // 2. Convert to DTO
            var details = new DownloadDetails
            {
                DownloadId = download.Id,
                Url = download.Url,
                Status = download.Status.ToString().ToLowerInvariant(),
                Progress = download.Progress,
                Speed = download.Speed,
                Eta = null, // ETA - not currently tracked
                DownloadedBytes = download.DownloadedBytes ?? 0,
                TotalBytes = download.TotalBytes ?? 0,
                FilePath = download.FilePath,
                ErrorMessage = download.ErrorMessage,
                CreatedAt = download.CreatedAt,
                CompletedAt = download.CompletedAt
            };

            return Results.Ok(details);
        })
        .WithName("GetDownloadById")
        .Produces<DownloadDetails>(200)
        .Produces<ProblemDetails>(404);

        // DELETE /api/downloads/{id} - Delete/cancel download
        group.MapDelete("/{id}", async (
            string id,
            IDownloadStatusRepository statusRepository,
            IDownloadQueue downloadQueue,
            ILoggerFactory loggerFactory,
            CancellationToken ct) =>
        {
            var logger = loggerFactory.CreateLogger("DownloadsEndpoints");
            
            // 1. Check if download exists
            var download = await statusRepository.GetByIdAsync(id, ct);

            if (download is null)
            {
                return Results.Problem(
                    detail: $"Download with ID '{id}' not found",
                    statusCode: 404,
                    title: "Not Found");
            }

            // 2. Cancel download if in progress
            if (download.Status == DownloadStatus.Downloading ||
                download.Status == DownloadStatus.Queued)
            {
                await downloadQueue.CancelAsync(id, ct);
            }

            // 3. Remove from repository
            await statusRepository.DeleteAsync(id, ct);

            // 4. Delete partial files if exist
            if (!string.IsNullOrEmpty(download.FilePath) && File.Exists(download.FilePath))
            {
                try
                {
                    File.Delete(download.FilePath);
                }
                catch (Exception ex)
                {
                    // Log but don't fail the request
                    logger.LogWarning(ex, "Failed to delete file {FilePath}", download.FilePath);
                }
            }

            return Results.NoContent();
        })
        .WithName("DeleteDownload")
        .Produces(204)
        .Produces<ProblemDetails>(404);
    }

    private static Result ValidateRequest(CreateDownloadRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Url))
            return Result.Fail("URL is required");

        // Validate URL format
        if (!Uri.TryCreate(request.Url, UriKind.Absolute, out _))
            return Result.Fail("Invalid URL format");

        // Validate TimeRange if present
        if (!string.IsNullOrEmpty(request.StartTime) || !string.IsNullOrEmpty(request.EndTime))
        {
            if (!TimeSpan.TryParse(request.StartTime, out var start))
                return Result.Fail("Invalid StartTime format (expected HH:MM:SS)");

            if (!TimeSpan.TryParse(request.EndTime, out var end))
                return Result.Fail("Invalid EndTime format (expected HH:MM:SS)");

            if (start >= end)
                return Result.Fail("StartTime must be less than EndTime");
        }

        // Validate OutputPath
        if (string.IsNullOrWhiteSpace(request.OutputPath))
            return Result.Fail("OutputPath is required");

        return Result.Ok();
    }

    private static DownloadRequest MapToDomainRequest(CreateDownloadRequest apiRequest)
    {
        Domain.Models.TimeRange? timeRange = null;

        if (!string.IsNullOrEmpty(apiRequest.StartTime) && !string.IsNullOrEmpty(apiRequest.EndTime))
        {
            var start = TimeSpan.Parse(apiRequest.StartTime);
            var end = TimeSpan.Parse(apiRequest.EndTime);

            // Convert to domain TimeRange
            timeRange = new Domain.Models.TimeRange
            {
                StartSeconds = (int)start.TotalSeconds,
                EndSeconds = (int)end.TotalSeconds
            };
        }

        return new DownloadRequest
        {
            Url = apiRequest.Url,
            OutputPath = apiRequest.OutputPath,
            TimeRange = timeRange,
            AudioOnly = apiRequest.AudioOnly
        };
    }
}
