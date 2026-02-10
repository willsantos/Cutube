using System.Text.Json;

namespace Cutube.Configuration;

/// <summary>
/// Service for managing application configuration
/// </summary>
public class ConfigService
{
    private static readonly string ConfigDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "cutube"
    );

    private static readonly string ConfigPath = Path.Combine(
        ConfigDirectory,
        "config.json"
    );

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Loads configuration from file
    /// If file doesn't exist, returns default config
    /// </summary>
    public async Task<AppConfig> LoadAsync(CancellationToken ct = default)
    {
        if (!File.Exists(ConfigPath))
        {
            return CreateDefaultConfig();
        }

        try
        {
            var json = await File.ReadAllTextAsync(ConfigPath, ct);
            var config = JsonSerializer.Deserialize<AppConfig>(json, _jsonOptions);

            return config ?? CreateDefaultConfig();
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                $"Error reading configuration from {ConfigPath}. " +
                $"File may be corrupted. Delete the file to use default config.",
                ex
            );
        }
    }

    /// <summary>
    /// Saves configuration to file
    /// Creates directory if it doesn't exist
    /// </summary>
    public async Task SaveAsync(AppConfig config, CancellationToken ct = default)
    {
        // Create directory if it doesn't exist
        Directory.CreateDirectory(ConfigDirectory);

        var json = JsonSerializer.Serialize(config, _jsonOptions);
        await File.WriteAllTextAsync(ConfigPath, json, ct);
    }

    /// <summary>
    /// Returns default configuration
    /// </summary>
    private static AppConfig CreateDefaultConfig()
    {
        return new AppConfig
        {
            ApiUrl = null,
            DefaultOutputPath = "~/Downloads",
            MaxConcurrentDownloads = 3,
            TimeoutSeconds = 300,
            VerboseLogging = false
        };
    }

    /// <summary>
    /// Returns full path of config file (for debugging)
    /// </summary>
    public string GetConfigPath() => ConfigPath;

    /// <summary>
    /// Checks if config file exists
    /// </summary>
    public bool ConfigExists() => File.Exists(ConfigPath);
}
