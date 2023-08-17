using System.Diagnostics;
using System.Text.RegularExpressions;

namespace cutube;

public class FfmpegHelper
{
    
    public string FfmpegPath { get; set; } 
    public string FfprobePath { get; set; }
    
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
    
    public FfmpegHelper()
    {
        FfmpegPath = GetFfmpegPath();
        FfprobePath = GetFfprobePath();
    }

    private static string GetFfprobePath()
    {
        foreach (var executableName in FfprobeExecutableNames)
            if (TryGetFromAppData(executableName, out var path) ||
                TryGetFromSystemPath(executableName, out path))
                return path;

        throw new Exception("Não foi possível encontrar o FFprobe.");
    }

    private static string GetFfmpegPath()
    {
        foreach (var executableName in FfmpegExecutableNames)
            if (TryGetFromAppData(executableName, out var path) ||
                TryGetFromSystemPath(executableName, out path))
                return path;

        throw new Exception("Não foi possível encontrar o FFmpeg.");
    }
    
    private static bool TryGetFromAppData(string executableName, out string path)
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var appDataExecutablePath = Path.Combine(appDataPath, executableName);

        if (File.Exists(appDataExecutablePath))
        {
            path = appDataExecutablePath;
            return true;
        }

        path = null;
        return false;
    }
    
    private static bool TryGetFromSystemPath(string executableName, out string path)
    {
        var systemPath = Environment.GetEnvironmentVariable("PATH");
        foreach (var folder in systemPath.Split(Path.PathSeparator))
        {
            var folderExecutablePath = Path.Combine(folder, executableName);

            if (File.Exists(folderExecutablePath))
            {
                path = folderExecutablePath;
                return true;
            }
        }

        path = null;
        return false;
    }
    
    public void ExecuteFfmpeg(string arguments, ProgressBar progressBar)
    {
        
        var startInfo = new ProcessStartInfo
        {
            FileName = FfmpegPath,
            Arguments = arguments,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = false,
            RedirectStandardError = true
        };        
        
        startInfo.EnvironmentVariables["PATH"] = Environment.GetEnvironmentVariable("PATH");
        startInfo.EnvironmentVariables["TEMP"] = Environment.GetEnvironmentVariable("TEMP");

        using (var process = new Process { StartInfo = startInfo })
        {
            try
            {
                var duration = TimeSpan.Zero;
                var durationRegex = new Regex(@"Duration: (\d+):(\d+):(\d+).(\d+)");
                var progressRegex = new Regex(@"time=(\d+):(\d+):(\d+).(\d+)");
                process.ErrorDataReceived += (sender, args) =>
                {
                    if (args.Data != null)
                    {
                        if (args.Data.Contains("Duration"))
                        {
                            var matchDuration = durationRegex.Match(args.Data);
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

                        if (args.Data.Contains("time"))
                        {
                            var matchTime = progressRegex.Match(args.Data);
                            if (matchTime.Success)
                            {
                                var hours =
                                    int.Parse(matchTime.Groups[1].Value);
                                var minutes =
                                    int.Parse(matchTime.Groups[2].Value);
                                var seconds =
                                    int.Parse(matchTime.Groups[3].Value);
                                var milliseconds =
                                    int.Parse(matchTime.Groups[4].Value);

                                var progress = new TimeSpan(0, hours, minutes,
                                    seconds, milliseconds);
                                var percentage =
                                    (int)(progress.TotalMilliseconds /
                                        duration.TotalMilliseconds * 100);

                                progressBar.Report(percentage);
                            }
                        }
                    }
                };

                process.Start();
                process.BeginErrorReadLine();
                process.WaitForExit();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
        
        
    }
}