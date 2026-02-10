// src/Cutube.Api/Hubs/DownloadHubEvents.cs
namespace Cutube.Api.Hubs;

/// <summary>
/// Evento disparado quando download inicia
/// </summary>
public record DownloadStartedEvent(
    string DownloadId,
    string Url,
    DateTime StartedAt
);

/// <summary>
/// Evento disparado periodicamente com progresso
/// </summary>
public record DownloadProgressEvent(
    string DownloadId,
    double Progress,          // 0-100
    double Speed,             // bytes/s
    string? Eta,              // HH:MM:SS format
    long DownloadedBytes,
    long TotalBytes,
    string Status             // "downloading", "processing"
);

/// <summary>
/// Evento disparado quando download completa com sucesso
/// </summary>
public record DownloadCompletedEvent(
    string DownloadId,
    string FilePath,
    long Size,
    TimeSpan Duration,
    DateTime CompletedAt
);

/// <summary>
/// Evento disparado quando download falha
/// </summary>
public record DownloadFailedEvent(
    string DownloadId,
    string Error,
    DateTime FailedAt
);
