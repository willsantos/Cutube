using System.Text.RegularExpressions;

namespace cutube;

public static class ValidationHelper
{
    private static readonly string[] ValidYouTubeHosts = 
    [
        "youtube.com",
        "www.youtube.com",
        "m.youtube.com",
        "youtu.be"
    ];

    public static bool IsValidUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return false;

        if (uri.Scheme != "http" && uri.Scheme != "https")
            return false;

        var host = uri.Host.ToLowerInvariant();
        return ValidYouTubeHosts.Contains(host);
    }

    public static void ValidateUrl(string url)
    {
        if (!IsValidUrl(url))
        {
            throw new ArgumentException(
                "❌ URL inválida. Use uma URL do YouTube (youtube.com ou youtu.be) que comece com http:// ou https://");
        }
    }

    private static readonly char[] InvalidFileNameChars = ['<', '>', ':', '"', '|', '?', '*', '/'];

    public static bool IsValidFileName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return true;

        if (name.Contains('/') || name.Contains('\\'))
            return false;

        return !name.Any(c => InvalidFileNameChars.Contains(c));
    }

    public static void ValidateFileName(string name)
    {
        if (!string.IsNullOrWhiteSpace(name) && !IsValidFileName(name))
        {
            throw new ArgumentException(
                "❌ Nome do arquivo inválido. Não use caracteres especiais (<, >, :, \", |, ?, *, /, \\) ou caminhos de diretório.");
        }
    }

    public static bool IsValidTimeRange(int start, int end)
    {
        return start > 0 && end > start;
    }

    public static void ValidateTimeRange(string start, string end)
    {
        var startSeconds = TimeHelper.ParseToSeconds(start);
        var endSeconds = TimeHelper.ParseToSeconds(end);

        if (startSeconds <= 0)
        {
            throw new ArgumentException(
                $"❌ Tempo de início deve ser maior que zero. Use formatos como: 00:01:30, 1:30, 90s, 1h30m. Valor recebido: {start}");
        }

        if (endSeconds <= startSeconds)
        {
            throw new ArgumentException(
                $"❌ Tempo de fim deve ser maior que o tempo de início. Início: {start}s ({startSeconds}s), Fim: {end}s ({endSeconds}s)");
        }
    }

    public static bool IsDirectoryWritable(string path, IFileService fileService)
    {
        return fileService.DirectoryExists(path) && fileService.HasWritePermission(path);
    }

    public static void ValidateDirectory(string path, IFileService fileService)
    {
        if (!fileService.DirectoryExists(path))
        {
            throw new DirectoryNotFoundException(
                $"❌ Diretório não encontrado: '{path}'. Verifique se o caminho está correto ou crie o diretório antes de continuar.");
        }

        if (!fileService.HasWritePermission(path))
        {
            throw new UnauthorizedAccessException(
                $"❌ Sem permissão de escrita em '{path}'. Escolha outro diretório ou verifique as permissões.");
        }
    }
}
