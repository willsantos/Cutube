
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

        var videoTitle = video.Title;

        var streamManifest =
            await youtube.Videos.Streams.GetManifestAsync(video.Id);
        var streamInfo = streamManifest
            .GetMuxedStreams()
            .GetWithHighestVideoQuality();


        var temp = Path.GetTempFileName();

        if (streamInfo == null)
            throw new Exception("Falha ao baixar o video");

        Console.WriteLine("Iniciando o download do video...");
        Console.WriteLine("O tempo de espera pode variar de acordo com a sua conexão.");

        await youtube.Videos.Streams.DownloadAsync(streamInfo, temp);
        
        Console.WriteLine("Video baixado com sucesso");

        videoTitle = TitleHelper.FormatTitle(videoTitle);
        var output = $"{videoTitle}.mp4";

        var ffmpeg = new FfmpegHelper();

        var arguments =
            $"-loglevel verbose -ss {timeStart} -i \"{temp}\" -t {timeEnd - timeStart} -c:v libx264 \"{output}\"";

        try
        {
            Console.WriteLine("Iniciando o corte do video...");
            Console.WriteLine("Esse processo pode demorar,aguarde...");
            ffmpeg.ExecuteFfmpeg(arguments, new ProgressBar());
            File.Delete(temp);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }


        Console.WriteLine(
            $"O video {videoTitle} foi baixado e cortado, o resultado está em: {Path.GetFullPath(output)}");
