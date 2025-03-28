using cutube;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;

Menu.Show();

var youtube = new YoutubeClient();

var videoUrl = Menu.Url;
var videoStart = Menu.Start;
var videoEnd = Menu.End;

var timeStart = TimeHelper.GetStartSeconds(videoStart);
var timeEnd = TimeHelper.GetEndSeconds(videoEnd);

var video = await youtube.Videos.GetAsync(videoUrl);
var videoTitle = TitleHelper.FormatTitle(video.Title);

var streamManifest =
    await youtube.Videos.Streams.GetManifestAsync(video.Id);

var videoStreamInfo = streamManifest
    .GetVideoStreams()
    .TryGetWithHighestVideoQuality();

var audioStreamInfo = streamManifest
    .GetAudioStreams()
    .TryGetWithHighestBitrate();

if (videoStreamInfo is null)
{
    Console.WriteLine("Não foi possível encontrar o video");
    return;
}

var tempVideo = Path.GetTempFileName();
var tempAudio = Path.GetTempFileName();

Console.WriteLine("Iniciando o download do video...");
Console.WriteLine("O tempo de espera pode variar de acordo com a sua conexão.");
await youtube.Videos.Streams.DownloadAsync(videoStreamInfo, tempVideo);
Console.WriteLine("Video baixado com sucesso");

if (audioStreamInfo is not null)
{
    Console.WriteLine("Iniciando o download do áudio...");
    await youtube.Videos.Streams.DownloadAsync(audioStreamInfo, tempAudio);
    Console.WriteLine("Áudio baixado com sucesso");
}
else
{
    Console.WriteLine("Não foi possível encontrar o áudio");
}

Console.WriteLine("Video baixado com sucesso");

var output = $"{videoTitle}.mp4";
var ffmpeg = new FfmpegHelper();

string arguments;

if (audioStreamInfo is not null)
{
    arguments =
        $"-loglevel verbose -i \"{tempVideo}\" -i \"{tempAudio}\"  -ss {timeStart} " +
        $"-t {timeEnd - timeStart} -c:v libx264 -c:a aac -strict experimental \"{output}\"";
}
else
{
    arguments =
        $"-loglevel verbose -i \"{tempVideo}\" -ss {timeStart}  " +
        $"-t {timeEnd - timeStart} -c:v libx264 -an \"{output}\"";
}

try
{
    Console.WriteLine("Iniciando o corte do video...");
    Console.WriteLine("Esse processo pode demorar,aguarde...");
    ffmpeg.ExecuteFfmpeg(arguments, new ProgressBar());
    File.Delete(tempVideo);
    if (File.Exists(tempAudio))
        File.Delete(tempAudio);
}
catch (Exception e)
{
    Console.WriteLine(e);
    throw;
}
finally
{
    Console.WriteLine(
    $"O video {videoTitle} foi baixado e cortado, o resultado está em: {Path.GetFullPath(output)}");
}