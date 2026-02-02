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
public class YtDlpHelper : IYtDlpService, IDisposable
{
    private YoutubeDL _ytdl;
    private readonly string _bundledPath;
    private readonly string _userPath;
    private readonly string? _systemPath;
    
    // Dependencies for testability
    private readonly IFileService _fileService;
    private readonly IHttpClientService? _httpClientService;
    private readonly IEnvironmentService _environmentService;
    private readonly IProcessService _processService;
    private readonly IConsoleService _consoleService;
    private readonly bool _skipAutoUpdate;

    // Constructor for production use
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public YtDlpHelper() : this(
        new FileService(),
        new HttpClientService(),
        new EnvironmentService(),
        new ProcessService(),
        new ConsoleService(),
        false
    )
    {
    }

    // Constructor for testing (dependency injection)
    public YtDlpHelper(
        IFileService fileService,
        IHttpClientService? httpClientService,
        IEnvironmentService environmentService,
        IProcessService processService,
        IConsoleService consoleService,
        bool skipAutoUpdate = false
    )
    {
        _fileService = fileService;
        _httpClientService = httpClientService;
        _environmentService = environmentService;
        _processService = processService;
        _consoleService = consoleService;
        _skipAutoUpdate = skipAutoUpdate;

        _bundledPath = GetBundledPath();
        _userPath = GetUserPath();
        _systemPath = GetSystemPath();
        
        _ytdl = new YoutubeDL
        {
            YoutubeDLPath = ResolveYtDlpPath()
        };
        
        // Auto-update em background (não bloqueia inicialização)
        if (!_skipAutoUpdate)
        {
            Task.Run(async () => await UpdateYtDlpIfNeeded());
        }
    }

    #region Path Resolution

    /// <summary>
    /// Caminho do yt-dlp bundleado com o app
    /// </summary>
    protected internal virtual string GetBundledPath()
    {
        var basePath = AppContext.BaseDirectory;
        
        if (_environmentService.IsWindows())
            return Path.Combine(basePath, "yt-dlp.exe");
        
        if (_environmentService.IsLinux())
            return Path.Combine(basePath, "yt-dlp");
        
        if (_environmentService.IsMacOS())
            return Path.Combine(basePath, "yt-dlp");
        
        throw new PlatformNotSupportedException();
    }

    /// <summary>
    /// Caminho para versão do usuário em AppData/Local
    /// </summary>
    protected internal virtual string GetUserPath()
    {
        var appData = _environmentService.GetFolderPath(
            Environment.SpecialFolder.LocalApplicationData
        );
        var folder = Path.Combine(appData, "Cutube");
        Directory.CreateDirectory(folder);
        
        if (_environmentService.IsWindows())
            return Path.Combine(folder, "yt-dlp.exe");
        
        return Path.Combine(folder, "yt-dlp");
    }

    /// <summary>
    /// Tenta encontrar yt-dlp no PATH do sistema
    /// </summary>
    protected internal virtual string? GetSystemPath()
    {
        var pathEnv = _environmentService.GetEnvironmentVariable("PATH");
        if (string.IsNullOrEmpty(pathEnv))
            return null;
        
        var exeName = _environmentService.IsWindows() ? "yt-dlp.exe" : "yt-dlp";
        
        return pathEnv.Split(Path.PathSeparator)
            .Select(folder => Path.Combine(folder, exeName))
            .FirstOrDefault(path => _fileService.Exists(path));
    }

    /// <summary>
    /// Resolve qual versão do yt-dlp usar (prioridade: user > bundle > system)
    /// </summary>
    protected internal virtual string ResolveYtDlpPath()
    {
        // 1. Versão do usuário (se existe e é recente)
        if (_fileService.Exists(_userPath) && IsRecentVersion(_userPath))
        {
            _consoleService.WriteLine($"Usando yt-dlp do usuário: {_userPath}");
            return _userPath;
        }
        
        // 2. Bundle do app
        if (_fileService.Exists(_bundledPath))
        {
            _consoleService.WriteLine($"Usando yt-dlp bundleado: {_bundledPath}");
            return _bundledPath;
        }
        
        // 3. PATH do sistema
        if (!string.IsNullOrEmpty(_systemPath))
        {
            _consoleService.WriteLine($"Usando yt-dlp do sistema: {_systemPath}");
            return _systemPath;
        }
        
        // 4. Download automático
        _consoleService.WriteLine("yt-dlp não encontrado. Baixando automaticamente...");
        Task.Run(async () => await DownloadLatestVersion(_userPath)).Wait();
        
        if (_fileService.Exists(_userPath))
            return _userPath;
        
        throw new Exception(
            "Não foi possível encontrar ou baixar yt-dlp. " +
            "Instale manualmente em: https://github.com/yt-dlp/yt-dlp/releases"
        );
    }

    /// <summary>
    /// Verifica se o arquivo é recente (< 7 dias)
    /// </summary>
    protected internal virtual bool IsRecentVersion(string path)
    {
        var lastWrite = _fileService.GetLastWriteTime(path);
        return (DateTime.Now - lastWrite).TotalDays < 7;
    }

    #endregion

    #region Auto-Update

    /// <summary>
    /// Verifica e baixa atualizações do yt-dlp em background
    /// </summary>
    internal async Task UpdateYtDlpIfNeeded()
    {
        try
        {
            if (_httpClientService == null)
            {
                _consoleService.WriteLine("⚠ Aviso: HttpClient não configurado, pulando auto-update");
                return;
            }

            var currentVersion = await GetCurrentVersion();
            var latestVersion = await GetLatestVersion();
            
            if (currentVersion == latestVersion)
            {
                return;
            }

            await DownloadLatestVersion(_userPath);
            
            // Atualizar referência
            _ytdl = new YoutubeDL { YoutubeDLPath = _userPath };
        }
        catch (Exception ex)
        {
            // Não falhar se update falhar (usar bundle)
            _consoleService.WriteLine($"⚠ Aviso: Não foi possível atualizar yt-dlp: {ex.Message}");
        }
    }

    /// <summary>
    /// Obtém versão atual do yt-dlp
    /// </summary>
    protected internal virtual async Task<string> GetCurrentVersion()
    {
        try
        {
            var process = _processService.Start(new ProcessStartInfo
            {
                FileName = _ytdl.YoutubeDLPath,
                Arguments = "--version",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            });

            var output = await process.StandardOutputReadToEndAsync();
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
    protected internal virtual async Task<string> GetLatestVersion()
    {
        if (_httpClientService == null)
            return "unknown";

        _httpClientService.DefaultRequestHeadersAdd("User-Agent", "Cutube");
        
        var response = await _httpClientService.GetStringAsync(
            "https://api.github.com/repos/yt-dlp/yt-dlp/releases/latest"
        );
        
        using var json = JsonDocument.Parse(response);
        var tagName = json.RootElement.GetProperty("tag_name").GetString();
        
        return tagName ?? "unknown";
    }

    /// <summary>
    /// Download do yt-dlp mais recente
    /// </summary>
    protected internal virtual async Task DownloadLatestVersion(string targetPath)
    {
        if (_httpClientService == null)
            throw new InvalidOperationException("HttpClient não configurado");

        var platform = GetPlatformIdentifier();
        var url = $"https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp{platform}";
        
        var data = await _httpClientService.GetByteArrayAsync(url);
        await _fileService.WriteAllBytesAsync(targetPath, data);
        
        // Tornar executável (Linux/macOS)
        if (!_environmentService.IsWindows())
        {
            var chmod = _processService.Start(new ProcessStartInfo
            {
                FileName = "chmod",
                Arguments = $"+x \"{targetPath}\"",
                UseShellExecute = false,
                CreateNoWindow = true
            });

            await chmod.WaitForExitAsync();
        }
    }

    /// <summary>
    /// Identifica sufixo do binário para plataforma atual
    /// </summary>
    protected internal string GetPlatformIdentifier()
    {
        // Detecção de arquitetura
        var archStr = _environmentService.OSArchitecture;
        Enum.TryParse<Architecture>(archStr, out var arch);
        
        if (_environmentService.IsWindows())
        {
            return arch switch
            {
                Architecture.X64 => "_x64.exe",
                Architecture.X86 => "_x86.exe",
                Architecture.Arm64 => "_arm64.exe",
                _ => "_x64.exe"
            };
        }
        
        if (_environmentService.IsLinux())
        {
            return arch switch
            {
                Architecture.X64 => "_linux",
                Architecture.Arm64 => "_linux_aarch64",
                Architecture.Arm => "_linux_armv7l",
                _ => "_linux"
            };
        }
        
        if (_environmentService.IsMacOS())
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
        var title = await FetchVideoTitleRawAsync(url);
        return TitleHelper.FormatTitle(title ?? "video");
    }

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    protected internal virtual async Task<string?> FetchVideoTitleRawAsync(string url)
    {
        var result = await _ytdl.RunVideoDataFetch(url);
        return result.Data.Title;
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
        
        await RunVideoDownloadAsync(url, options, progress);
    }

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    protected internal virtual Task RunVideoDownloadAsync(
        string url,
        OptionSet options,
        IProgress<DownloadProgress>? progress)
    {
        return _ytdl.RunVideoDownload(url, overrideOptions: options, progress: progress);
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
        var tempFile = CreateTempFile();
        
        _consoleService.WriteLine("Baixando vídeo completo...");
        await DownloadAsync(url, tempFile, progress);
        
        // Usar FFmpeg para corte
        var timeStart = TimeHelper.GetStartSeconds(startTime);
        var timeEnd = TimeHelper.GetEndSeconds(endTime);
        var duration = timeEnd - timeStart;
        
        _consoleService.WriteLine($"Cortando vídeo ({startTime} - {endTime})...");
        
        var ffmpeg = new FfmpegHelper();
        var arguments =
            $"-i \"{tempFile}\" " +
            $"-ss {timeStart} " +
            $"-t {duration} " +
            $"-c:v libx264 -c:a aac " +
            $"\"{outputFile}\"";
        
        ExecuteFfmpeg(arguments);
        
        // Limpar temp
        _fileService.Delete(tempFile);
    }

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    protected internal virtual string CreateTempFile()
    {
        return Path.GetTempFileName() + ".mp4";
    }

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    protected internal virtual void ExecuteFfmpeg(string arguments)
    {
        var ffmpeg = new FfmpegHelper();
        ffmpeg.ExecuteFfmpeg(arguments, new ProgressBar());
    }

    #endregion

    public void Dispose()
    {
        _httpClientService?.Dispose();
    }
}
