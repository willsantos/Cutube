using Cutube.Api.DTOs;
using Cutube.Domain.Services;
using FluentResults;
using Microsoft.AspNetCore.Mvc;

namespace Cutube.Api.Endpoints;

public static class VideosEndpoints
{
    public static void MapVideosEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/videos")
            .WithTags("Videos");

        // GET /api/videos/info - Get video metadata
        group.MapGet("/info", async (
            [FromQuery] string url,
            IMetadataService metadataService,
            CancellationToken ct) =>
        {
            // 1. Validate URL
            if (string.IsNullOrWhiteSpace(url))
            {
                return Results.Problem(
                    detail: "URL parameter is required",
                    statusCode: 400,
                    title: "Bad Request");
            }

            // 2. Get metadata via Domain
            Result<Domain.Models.VideoMetadata> metadataResult = await metadataService.GetMetadataAsync(url, ct);

            if (metadataResult.IsFailed)
            {
                return Results.Problem(
                    detail: metadataResult.Errors.First().Message,
                    statusCode: 400,
                    title: "Bad Request");
            }

            var metadata = metadataResult.Value;

            // 3. Convert to DTO
            var response = new VideoInfoResponse
            {
                Url = metadata.Url,
                Title = metadata.Title,
                Uploader = metadata.Uploader,
                Duration = metadata.Duration.ToString(@"hh\:mm\:ss"),
                ThumbnailUrl = metadata.ThumbnailUrl,
                UploadDate = metadata.UploadDate
            };

            return Results.Ok(response);
        })
        .WithName("GetVideoInfo")
        .Produces<VideoInfoResponse>(200)
        .Produces<ProblemDetails>(400);
    }
}
