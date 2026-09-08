using System.IO.Compression;

namespace Cutube.Infrastructure;

/// <summary>
/// Localiza o executável do FFmpeg (versão do usuário → bundle → PATH) e,
/// como último recurso no Windows, baixa e extrai um build estático oficial
/// (BtbN/FFmpeg-Builds) para a pasta de dados do usuário — o mesmo padrão
/// do YtDlpPathResolver
/// </summary>
public static class FfmpegLocator
{
    private const string BtbNLatestBaseUrl =
        "https://github.com/BtbN/FFmpeg-Builds/releases/latest/download/";

    /// <summary>
    /// Evita repetir um download grande que já falhou nesta execução
    /// </summary>
    private static bool _downloadFailed;

    /// <summary>
    /// Caminho do ffmpeg se existir em: LocalApplicationData/Cutube,
    /// diretório do app (ou bin/) ou PATH do sistema. Null se não encontrar
    /// </summary>
    public static string? Locate()
    {
        var exeName = OperatingSystem.IsWindows() ? "ffmpeg.exe" : "ffmpeg";

        // 1. Versão do usuário em LocalApplicationData
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var userPath = Path.Combine(appData, "Cutube", exeName);
        if (File.Exists(userPath))
            return userPath;

        // 2. Bundle do app (bin/ e raiz)
        var basePath = AppContext.BaseDirectory;
        var bundledPath = Path.Combine(basePath, "bin", exeName);
        if (File.Exists(bundledPath))
            return bundledPath;

        var bundledDirectPath = Path.Combine(basePath, exeName);
        if (File.Exists(bundledDirectPath))
            return bundledDirectPath;

        // 3. PATH do sistema
        var pathEnv = Environment.GetEnvironmentVariable("PATH") ?? "";
        var systemPath = pathEnv
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(folder => folder.Length > 0)
            .Select(folder => Path.Combine(folder.Trim('"'), exeName))
            .FirstOrDefault(File.Exists);

        return systemPath;
    }

    /// <summary>
    /// Garante um ffmpeg utilizável: tenta baixar o build estático oficial
    /// (apenas Windows, onde não há gerenciador de pacotes padrão) e retorna
    /// o caminho ou null. Falhas não são repetidas na mesma execução
    /// </summary>
    public static string? EnsureDownloaded()
    {
        if (!OperatingSystem.IsWindows() || _downloadFailed)
            return null;

        var url = GetDownloadUrl();
        if (url == null)
            return null;

        try
        {
            Console.WriteLine("FFmpeg não encontrado. Baixando build estático oficial (~120 MB, uma única vez)...");
            using var httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(15) };
            DownloadAndExtractAsync(httpClient, url, GetUserDirectory())
                .GetAwaiter()
                .GetResult();
            return Locate();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠ Aviso: falha ao baixar FFmpeg automaticamente: {ex.Message}");
            _downloadFailed = true;
            return null;
        }
    }

    /// <summary>
    /// URL do build estático latest compatível com a plataforma.
    /// Windows ARM32 e outros sistemas não têm asset de binário único → null
    /// </summary>
    public static string? GetDownloadUrl()
    {
        if (!OperatingSystem.IsWindows())
            return null;

        var arch = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture;
        return arch switch
        {
            System.Runtime.InteropServices.Architecture.Arm64 =>
                $"{BtbNLatestBaseUrl}ffmpeg-master-latest-winarm64-gpl.zip",
            // BtbN não publica x86 nem ARM32; o asset x64 não roda nesses processos
            System.Runtime.InteropServices.Architecture.X86 => null,
            System.Runtime.InteropServices.Architecture.Arm => null,
            _ => $"{BtbNLatestBaseUrl}ffmpeg-master-latest-win64-gpl.zip"
        };
    }

    /// <summary>
    /// Baixa o zip (streaming para não carregar ~120 MB em memória) e extrai
    /// ffmpeg.exe e ffprobe.exe de qualquer profundidade de pasta para
    /// <paramref name="targetDirectory"/>
    /// </summary>
    public static async Task DownloadAndExtractAsync(
        HttpClient httpClient, string zipUrl, string targetDirectory)
    {
        Directory.CreateDirectory(targetDirectory);

        var tempZip = Path.Combine(Path.GetTempPath(), $"cutube-ffmpeg-{Guid.NewGuid():N}.zip");
        try
        {
            using (var response = await httpClient.GetAsync(
                       zipUrl, HttpCompletionOption.ResponseHeadersRead))
            {
                response.EnsureSuccessStatusCode();
                await using var httpStream = await response.Content.ReadAsStreamAsync();
                await using var fileStream = File.Create(tempZip);
                await httpStream.CopyToAsync(fileStream);
            }

            using var archive = ZipFile.OpenRead(tempZip);
            var extracted = 0;
            foreach (var entry in archive.Entries)
            {
                var name = Path.GetFileName(entry.FullName);
                if (!name.Equals("ffmpeg.exe", StringComparison.OrdinalIgnoreCase) &&
                    !name.Equals("ffprobe.exe", StringComparison.OrdinalIgnoreCase))
                    continue;

                var targetPath = Path.Combine(targetDirectory, name);
                using var entryStream = entry.Open();
                await using var targetStream = File.Create(targetPath);
                await entryStream.CopyToAsync(targetStream);
                extracted++;
            }

            if (extracted == 0)
                throw new InvalidOperationException(
                    "o zip do FFmpeg não contém ffmpeg.exe/ffprobe.exe (estrutura do build mudou?)");
        }
        finally
        {
            try { if (File.Exists(tempZip)) File.Delete(tempZip); } catch { /* best-effort */ }
        }
    }

    private static string GetUserDirectory()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(appData, "Cutube");
    }
}
