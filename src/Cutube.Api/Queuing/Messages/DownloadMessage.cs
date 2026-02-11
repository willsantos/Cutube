namespace Cutube.Api.Queuing.Messages;

/// <summary>
/// Mensagem de download para processamento assíncrono.
/// </summary>
public record DownloadMessage
{
    /// <summary>
    /// ID único da mensagem (gerado automaticamente).
    /// </summary>
    public string MessageId { get; init; } = Guid.NewGuid().ToString();

    /// <summary>
    /// ID de correlação para tracking end-to-end.
    /// </summary>
    public required string CorrelationId { get; init; }

    /// <summary>
    /// URL do vídeo para download.
    /// </summary>
    public required string Url { get; init; }

    /// <summary>
    /// Tempo de início (opcional, para recortes).
    /// Formato: "HH:MM:SS" ou "segundos".
    /// </summary>
    public string? StartTime { get; init; }

    /// <summary>
    /// Tempo de término (opcional, para recortes).
    /// Formato: "HH:MM:SS" ou "segundos".
    /// </summary>
    public string? EndTime { get; init; }

    /// <summary>
    /// Caminho de saída para o arquivo.
    /// </summary>
    public required string OutputPath { get; init; }

    /// <summary>
    /// Download apenas áudio.
    /// </summary>
    public bool AudioOnly { get; init; }

    /// <summary>
    /// Nome do arquivo de saída (opcional).
    /// </summary>
    public string? OutputFilename { get; init; }

    /// <summary>
    /// Prioridade da mensagem.
    /// </summary>
    public string Priority { get; init; } = "normal";

    /// <summary>
    /// Timestamp de criação da mensagem.
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Metadados adicionais (título, duração, thumbnail).
    /// </summary>
    public DownloadMetadata? Metadata { get; init; }

    /// <summary>
    /// Número de tentativas de processamento (para retry).
    /// </summary>
    public int RetryCount { get; init; } = 0;
}

/// <summary>
/// Metadados do vídeo.
/// </summary>
public record DownloadMetadata
{
    /// <summary>
    /// Título do vídeo.
    /// </summary>
    public string? Title { get; init; }

    /// <summary>
    /// Duração do vídeo.
    /// </summary>
    public string? Duration { get; init; }

    /// <summary>
    /// URL da thumbnail.
    /// </summary>
    public string? Thumbnail { get; init; }

    /// <summary>
    /// Nome do canal.
    /// </summary>
    public string? Channel { get; init; }
}
