using YoutubeDLSharp;

namespace cutube;

public class ProgramWorkflow
{
    private readonly IMenuService _menu;
    private readonly IYtDlpService _ytdl;
    private readonly IConsoleService _console;

    public ProgramWorkflow(IMenuService menu, IYtDlpService ytdl, IConsoleService console)
    {
        _menu = menu;
        _ytdl = ytdl;
        _console = console;
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
        var output = $"{fileName}.mp4";

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
                $"O vídeo {videoTitle} foi baixado e cortado, o resultado está em: {Path.GetFullPath(output)}"
            );
        }
    }
}
