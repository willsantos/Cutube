using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using YoutubeDLSharp;
using YoutubeDLSharp.Options;

namespace cutube;

/// <summary>
/// Helper para interagir com yt-dlp (YouTube downloader)
/// Implementa bundling, auto-update e fallback robusto
/// </summary>
public class YtDlpHelper
{
    private YoutubeDL _ytdl;
    private readonly string _bundledPath;
    private readonly string _userPath;
    private readonly string _systemPath;

    public YtDlpHelper()
    {
        _bundledPath = GetBundledPath();
        _userPath = GetUserPath();
        _systemPath = GetSystemPath();
        
        _ytdl = new YoutubeDL
        {
            YoutubeDLPath = ResolveYtDlpPath()
        };
        
        // Auto-update em background (não bloqueia inicialização)
        Task.Run(async () => await UpdateYtDlpIfNeeded());
    }

    #region Path Resolution

    /// <summary>
    /// Caminho do yt-dlp bundleado com o app
    /// </summary>
    private string GetBundledPath()
    {
        var basePath = AppContext.BaseDirectory;
        
        if (OperatingSystem.IsWindows())
            return Path.Combine(basePath, "yt-dlp.exe");
        
        if (OperatingSystem.IsLinux())
            return Path.Combine(basePath, "yt-dlp");
        
        if (OperatingSystem.IsMacOS())
            return Path.Combine(basePath, "yt-dlp");
        
        throw new PlatformNotSupportedException();
    }

    /// <summary>
    /// Caminho para versão do usuário em AppData/Local
    /// </summary>
    private string GetUserPath()
    {
        var appData = Environment.GetFolderPath(
            Environment.SpecialFolder.LocalApplicationData
        );
        var folder = Path.Combine(appData, "Cutube");
        Directory.CreateDirectory(folder);
        
        if (OperatingSystem.IsWindows())
            return Path.Combine(folder, "yt-dlp.exe");
        
        return Path.Combine(folder, "yt-dlp");
    }

    /// <summary>
    /// Tenta encontrar yt-dlp no PATH do sistema
    /// </summary>
    private string GetSystemPath()
    {
        var pathEnv = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrEmpty(pathEnv))
            return null!;
        
        var exeName = OperatingSystem.IsWindows() ? "yt-dlp.exe" : "yt-dlp";
        
        return pathEnv.Split(Path.PathSeparator)
            .Select(folder => Path.Combine(folder, exeName))
            .FirstOrDefault(File.Exists);
    }

    /// <summary>
    /// Resolve qual versão do yt-dlp usar (prioridade: user > bundle > system)
    /// </summary>
    private string ResolveYtDlpPath()
    {
        // 1. Versão do usuário (se existe e é recente)
        if (File.Exists(_userPath) && IsRecentVersion(_userPath))
        {
            Console.WriteLine($"Usando yt-dlp do usuário: {_userPath}");
            return _userPath;
        }
        
        // 2. Bundle do app
        if (File.Exists(_bundledPath))
        {
            Console.WriteLine($"Usando yt-dlp bundleado: {_bundledPath}");
            return _bundledPath;
        }
        
        // 3. PATH do sistema
        if (!string.IsNullOrEmpty(_systemPath))
        {
            Console.WriteLine($"Usando yt-dlp do sistema: {_systemPath}");
            return _systemPath;
        }
        
        // 4. Download automático
        Console.WriteLine("yt-dlp não encontrado. Baixando automaticamente...");
        Task.Run(async () => await DownloadLatestVersion(_userPath)).Wait();
        
        if (File.Exists(_userPath))
            return _userPath;
        
        throw new Exception(
            "Não foi possível encontrar ou baixar yt-dlp. " +
            "Instale manualmente em: https://github.com/yt-dlp/yt-dlp/releases"
        );
    }

    /// <summary>
    /// Verifica se o arquivo é recente (< 7 dias)
    /// </summary>
    private bool IsRecentVersion(string path)
    {
        var fileInfo = new FileInfo(path);
        return (DateTime.Now - fileInfo.LastWriteTime).TotalDays < 7;
    }

    #endregion

    #region Auto-Update

    /// <summary>
    /// Verifica e baixa atualizações do yt-dlp em background
    /// </summary>
    private async Task UpdateYtDlpIfNeeded()
    {
        try
        {
            var currentVersion = await GetCurrentVersion();
            var latestVersion = await GetLatestVersion();
            
            if (currentVersion == latestVersion)
            {
                Console.WriteLine("✓ yt-dlp está atualizado ({currentVersion})");
                return;
            }

            Console.WriteLine($"Atualizando yt-dlp: {currentVersion} → {latestVersion}");
            await DownloadLatestVersion(_userPath);
            Console.WriteLine("✓ yt-dlp atualizado com sucesso!");
            
            // Atualizar referência
            _ytdl = new YoutubeDL { YoutubeDLPath = _userPath };
        }
        catch (Exception ex)
        {
            // Não falhar se update falhar (usar bundle)
            Console.WriteLine($"⚠ Aviso: Não foi possível atualizar yt-dlp: {ex.Message}");
        }
    }

    /// <summary>
    /// Obtém versão atual do yt-dlp
    /// </summary>
    private async Task<string> GetCurrentVersion()
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = _ytdl.YoutubeDLPath,
                    Arguments = "--version",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            return output.Trim();
        }
        catch
        {
            return "unknown";
        }
    }

    /// <summary>
    /// Consulta GitHub API para última versão do yt-dlp
    /// </summary>
    private async Task<string> GetLatestVersion()
    {
        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("User-Agent", "Cutube");
        
        var response = await client.GetStringAsync(
            "https://api.github.com/repos/yt-dlp/yt-dlp/releases/latest"
        );
        
        using var json = JsonDocument.Parse(response);
        var tagName = json.RootElement.GetProperty("tag_name").GetString();
        
        return tagName ?? "unknown";
    }

    /// <summary>
    /// Download do yt-dlp mais recente
    /// </summary>
    private async Task DownloadLatestVersion(string targetPath)
    {
        var platform = GetPlatformIdentifier();
        var url = $"https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp{platform}";
        
        using var client = new HttpClient();
        var data = await client.GetByteArrayAsync(url);
        await File.WriteAllBytesAsync(targetPath, data);
        
        // Tornar executável (Linux/macOS)
        if (!OperatingSystem.IsWindows())
        {
            var chmod = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "chmod",
                    Arguments = $"+x \"{targetPath}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            chmod.Start();
            await chmod.WaitForExitAsync();
        }
    }

    /// <summary>
    /// Identifica sufixo do binário para plataforma atual
    /// </summary>
    private string GetPlatformIdentifier()
    {
        // Detecção de arquitetura
        var arch = RuntimeInformation.OSArchitecture;
        
        if (OperatingSystem.IsWindows())
        {
            return arch switch
            {
                Architecture.X64 => "_x64.exe",
                Architecture.X86 => "_x86.exe",
                Architecture.Arm64 => "_arm64.exe",
                _ => "_x64.exe"
            };
        }
        
        if (OperatingSystem.IsLinux())
        {
            return arch switch
            {
                Architecture.X64 => "_linux",
                Architecture.Arm64 => "_linux_aarch64",
                Architecture.Arm => "_linux_armv7l",
                _ => "_linux"
            };
        }
        
        if (OperatingSystem.IsMacOS())
        {
            return arch switch
            {
                Architecture.X64 => "_macos",
                Architecture.Arm64 => "_macos_arm64",
                _ => "_macos"
            };
        }
        
        return "";
    }

    #endregion

    #region Public API

    /// <summary>
    /// Obtém título do vídeo e sanitiza para uso como filename
    /// </summary>
    public async Task<string> GetVideoTitleAsync(string url)
    {
        var result = await _ytdl.RunVideoDataFetch(url);
        var title = result.Data.Title;
        return TitleHelper.FormatTitle(title ?? "video");
    }

    /// <summary>
    /// Download completo do vídeo (melhor qualidade)
    /// </summary>
    public async Task DownloadAsync(
        string url, 
        string outputFile,
        IProgress<DownloadProgress>? progress = null)
    {
        // Download com melhor qualidade (video + audio mesclado em MP4)
        var options = new OptionSet
        {
            Format = "bestvideo+bestaudio",
            MergeOutputFormat = DownloadMergeFormat.Mp4,
            Output = outputFile
        };
        
        var res = await _ytdl.RunVideoDownload(url, overrideOptions: options, progress: progress);
    }

    /// <summary>
    /// Download com recorte de tempo (download completo + FFmpeg)
    /// </summary>
    public async Task DownloadWithTimeRangeAsync(
        string url,
        string outputFile,
        string startTime,
        string endTime,
        IProgress<DownloadProgress>? progress = null)
    {
        // Download completo primeiro
        var tempFile = Path.GetTempFileName() + ".mp4";
        
        Console.WriteLine("Baixando vídeo completo...");
        await DownloadAsync(url, tempFile, progress);
        
        // Usar FFmpeg para corte
        var timeStart = TimeHelper.GetStartSeconds(startTime);
        var timeEnd = TimeHelper.GetEndSeconds(endTime);
        var duration = timeEnd - timeStart;
        
        Console.WriteLine($"Cortando vídeo ({startTime} - {endTime})...");
        
        var ffmpeg = new FfmpegHelper();
        var arguments =
            $"-i \"{tempFile}\" " +
            $"-ss {timeStart} " +
            $"-t {duration} " +
            $"-c:v libx264 -c:a aac " +
            $"\"{outputFile}\"";
        
        ffmpeg.ExecuteFfmpeg(arguments, new ProgressBar());
        
        // Limpar temp
        File.Delete(tempFile);
    }

    #endregion
}
