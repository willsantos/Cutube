using System.Threading;
using YoutubeDLSharp;
using Cutube.Cli.Logging;
using Cutube.Cli.ErrorHandling;

namespace Cutube.Cli;

public class ProgramWorkflow : IDisposable
{
    private readonly IMenuService _menu;
    private readonly IYtDlpService _ytdl;
    private readonly IConsoleService _console;
    private readonly IFileService _fileService;
    private readonly CancellationToken _ct;
    private readonly ILoggerService? _logger;
    private readonly IErrorHandler? _errorHandler;

    public ProgramWorkflow(
        IMenuService menu,
        IYtDlpService ytdl,
        IConsoleService console,
        IFileService fileService,
        CancellationToken ct = default,
        ILoggerService? logger = null,
        IErrorHandler? errorHandler = null)
    {
        _menu = menu;
        _ytdl = ytdl;
        _console = console;
        _fileService = fileService;
        _ct = ct;
        _logger = logger;
        _errorHandler = errorHandler;
    }

    public async Task<Result> RunAsync()
    {
        try
        {
            var errorHandler = _errorHandler ?? new DefaultErrorHandler();
            var menuResult = _menu.Show(_console, _fileService, errorHandler);
            if (!menuResult.IsSuccess)
                return menuResult;

            var videoUrl = _menu.Url;
            var videoStart = _menu.Start;
            var videoEnd = _menu.End;

            _console.WriteLine("Obtendo informações do vídeo...");

            var titleResult = await GetVideoTitleAsync(videoUrl);
            if (titleResult.IsFailure)
                return titleResult;

            var fileName = string.IsNullOrWhiteSpace(_menu.CustomFileName)
                ? titleResult.Value
                : TitleHelper.FormatTitle(_menu.CustomFileName);

            var outputDirResult = GetOutputDirectory();
            if (outputDirResult.IsFailure)
                return outputDirResult;

            var outputDir = outputDirResult.Value;
            var extension = _menu.AudioOnly ? ".mp3" : ".mp4";
            var output = Path.Combine(outputDir, $"{fileName}{extension}");

            return await DownloadVideoAsync(videoUrl, output, videoStart, videoEnd);
        }
        catch (OperationCanceledException)
        {
            _console.WriteLine("\n⚠️  Operação cancelada pelo usuário.");
            return Result.Success();
        }
        catch (Exception ex)
        {
            if (_errorHandler != null)
            {
                var errorMessage = _errorHandler.GetUserFriendlyMessage(ex);
                _console.WriteLine($"Erro: {errorMessage}");
                return Result.Failure(ErrorType.Critical, errorMessage, ex);
            }
            else
            {
                _console.WriteLine($"Erro: {ex.Message}");
                return Result.Failure(ErrorType.Critical, ex.Message, ex);
            }
        }
    }

    private async Task<Result<string>> GetVideoTitleAsync(string url)
    {
        if (_errorHandler != null)
        {
            return await _errorHandler.TryExecuteAsync(
                () => _ytdl.GetVideoTitleAsync(url, _ct),
                ErrorType.Network,
                $"Obter informações do vídeo: {url}"
            );
        }
        else
        {
            var title = await _ytdl.GetVideoTitleAsync(url, _ct);
            return Result<string>.Success(title);
        }
    }

    private Result<string> GetOutputDirectory()
    {
        try
        {
            var outputDir = string.IsNullOrWhiteSpace(_menu.OutputDirectory)
                ? Directory.GetCurrentDirectory()
                : NormalizePath(_menu.OutputDirectory);

            if (!_fileService.DirectoryExists(outputDir))
            {
                _console.WriteLine($"⚠️  Diretório '{outputDir}' não existe.");
                _console.Write("Deseja criá-lo? (s/n): ");
                var response = _console.ReadLine()?.ToLower();
                if (response == "s")
                {
                    _fileService.CreateDirectory(outputDir);
                    _console.WriteLine($"✓ Diretório criado: {outputDir}");
                }
                else
                {
                    _console.WriteLine("❌ Operação cancelada.");
                    return Result<string>.Failure(ErrorType.Validation, "Operação cancelada pelo usuário");
                }
            }

            ValidationHelper.ValidateDirectory(outputDir, _fileService);
            return Result<string>.Success(outputDir);
        }
        catch (Exception ex)
        {
            if (_errorHandler != null)
            {
                var message = _errorHandler.GetUserFriendlyMessage(ex);
                return Result<string>.Failure(ErrorType.FileSystem, message, ex);
            }
            return Result<string>.Failure(ErrorType.FileSystem, ex.Message, ex);
        }
    }

    private async Task<Result> DownloadVideoAsync(string url, string output, string start, string end)
    {
        try
        {
            var typeLabelInicio = _menu.AudioOnly ? "áudio" : "vídeo";
            _console.WriteLine($"Iniciando o download e corte do {typeLabelInicio}...");
            _console.WriteLine("Esse processo pode demorar, aguarde...");

            var progress = new Progress<DownloadProgress>(p =>
            {
                if (p.Progress > 0)
                {
                    var percentage = p.Progress * 100;
                    _console.WriteLine($"Progresso: {percentage:F0}%");
                }

                if (p.State != DownloadState.None)
                {
                    _console.WriteLine($"Estado: {p.State}");
                }
            });

            if (_menu.AudioOnly)
            {
                await _ytdl.DownloadAudioAsync(
                    url,
                    output,
                    start,
                    end,
                    progress,
                    _ct
                );
            }
            else
            {
                await _ytdl.DownloadWithTimeRangeAsync(
                    url,
                    output,
                    start,
                    end,
                    progress,
                    _ct
                );
            }

            var typeLabel = _menu.AudioOnly ? "Áudio" : "Vídeo";
            _console.WriteLine(
                $"✓ {typeLabel} salvo em: {Path.GetFullPath(output)}"
            );

            return Result.Success();
        }
        catch (Exception ex)
        {
            if (_errorHandler != null)
            {
                var message = _errorHandler.GetUserFriendlyMessage(ex);
                return Result.Failure(ErrorType.Network, message, ex);
            }
            return Result.Failure(ErrorType.Network, ex.Message, ex);
        }
    }

    private string NormalizePath(string path)
    {
        var fullPath = Path.GetFullPath(path);
        fullPath = fullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return fullPath;
    }

    public void Dispose()
    {
        _ytdl?.Dispose();
    }
}
