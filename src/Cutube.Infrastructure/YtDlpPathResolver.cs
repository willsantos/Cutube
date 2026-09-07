namespace Cutube.Infrastructure;

/// <summary>
/// Resolves the path to yt-dlp binary using the same priority as the original YtDlpHelper:
/// 1. User version (~/.local/share/Cutube/yt-dlp)
/// 2. Bundled version (AppContext.BaseDirectory/bin/yt-dlp)
/// 3. System PATH
/// </summary>
public static class YtDlpPathResolver
{
    /// <summary>
    /// Resolves the best available yt-dlp path
    /// </summary>
    /// <returns>Path to yt-dlp executable</returns>
    /// <exception cref="FileNotFoundException">Thrown when yt-dlp cannot be found</exception>
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

        throw new FileNotFoundException(
            $"yt-dlp não encontrado. Verifique a instalação ou baixe em: https://github.com/yt-dlp/yt-dlp/releases");
    }
}
