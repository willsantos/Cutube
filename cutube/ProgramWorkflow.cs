using System.Threading;
using YoutubeDLSharp;

namespace cutube;

public class ProgramWorkflow : IDisposable
{
    private readonly IMenuService _menu;
    private readonly IYtDlpService _ytdl;
    private readonly IConsoleService _console;
    private readonly IFileService _fileService;
    private readonly CancellationToken _ct;

    public ProgramWorkflow(
        IMenuService menu,
        IYtDlpService ytdl,
        IConsoleService console,
        IFileService fileService,
        CancellationToken ct = default)
    {
        _menu = menu;
        _ytdl = ytdl;
        _console = console;
        _fileService = fileService;
        _ct = ct;
    }

    public async Task RunAsync()
    {
        _menu.Show();

        ValidationHelper.ValidateUrl(_menu.Url);
        ValidationHelper.ValidateTimeRange(_menu.Start, _menu.End);

        if (!string.IsNullOrWhiteSpace(_menu.CustomFileName))
        {
            ValidationHelper.ValidateFileName(_menu.CustomFileName);
        }

        var videoUrl = _menu.Url;
        var videoStart = _menu.Start;
        var videoEnd = _menu.End;

        _console.WriteLine("Obtendo informações do vídeo...");
        var videoTitle = await _ytdl.GetVideoTitleAsync(videoUrl, _ct);

        var fileName = string.IsNullOrWhiteSpace(_menu.CustomFileName)
            ? videoTitle
            : TitleHelper.FormatTitle(_menu.CustomFileName);

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
                return;
            }
        }

        ValidationHelper.ValidateDirectory(outputDir, _fileService);

        var extension = _menu.AudioOnly ? ".mp3" : ".mp4";
        var output = Path.Combine(outputDir, $"{fileName}{extension}");

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
                    videoUrl,
                    output,
                    videoStart,
                    videoEnd,
                    progress,
                    _ct
                );
            }
            else
            {
                await _ytdl.DownloadWithTimeRangeAsync(
                    videoUrl,
                    output,
                    videoStart,
                    videoEnd,
                    progress,
                    _ct
                );
            }

            var typeLabel = _menu.AudioOnly ? "Áudio" : "Vídeo";
            _console.WriteLine(
                $"✓ {typeLabel} salvo em: {Path.GetFullPath(output)}"
            );
        }
        catch (OperationCanceledException)
        {
            _console.WriteLine("\n⚠️  Operação cancelada pelo usuário.");
        }
        catch (Exception e)
        {
            _console.WriteLine($"Erro: {e.Message}");
            throw;
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
