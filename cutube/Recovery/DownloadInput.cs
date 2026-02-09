namespace Cutube.Recovery;

/// <summary>
/// Parâmetros de entrada para operação de download
/// </summary>
public class DownloadInput
{
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
    public string? StartTime { get; set; }

    /// <summary>
    /// Tempo de fim do recorte (opcional)
    /// </summary>
    public string? EndTime { get; set; }

    /// <summary>
    /// Flag se é download de áudio apenas
    /// </summary>
    public bool AudioOnly { get; set; }
}
