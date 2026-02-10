namespace Cutube.Configuration;

/// <summary>
/// Default configuration values for Cutube CLI
/// </summary>
public static class ConfigDefaults
{
    /// <summary>
    /// Default API URL (null means local mode)
    /// </summary>
    public const string? DefaultApiUrl = null;

    /// <summary>
    /// Default output path for downloads
    /// </summary>
    public const string DefaultOutputPath = "~/Downloads";

    /// <summary>
    /// Default maximum concurrent downloads
    /// </summary>
    public const int DefaultMaxConcurrentDownloads = 3;

    /// <summary>
    /// Default timeout in seconds for API operations
    /// </summary>
    public const int DefaultTimeoutSeconds = 300;

    /// <summary>
    /// Default verbose logging setting
    /// </summary>
    public const bool DefaultVerboseLogging = false;

    /// <summary>
    /// Creates a new AppConfig with default values
    /// </summary>
    public static AppConfig CreateDefault() => new()
    {
        ApiUrl = DefaultApiUrl,
        DefaultOutputPath = DefaultOutputPath,
        MaxConcurrentDownloads = DefaultMaxConcurrentDownloads,
        TimeoutSeconds = DefaultTimeoutSeconds,
        VerboseLogging = DefaultVerboseLogging
    };
}
