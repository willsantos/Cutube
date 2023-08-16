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
    
    public void ExecuteFfmpeg(string arguments)
    {
        using (var process = new System.Diagnostics.Process())
        {
            process.StartInfo.FileName = FfmpegPath;
            process.StartInfo.Arguments = arguments;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.RedirectStandardOutput = false;
            process.StartInfo.RedirectStandardError = false;

            try
            {
                process.Start();
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