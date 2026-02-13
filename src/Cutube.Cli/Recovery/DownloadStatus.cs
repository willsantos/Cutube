namespace Cutube.Cli.Recovery;

/// <summary>
/// Status de um download em andamento
/// </summary>
public enum DownloadStatus
{
    /// <summary>
    /// Download criado mas ainda não iniciado
    /// </summary>
    Pending,

    /// <summary>
    /// Baixando conteúdo do YouTube
    /// </summary>
    Downloading,

    /// <summary>
    /// Processando com FFmpeg (corte/extração áudio)
    /// </summary>
    Processing,

    /// <summary>
    /// Download concluído com sucesso
    /// </summary>
    Completed,

    /// <summary>
    /// Download falhou (pode ser retomado)
    /// </summary>
    Failed,

    /// <summary>
    /// Download cancelado pelo usuário (CTRL+C)
    /// </summary>
    Cancelled
}
