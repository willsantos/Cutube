using YoutubeDLSharp;

namespace cutube;

public class ProgramWorkflow : IDisposable
{
    private readonly IMenuService _menu;
    private readonly IYtDlpService _ytdl;
    private readonly IConsoleService _console;
    private readonly IFileService _fileService;

    public ProgramWorkflow(IMenuService menu, IYtDlpService ytdl, IConsoleService console, IFileService fileService)
    {
        _menu = menu;
        _ytdl = ytdl;
        _console = console;
        _fileService = fileService;
    }

    public async Task RunAsync()
    {
        _menu.Show();

        var videoUrl = _menu.Url;
        var videoStart = _menu.Start;
        var videoEnd = _menu.End;

        _console.WriteLine("Obtendo informações do vídeo...");
        var videoTitle = await _ytdl.GetVideoTitleAsync(videoUrl);

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

        if (!_fileService.HasWritePermission(outputDir))
        {
            _console.WriteLine($"❌ Erro: Sem permissão de escrita em '{outputDir}'");
            return;
        }

        var output = Path.Combine(outputDir, $"{fileName}.mp4");

        try
        {
            _console.WriteLine("Iniciando o download e corte do vídeo...");
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

            await _ytdl.DownloadWithTimeRangeAsync(
                videoUrl,
                output,
                videoStart,
                videoEnd,
                progress
            );
        }
        catch (Exception e)
        {
            _console.WriteLine($"Erro: {e.Message}");
            throw;
        }
        finally
        {
            _console.WriteLine(
                $"✓ Vídeo salvo em: {Path.GetFullPath(output)}"
            );
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
