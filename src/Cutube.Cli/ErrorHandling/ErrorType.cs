namespace Cutube.ErrorHandling;

public enum ErrorType
{
    Unknown,
    Network,           // Erro de conexão (retry)
    FileSystem,        // Erro de disco (log & continue)
    Validation,        // Input inválido (user error)
    DependencyMissing, // yt-dlp/ffmpeg não encontrado (offer solution)
    Critical           // Erro fatal (graceful shutdown)
}
