namespace Cutube.Cli.Configuration;

using System.Text.Json.Serialization;

/// <summary>
/// Configuration for the Cutube CLI application
/// </summary>
public class AppConfig
{
    /// <summary>
    /// Remote API URL (ex: http://localhost:5000)
    /// If null or empty, uses local mode (standalone)
    /// </summary>
    [JsonPropertyName("apiUrl")]
    public string? ApiUrl { get; set; }

    /// <summary>
    /// Indicates whether to use remote API
    /// </summary>
    [JsonIgnore]
    public bool UseApi => !string.IsNullOrWhiteSpace(ApiUrl);

    /// <summary>
    /// Default path for saving downloads
    /// Default: ~/Downloads
    /// </summary>
    [JsonPropertyName("defaultOutputPath")]
    public string DefaultOutputPath { get; set; } = "~/Downloads";

    /// <summary>
    /// Maximum number of concurrent downloads (API mode only)
    /// Default: 3
    /// </summary>
    [JsonPropertyName("maxConcurrentDownloads")]
    public int MaxConcurrentDownloads { get; set; } = 3;

    /// <summary>
    /// Timeout in seconds for API operations
    /// Default: 300 (5 minutes)
    /// </summary>
    [JsonPropertyName("timeoutSeconds")]
    public int TimeoutSeconds { get; set; } = 300;

    /// <summary>
    /// Enables verbose debug logging
    /// Default: false
    /// </summary>
    [JsonPropertyName("verboseLogging")]
    public bool VerboseLogging { get; set; } = false;
}
