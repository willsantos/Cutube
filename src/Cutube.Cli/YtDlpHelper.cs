using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using YoutubeDLSharp;
using YoutubeDLSharp.Options;
using Cutube.Cli.Logging;
using Cutube.Cli.ErrorHandling;
using Cutube.Cli.Recovery;
using YtdlDownloadState = YoutubeDLSharp.DownloadState;

namespace Cutube.Cli;

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
    private readonly IErrorHandler? _errorHandler;
    private readonly ILoggerService? _logger;
    private readonly IDownloadStateManager? _stateManager;

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
        bool skipAutoUpdate = false,
        IErrorHandler? errorHandler = null,
        ILoggerService? logger = null,
        IDownloadStateManager? stateManager = null
    )
    {
        _fileService = fileService;
        _httpClientService = httpClientService;
        _environmentService = environmentService;
        _processService = processService;
        _consoleService = consoleService;
        _skipAutoUpdate = skipAutoUpdate;
        _errorHandler = errorHandler;
        _logger = logger;
        _stateManager = stateManager;

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
    public async Task<string> GetVideoTitleAsync(string url, CancellationToken ct = default)
    {
        if (_errorHandler != null)
        {
            var result = await _errorHandler.TryExecuteAsync(
                async () => {
                    var title = await FetchVideoTitleRawAsync(url, ct);
                    return TitleHelper.FormatTitle(title ?? "video");
                },
                ErrorType.Network,
                $"Obter informações do vídeo: {url}",
                new RetryPolicy(maxRetries: 3, logger: _logger) // Retry automático para network errors
            );

            return result.Value;
        }
        else
        {
            var title = await FetchVideoTitleRawAsync(url, ct);
            return TitleHelper.FormatTitle(title ?? "video");
        }
    }

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    protected internal virtual async Task<string?> FetchVideoTitleRawAsync(string url, CancellationToken ct)
    {
        var result = await _ytdl.RunVideoDataFetch(url, ct: ct);
        return result.Data.Title;
    }

    /// <summary>
    /// Download completo do vídeo (melhor qualidade)
    /// </summary>
    public async Task DownloadAsync(
        string url,
        string outputFile,
        IProgress<DownloadProgress>? progress = null,
        CancellationToken ct = default)
    {
        if (_errorHandler != null)
        {
            await _errorHandler.TryExecuteAsync(
                async () => {
                    await DownloadAsyncInternal(url, outputFile, progress, ct);
                    return true;
                },
                ErrorType.Network,
                $"Download do vídeo: {url}",
                new RetryPolicy(maxRetries: 3, logger: _logger)
            );
        }
        else
        {
            await DownloadAsyncInternal(url, outputFile, progress, ct);
        }
    }

    private async Task DownloadAsyncInternal(
        string url,
        string outputFile,
        IProgress<DownloadProgress>? progress,
        CancellationToken ct)
    {
        // Download com melhor qualidade (video + audio mesclado em MP4)
        var options = new OptionSet
        {
            Format = "bestvideo+bestaudio",
            MergeOutputFormat = DownloadMergeFormat.Mp4,
            Output = outputFile
        };

        await RunVideoDownloadAsync(url, options, progress, ct);
    }

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    protected internal virtual Task RunVideoDownloadAsync(
        string url,
        OptionSet options,
        IProgress<DownloadProgress>? progress,
        CancellationToken ct)
    {
        return _ytdl.RunVideoDownload(url, overrideOptions: options, progress: progress, ct: ct);
    }

    /// <summary>
    /// Download com recorte de tempo (download completo + FFmpeg)
    /// </summary>
    public async Task DownloadWithTimeRangeAsync(
        string url,
        string outputFile,
        string startTime,
        string endTime,
        IProgress<DownloadProgress>? progress = null,
        CancellationToken ct = default)
    {
        if (_errorHandler != null)
        {
            await _errorHandler.TryExecuteAsync(
                async () => {
                    await DownloadWithTimeRangeAsyncInternal(url, outputFile, startTime, endTime, progress, ct);
                    return true;
                },
                ErrorType.Network,
                $"Download com recorte: {url}",
                new RetryPolicy(maxRetries: 3, logger: _logger)
            );
        }
        else
        {
            await DownloadWithTimeRangeAsyncInternal(url, outputFile, startTime, endTime, progress, ct);
        }
    }

    private async Task DownloadWithTimeRangeAsyncInternal(
        string url,
        string outputFile,
        string startTime,
        string endTime,
        IProgress<DownloadProgress>? progress,
        CancellationToken ct)
    {
        // Download completo primeiro
        var tempFile = CreateTempFile();

        try
        {
            _consoleService.WriteLine("Baixando vídeo completo...");
            await DownloadAsyncInternal(url, tempFile, progress, ct);
            
            // Usar FFmpeg para corte
            var timeStart = TimeHelper.GetStartSeconds(startTime);
            var timeEnd = TimeHelper.GetEndSeconds(endTime);
            var duration = timeEnd - timeStart;
            
            _consoleService.WriteLine($"Cortando vídeo ({startTime} - {endTime})...");
            
            var arguments =
                $"-i \"{tempFile}\" " +
                $"-ss {timeStart} " +
                $"-t {duration} " +
                $"-c:v libx264 -c:a aac " +
                $"\"{outputFile}\"";
            
            ExecuteFfmpeg(arguments, new ProgressBar(), ct);
            
            // Limpar temp
            _fileService.Delete(tempFile);
        }
        catch (OperationCanceledException)
        {
            // Cleanup arquivos temporários
            CleanupTempFile(tempFile);
            CleanupTempFile(outputFile);
            throw;
        }
    }

    /// <summary>
    /// Download de áudio MP3 com recorte de tempo
    /// </summary>
    public async Task DownloadAudioAsync(
        string url,
        string outputFile,
        string startTime,
        string endTime,
        IProgress<DownloadProgress>? progress = null,
        CancellationToken ct = default)
    {
        if (_errorHandler != null)
        {
            await _errorHandler.TryExecuteAsync(
                async () => {
                    await DownloadAudioAsyncInternal(url, outputFile, startTime, endTime, progress, ct);
                    return true;
                },
                ErrorType.Network,
                $"Download de áudio: {url}",
                new RetryPolicy(maxRetries: 3, logger: _logger)
            );
        }
        else
        {
            await DownloadAudioAsyncInternal(url, outputFile, startTime, endTime, progress, ct);
        }
    }

    private async Task DownloadAudioAsyncInternal(
        string url,
        string outputFile,
        string startTime,
        string endTime,
        IProgress<DownloadProgress>? progress,
        CancellationToken ct)
    {
        // Download de áudio completo primeiro
        var tempBase = Path.GetTempFileName();
        var tempFile = tempBase + ".mp3";
        
        try
        {
            _consoleService.WriteLine("Baixando áudio completo...");
            await DownloadAudioFullAsync(url, tempFile, progress, ct);
            
            // Verificar se arquivo temporário existe
            // O yt-dlp pode ter criado com extensão diferente
            var actualTempFile = tempFile;
            if (!_fileService.Exists(tempFile))
            {
                // Tenta sem extensão ou com outras extensões comuns de áudio
                var altExtensions = new[] { "", ".m4a", ".webm", ".opus" };
                foreach (var ext in altExtensions)
                {
                    var altPath = tempBase + ext;
                    if (_fileService.Exists(altPath))
                    {
                        actualTempFile = altPath;
                        break;
                    }
                }
            }
            
            if (!_fileService.Exists(actualTempFile))
            {
                throw new Exception($"Falha no download: arquivo temporário não criado (esperado: {tempFile})");
            }
            
            _consoleService.WriteLine($"Arquivo temporário criado: {actualTempFile}");
            
            // Converter para MP3 e cortar com FFmpeg
            var timeStart = TimeHelper.GetStartSeconds(startTime);
            var timeEnd = TimeHelper.GetEndSeconds(endTime);
            var duration = timeEnd - timeStart;
            
            _consoleService.WriteLine($"Convertendo para MP3 e cortando ({startTime} - {endTime})...");
            
            var arguments =
                $"-i \"{actualTempFile}\" " +
                $"-ss {timeStart} " +
                $"-t {duration} " +
                $"-vn " +  // No video
                $"-c:a libmp3lame " +
                $"-q:a 2 " +  // Qualidade alta (~192kbps)
                $"\"{outputFile}\"";
            
            ExecuteFfmpeg(arguments, new ProgressBar(), ct);
            
            // Verificar se arquivo de saída foi criado
            if (!_fileService.Exists(outputFile))
            {
                throw new Exception($"Falha na conversão: arquivo de saída não criado ({outputFile})");
            }
            
            // Limpar temp
            _fileService.Delete(actualTempFile);
        }
        catch (OperationCanceledException)
        {
            // Cleanup arquivos temporários
            CleanupTempFile(tempFile);
            CleanupTempFile(outputFile);
            
            // Limpar arquivos com extensões alternativas
            var altExtensions = new[] { "", ".m4a", ".webm", ".opus" };
            foreach (var ext in altExtensions)
            {
                CleanupTempFile(tempBase + ext);
            }
            
            throw;
        }
    }

    private async Task DownloadAudioFullAsync(
        string url,
        string outputFile,
        IProgress<DownloadProgress>? progress = null,
        CancellationToken ct = default)
    {
        var options = new OptionSet
        {
            Format = "bestaudio/best",
            ExtractAudio = true,
            AudioFormat = AudioConversionFormat.Mp3,
            AudioQuality = 2,
            Output = outputFile
        };
        
        await RunVideoDownloadAsync(url, options, progress, ct);
    }

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    protected internal virtual string CreateTempFile(string extension = ".mp4")
    {
        return Path.GetTempFileName() + extension;
    }

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    protected internal virtual void ExecuteFfmpeg(string arguments, IProgress<int> progress, CancellationToken ct)
    {
        var ffmpeg = new FfmpegHelper();
        ffmpeg.ExecuteFfmpeg(arguments, progress, ct);
    }

    /// <summary>
    /// Remove arquivo temporário se existir (silenciosamente)
    /// </summary>
    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    protected internal virtual void CleanupTempFile(string path)
    {
        try
        {
            if (_fileService.Exists(path))
            {
                _fileService.Delete(path);
            }
        }
        catch
        {
            // Ignorar erros de cleanup
        }
    }

    #endregion

    #region Recovery Support

    /// <summary>
    /// Download com suporte a recuperação de estado
    /// Cria e gerencia DownloadState durante o processo
    /// </summary>
    public async Task<Result<string>> DownloadWithRecoveryAsync(
        DownloadInput input,
        IProgress<DownloadProgress>? progress = null,
        CancellationToken ct = default)
    {
        string? stateId = null;

        try
        {
            // 1. Criar estado inicial
            stateId = await CreateDownloadStateAsync(input);

            // 2. Hook de progresso para salvar estado
            Progress<DownloadProgress>? stateProgress = null;
            if (!string.IsNullOrEmpty(stateId))
            {
                stateProgress = new Progress<DownloadProgress>(p =>
                {
                    // Propagar para progress original
                    progress?.Report(p);

                    // Salvar estado
                    _ = UpdateDownloadProgressAsync(stateId, p);
                });
            }

            // 3. Executar download (usar progress com hook)
            var effectiveProgress = stateProgress ?? progress;

            if (input.AudioOnly && !string.IsNullOrEmpty(input.StartTime) && !string.IsNullOrEmpty(input.EndTime))
            {
                await DownloadAudioAsync(
                    input.Url,
                    input.OutputPath,
                    input.StartTime!,
                    input.EndTime!,
                    effectiveProgress,
                    ct
                );
            }
            else if (!string.IsNullOrEmpty(input.StartTime) && !string.IsNullOrEmpty(input.EndTime))
            {
                await DownloadWithTimeRangeAsync(
                    input.Url,
                    input.OutputPath,
                    input.StartTime!,
                    input.EndTime!,
                    effectiveProgress,
                    ct
                );
            }
            else
            {
                await DownloadAsync(
                    input.Url,
                    input.OutputPath,
                    effectiveProgress,
                    ct
                );
            }

            // 4. Marcar como completado
            await MarkDownloadCompletedAsync(stateId);

            return Result<string>.Success(input.OutputPath);
        }
        catch (OperationCanceledException)
        {
            // CTRL+C
            await MarkDownloadCancelledAsync(stateId);
            _consoleService?.WriteLine("\n⚠️  Download cancelado pelo usuário.");
            throw;
        }
        catch (Exception ex)
        {
            // Erro durante download
            await MarkDownloadFailedAsync(stateId, ex);
            _logger?.LogError(ex, "Download failed", ("stateId", stateId ?? "none"));
            return Result<string>.Failure(ErrorType.Network, "Download failed", ex);
        }
    }

    #endregion

    #region State Management

    /// <summary>
    /// Cria estado inicial de download
    /// </summary>
    private async Task<string?> CreateDownloadStateAsync(DownloadInput input)
    {
        if (_stateManager == null)
            return null;

        var state = new Cutube.Cli.Recovery.DownloadState
        {
            Url = input.Url,
            OutputPath = input.OutputPath,
            StartTime = !string.IsNullOrEmpty(input.StartTime) ? TimeHelper.ParseTimeToTimeSpan(input.StartTime) : null,
            EndTime = !string.IsNullOrEmpty(input.EndTime) ? TimeHelper.ParseTimeToTimeSpan(input.EndTime) : null,
            AudioOnly = input.AudioOnly,
            Status = DownloadStatus.Downloading,
            ProgressPercent = 0
        };

        var stateId = await _stateManager.CreateStateAsync(state);
        _logger?.LogInfo("Download state created", ("stateId", stateId ?? ""));

        return stateId;
    }

    /// <summary>
    /// Atualiza progresso do estado (a cada 10%)
    /// </summary>
    private async Task UpdateDownloadProgressAsync(string? stateId, DownloadProgress progressData)
    {
        if (string.IsNullOrEmpty(stateId) || _stateManager == null)
            return;

        var progressPercent = (int)(progressData.Progress * 100);

        // Salvar a cada 10%
        if (progressPercent % 10 == 0)
        {
            await _stateManager.UpdateStateAsync(stateId, s =>
            {
                s.ProgressPercent = progressPercent;
                // DownloadProgress não expõe bytes diretamente, usar apenas percent
                s.DownloadedBytes = 0;
                s.TotalBytes = null;
            });
        }
    }

    /// <summary>
    /// Marca estado como completado
    /// </summary>
    private async Task MarkDownloadCompletedAsync(string? stateId)
    {
        if (string.IsNullOrEmpty(stateId) || _stateManager == null)
            return;

        await _stateManager.MarkCompletedAsync(stateId);
        _logger?.LogInfo("Download marked as completed", ("stateId", stateId));
    }

    /// <summary>
    /// Marca estado como falho
    /// </summary>
    private async Task MarkDownloadFailedAsync(string? stateId, Exception exception)
    {
        if (string.IsNullOrEmpty(stateId) || _stateManager == null)
            return;

        await _stateManager.MarkFailedAsync(stateId, exception.Message);
        _logger?.LogError(exception, "Download marked as failed", ("stateId", stateId));
    }

    /// <summary>
    /// Marca estado como cancelado
    /// </summary>
    private async Task MarkDownloadCancelledAsync(string? stateId)
    {
        if (string.IsNullOrEmpty(stateId) || _stateManager == null)
            return;

        await _stateManager.MarkCancelledAsync(stateId);
        _logger?.LogInfo("Download marked as cancelled", ("stateId", stateId));
    }

    #endregion

    public void Dispose()
    {
        _httpClientService?.Dispose();
    }
}
