namespace Cutube.Infrastructure;

/// <summary>
/// Resolves the path to yt-dlp binary:
/// 1. User version (LocalApplicationData/Cutube)
/// 2. Bundled version (AppContext.BaseDirectory/bin)
/// 3. System PATH
/// 4. Automatic download to the user location
/// </summary>
public static class YtDlpPathResolver
{
    private const string LatestReleaseBaseUrl =
        "https://github.com/yt-dlp/yt-dlp/releases/latest/download/";

    /// <summary>
    /// Resolves the best available yt-dlp path, downloading it automatically
    /// as a last resort (matching the README's "included automatically" promise)
    /// </summary>
    /// <returns>Path to yt-dlp executable</returns>
    /// <exception cref="FileNotFoundException">Thrown when yt-dlp cannot be found or downloaded</exception>
    public static string Resolve()
    {
        var isWindows = OperatingSystem.IsWindows();
        var exeName = isWindows ? "yt-dlp.exe" : "yt-dlp";

        // 1. User version in LocalApplicationData
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var userPath = Path.Combine(appData, "Cutube", exeName);
        if (File.Exists(userPath))
            return userPath;

        // 2. Bundled with the app (in bin/ subdirectory)
        var basePath = AppContext.BaseDirectory;
        var bundledPath = Path.Combine(basePath, "bin", exeName);
        if (File.Exists(bundledPath))
            return bundledPath;

        // 2b. Bundled directly in base directory
        var bundledDirectPath = Path.Combine(basePath, exeName);
        if (File.Exists(bundledDirectPath))
            return bundledDirectPath;

        // 3. System PATH
        var pathEnv = Environment.GetEnvironmentVariable("PATH") ?? "";
        var systemPath = pathEnv
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(folder => folder.Length > 0)
            .Select(folder => Path.Combine(folder.Trim('"'), exeName))
            .FirstOrDefault(File.Exists);

        if (systemPath != null)
            return systemPath;

        // 4. Automatic download to the user location
        TryDownloadTo(userPath);

        if (File.Exists(userPath))
            return userPath;

        throw new FileNotFoundException(
            $"yt-dlp não encontrado. Verifique a instalação ou baixe em: https://github.com/yt-dlp/yt-dlp/releases");
    }

    private static void TryDownloadTo(string targetPath)
    {
        try
        {
            Console.WriteLine("yt-dlp não encontrado. Baixando automaticamente...");
            DownloadToFileAsync(new HttpClient(), GetDownloadUrl(), targetPath)
                .GetAwaiter()
                .GetResult();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠ Aviso: falha ao baixar yt-dlp automaticamente: {ex.Message}");
        }
    }

    /// <summary>
    /// Downloads the yt-dlp binary from <paramref name="url"/> to
    /// <paramref name="targetPath"/> and marks it executable on Unix-like systems
    /// </summary>
    public static async Task DownloadToFileAsync(HttpClient httpClient, string url, string targetPath)
    {
        var data = await httpClient.GetByteArrayAsync(url);

        var directory = Path.GetDirectoryName(targetPath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        await File.WriteAllBytesAsync(targetPath, data);

        if (!OperatingSystem.IsWindows())
        {
            var chmod = System.Diagnostics.Process.Start(
                new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "chmod",
                    Arguments = $"+x \"{targetPath}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                });
            if (chmod != null)
                await chmod.WaitForExitAsync();
        }
    }

    /// <summary>
    /// Builds the download URL for the latest yt-dlp release matching the
    /// current OS and architecture
    /// </summary>
    public static string GetDownloadUrl()
    {
        var arch = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture;

        if (OperatingSystem.IsWindows())
        {
            var suffix = arch switch
            {
                System.Runtime.InteropServices.Architecture.X64 => "_x64.exe",
                System.Runtime.InteropServices.Architecture.X86 => "_x86.exe",
                System.Runtime.InteropServices.Architecture.Arm64 => "_arm64.exe",
                _ => "_x64.exe"
            };
            return $"{LatestReleaseBaseUrl}yt-dlp{suffix}";
        }

        if (OperatingSystem.IsLinux())
        {
            var suffix = arch switch
            {
                System.Runtime.InteropServices.Architecture.X64 => "_linux",
                System.Runtime.InteropServices.Architecture.Arm64 => "_linux_aarch64",
                System.Runtime.InteropServices.Architecture.Armv6 => "_linux_armv7l",
                _ => "_linux"
            };
            return $"{LatestReleaseBaseUrl}yt-dlp{suffix}";
        }

        if (OperatingSystem.IsMacOS())
        {
            var suffix = arch switch
            {
                System.Runtime.InteropServices.Architecture.X64 => "_macos",
                System.Runtime.InteropServices.Architecture.Arm64 => "_macos_arm64",
                _ => "_macos"
            };
            return $"{LatestReleaseBaseUrl}yt-dlp{suffix}";
        }

        return $"{LatestReleaseBaseUrl}yt-dlp";
    }
}
