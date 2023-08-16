// See https://aka.ms/new-console-template for more information

using YoutubeExplode;

var youtube = new YoutubeClient();
var videoUrl = "https://www.youtube.com/watch?v=oKmWXD2i3y8";
var video = await youtube.Videos.GetAsync(videoUrl);

Console.WriteLine(video.Title);