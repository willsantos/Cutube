using cutube;
using YoutubeDLSharp;

Menu.Show();

var videoUrl = Menu.Url;
var videoStart = Menu.Start;
var videoEnd = Menu.End;

var ytdl = new YtDlpHelper();

Console.WriteLine("Obtendo informações do vídeo...");
var videoTitle = await ytdl.GetVideoTitleAsync(videoUrl);

var output = $"{videoTitle}.mp4";

try
{
    Console.WriteLine("Iniciando o download e corte do vídeo...");
    Console.WriteLine("Esse processo pode demorar, aguarde...");
    
    var progress = new Progress<DownloadProgress>(p => 
    {
        if (p.Progress > 0)
        {
            var percentage = p.Progress * 100;
            Console.WriteLine($"Progresso: {percentage:F0}%");
        }
        
        if (p.State != YoutubeDLSharp.DownloadState.None)
        {
            Console.WriteLine($"Estado: {p.State}");
        }
    });

    await ytdl.DownloadWithTimeRangeAsync(
        videoUrl,
        output,
        videoStart,
        videoEnd,
        progress
    );
}
catch (Exception e)
{
    Console.WriteLine($"Erro: {e.Message}");
    throw;
}
finally
{
    Console.WriteLine(
        $"O vídeo {videoTitle} foi baixado e cortado, o resultado está em: {Path.GetFullPath(output)}"
    );
}
