using System.Text.Json.Serialization;

namespace Cutube.Recovery;

/// <summary>
/// Estado persistente de um download para recuperação
/// </summary>
public class DownloadState
{
    /// <summary>
    /// Identificador único do estado (GUID)
    /// </summary>
    public string StateId { get; init; } = Guid.NewGuid().ToString();

    /// <summary>
    /// URL do vídeo do YouTube
    /// </summary>
    public required string Url { get; set; }

    /// <summary>
    /// Caminho de saída final do arquivo
    /// </summary>
    public required string OutputPath { get; set; }

    /// <summary>
    /// Tempo de início do recorte (opcional)
    /// </summary>
    public TimeSpan? StartTime { get; set; }

    /// <summary>
    /// Tempo de fim do recorte (opcional)
    /// </summary>
    public TimeSpan? EndTime { get; set; }

    /// <summary>
    /// Flag se é download de áudio apenas
    /// </summary>
    public bool AudioOnly { get; set; }

    /// <summary>
    /// Status atual do download
    /// </summary>
    public DownloadStatus Status { get; set; }

    /// <summary>
    /// Progresso atual (0-100)
    /// </summary>
    public int ProgressPercent { get; set; }

    /// <summary>
    /// Caminho do arquivo temporário (se existir)
    /// </summary>
    public string? TempFilePath { get; set; }

    /// <summary>
    /// Mensagem de erro (se falhou)
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Data/hora de criação do estado
    /// </summary>
    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Data/hora da última atualização
    /// </summary>
    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Duração total do download em segundos (se conhecido)
    /// </summary>
    public int? TotalDurationSeconds { get; set; }

    /// <summary>
    /// Tamanho baixado em bytes
    /// </summary>
    public long DownloadedBytes { get; set; }

    /// <summary>
    /// Tamanho total em bytes (se conhecido)
    /// </summary>
    public long? TotalBytes { get; set; }
}
