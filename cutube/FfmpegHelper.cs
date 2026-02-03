using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading;

namespace cutube;

public class FfmpegHelper : IFfmpegHelper
{
    private string FfmpegPath { get; set; }
    private string FfprobePath { get; set; }

    private readonly IEnvironmentService _environmentService;
    private readonly IFileService _fileService;
    private readonly IProcessRunner _processRunner;
    private readonly IConsoleService _consoleService;
    
    private static readonly string[] FfmpegExecutableNames = new string[]
    {
        "ffmpeg.exe", 
        "ffmpeg"
    };
    
    private static readonly string[] FfprobeExecutableNames = new string[]
    {
        "ffprobe.exe", 
        "ffprobe"
    };
    
    public FfmpegHelper() : this(
        new EnvironmentService(),
        new FileService(),
        new ProcessRunner(),
        new ConsoleService()
    )
    {
    }

    public FfmpegHelper(
        IEnvironmentService environmentService,
        IFileService fileService,
        IProcessRunner processRunner,
        IConsoleService consoleService)
    {
        _environmentService = environmentService;
        _fileService = fileService;
        _processRunner = processRunner;
        _consoleService = consoleService;

        FfmpegPath = GetFfmpegPath();
        FfprobePath = GetFfprobePath();
    }

    protected internal string GetFfprobePath()
    {
        foreach (var executableName in FfprobeExecutableNames)
            if (TryGetFromAppData(executableName, out var path) ||
                TryGetFromSystemPath(executableName, out path))
                return path;

        throw new Exception("Não foi possível encontrar o FFprobe.");
    }

    protected internal string GetFfmpegPath()
    {
        foreach (var executableName in FfmpegExecutableNames)
            if (TryGetFromAppData(executableName, out var path) ||
                TryGetFromSystemPath(executableName, out path))
                return path;

        throw new Exception("Não foi possível encontrar o FFmpeg.");
    }
    
    protected internal bool TryGetFromAppData(string executableName, out string path)
    {
        var appDataPath = _environmentService.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var appDataExecutablePath = Path.Combine(appDataPath, executableName);

        if (_fileService.Exists(appDataExecutablePath))
        {
            path = appDataExecutablePath;
            return true;
        }

        path = null!;
        return false;
    }
    
    protected internal bool TryGetFromSystemPath(string executableName, out string path)
    {
        var systemPath = _environmentService.GetEnvironmentVariable("PATH");
        if(systemPath == null)
            throw new Exception("Não foi possível encontrar o PATH do sistema.");
        foreach (var folder in systemPath.Split(Path.PathSeparator))
        {
            var folderExecutablePath = Path.Combine(folder, executableName);

            if (!_fileService.Exists(folderExecutablePath)) continue;
            path = folderExecutablePath;
            return true;
        }

        path = null!;
        return false;
    }
    
    public void ExecuteFfmpeg(string arguments, IProgress<int> progress, CancellationToken ct = default)
    {
        
        var startInfo = new ProcessStartInfo
        {
            FileName = FfmpegPath,
            Arguments = arguments,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = false,
            RedirectStandardError = true,
            EnvironmentVariables =
            {
                ["PATH"] = _environmentService.GetEnvironmentVariable("PATH"),
                ["TEMP"] = _environmentService.GetEnvironmentVariable("TEMP")
            }
        };

        try
        {
            var duration = TimeSpan.Zero;
            var durationRegex = new Regex(@"Duration: (\d+):(\d+):(\d+).(\d+)");
            var progressRegex = new Regex(@"time=(\d+):(\d+):(\d+).(\d+)");
            
            _processRunner.RunAsync(startInfo, data =>
            {
                ct.ThrowIfCancellationRequested();
                
                if (data == null) return;
                if (data.Contains("Duration"))
                {
                    var matchDuration = durationRegex.Match(data);
                    if (matchDuration.Success)
                    {
                        var hours =
                            int.Parse(matchDuration.Groups[1].Value);
                        var minutes =
                            int.Parse(matchDuration.Groups[2].Value);
                        var seconds =
                            int.Parse(matchDuration.Groups[3].Value);
                        var milliseconds =
                            int.Parse(matchDuration.Groups[4].Value);

                        duration = new TimeSpan(0, hours, minutes,
                            seconds, milliseconds);
                    }
                }

                if (!data.Contains("time")) return;
                {
                    var matchTime = progressRegex.Match(data);
                    if (!matchTime.Success) return;
                    if (duration.TotalMilliseconds <= 0) return;
                    var hours =
                        int.Parse(matchTime.Groups[1].Value);
                    var minutes =
                        int.Parse(matchTime.Groups[2].Value);
                    var seconds =
                        int.Parse(matchTime.Groups[3].Value);
                    var milliseconds =
                        int.Parse(matchTime.Groups[4].Value);

                    var progressTime = new TimeSpan(0, hours, minutes,
                        seconds, milliseconds);
                    var percentage =
                        (int)(progressTime.TotalMilliseconds /
                            duration.TotalMilliseconds * 100);

                    progress.Report(percentage);
                }
            }, ct).GetAwaiter().GetResult();
        }
        catch (OperationCanceledException)
        {
            _consoleService.WriteLine("\n⚠️  Operação cancelada pelo usuário.");
            throw;
        }
        catch (Exception e)
        {
            _consoleService.WriteLine(e.ToString());
            throw;
        }
    }
}
