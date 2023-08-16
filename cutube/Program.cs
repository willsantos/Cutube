// See https://aka.ms/new-console-template for more information

using cutube;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;

Menu.Show();

var youtube = new YoutubeClient();

var videoUrl = Menu.Url;
var videoStart = Menu.Start;
var videoEnd = Menu.End;

var timeStart = TimeHelper.GetStartSeconds(videoStart);
var timeEnd= TimeHelper.GetEndSeconds(videoEnd);

var video = await youtube.Videos.GetAsync(videoUrl);

var videoTitle = video.Title;

var streamManifest = await youtube.Videos.Streams.GetManifestAsync(video.Id);
var streamInfo = streamManifest
    .GetMuxedStreams()
    .TryGetWithHighestVideoQuality();

var temp = Path.GetTempFileName();

if (streamInfo == null)
    throw new Exception("Falha ao baixar o video");    
await youtube.Videos.Streams.DownloadAsync(streamInfo, temp);

videoTitle = TitleHelper.FormatTitle(videoTitle);
var output = $"{videoTitle}.mp4";

var ffmpeg = new FfmpegHelper();

var arguments = $"-loglevel verbose -ss {timeStart} -i \"{temp}\" -t {timeEnd - timeStart} -c:v libx264 \"{output}\"";

try
{
    ffmpeg.ExecuteFfmpeg(arguments);
    File.Delete(temp);
}
catch (Exception e)
{
    Console.WriteLine(e);
    throw;
}


Console.WriteLine($"O video {videoTitle} foi baixado e cortado, o resultado está em: {Path.GetFullPath(output)}");    