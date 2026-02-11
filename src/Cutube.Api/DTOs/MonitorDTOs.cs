using System.Diagnostics.CodeAnalysis;
using Cutube.Api.Models;

namespace Cutube.Api.DTOs;

/// <summary>
/// Download metadata response
/// </summary>
[ExcludeFromCodeCoverage]
public class DownloadMetadataResponse
{
    public string? Title { get; init; }
    public string? Duration { get; init; }
    public string? Thumbnail { get; init; }
    public string? Channel { get; init; }
}

/// <summary>
/// Full download status response for monitor
/// </summary>
[ExcludeFromCodeCoverage]
public class DownloadStatusResponse
{
    public required string Id { get; init; }
    public required string CorrelationId { get; init; }
    public required string Url { get; init; }
    public required string State { get; init; }
    public int Progress { get; init; }
    public double Speed { get; init; }
    public long DownloadedBytes { get; init; }
    public long? TotalBytes { get; init; }
    public string? Eta { get; init; }
    public string? OutputPath { get; init; }
    public string? OutputFilename { get; init; }
    public bool AudioOnly { get; init; }
    public string? ErrorMessage { get; init; }
    public int RetryCount { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public DateTime? StartedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public TimeSpan? Duration { get; init; }
    public DownloadMetadataResponse? Metadata { get; init; }
}

/// <summary>
/// Download list response for monitor
/// </summary>
[ExcludeFromCodeCoverage]
public class DownloadListResponse
{
    public required IReadOnlyList<DownloadStatusResponse> Downloads { get; init; }
    public int TotalCount { get; init; }
}

/// <summary>
/// System metrics response
/// </summary>
[ExcludeFromCodeCoverage]
public class MetricsResponse
{
    public int TotalDownloads { get; init; }
    public int PendingCount { get; init; }
    public int QueuedCount { get; init; }
    public int ProcessingCount { get; init; }
    public int CompletedCount { get; init; }
    public int FailedCount { get; init; }
    public int DeadLetterCount { get; init; }
    public double AverageProcessingTimeSeconds { get; init; }
    public double ErrorRate { get; init; }
    public double ThroughputPerMinute { get; init; }
}

/// <summary>
/// Update status request (callback from worker)
/// </summary>
[ExcludeFromCodeCoverage]
public class UpdateStatusRequest
{
    public required string State { get; init; }
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// Update progress request (callback from worker)
/// </summary>
[ExcludeFromCodeCoverage]
public class UpdateProgressRequest
{
    public int Progress { get; init; }
    public double Speed { get; init; }
    public long DownloadedBytes { get; init; }
    public long? TotalBytes { get; init; }
    public string? Eta { get; init; }
}
