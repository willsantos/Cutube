namespace Cutube.Contracts.Messages;

public record DownloadMessage
{
    public string MessageId { get; init; } = Guid.NewGuid().ToString();

    public required string CorrelationId { get; init; }

    public required string Url { get; init; }

    public string? StartTime { get; init; }

    public string? EndTime { get; init; }

    public required string OutputPath { get; init; }

    public bool AudioOnly { get; init; }

    public string? OutputFilename { get; init; }

    public string Priority { get; init; } = "normal";

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    public DownloadMetadata? Metadata { get; init; }

    public int RetryCount { get; init; } = 0;
}

public record DownloadMetadata
{
    public string? Title { get; init; }

    public string? Duration { get; init; }

    public string? Thumbnail { get; init; }

    public string? Channel { get; init; }
}
