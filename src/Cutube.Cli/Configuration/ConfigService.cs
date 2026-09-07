using System.Text.Json;

namespace Cutube.Cli.Configuration;

/// <summary>
/// Service for managing application configuration
/// </summary>
public class ConfigService
{
    private readonly string _configPath;

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Creates a new ConfigService with default config path
    /// </summary>
    public ConfigService() : this(GetDefaultConfigDirectory())
    {
    }

    /// <summary>
    /// Creates a new ConfigService with custom config directory (for testing)
    /// </summary>
    /// <param name="configDirectory">Directory to store config file</param>
    public ConfigService(string configDirectory)
    {
        Directory.CreateDirectory(configDirectory);
        _configPath = Path.Combine(configDirectory, "config.json");
    }

    private static string GetDefaultConfigDirectory()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "cutube"
        );
    }

    /// <summary>
    /// Loads configuration from file
    /// If file doesn't exist, returns default config
    /// </summary>
    public async Task<AppConfig> LoadAsync(CancellationToken ct = default)
    {
        if (!File.Exists(_configPath))
        {
            return CreateDefaultConfig();
        }

        try
        {
            var json = await File.ReadAllTextAsync(_configPath, ct);
            var config = JsonSerializer.Deserialize<AppConfig>(json, _jsonOptions);

            return config ?? CreateDefaultConfig();
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                $"Error reading configuration from {_configPath}. " +
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
        var directory = Path.GetDirectoryName(_configPath)!;
        Directory.CreateDirectory(directory);

        var json = JsonSerializer.Serialize(config, _jsonOptions);
        await File.WriteAllTextAsync(_configPath, json, ct);
    }

    /// <summary>
    /// Returns default configuration
    /// </summary>
    private static AppConfig CreateDefaultConfig()
    {
        return new AppConfig
        {
            DefaultOutputPath = "~/Downloads",
            MaxConcurrentDownloads = 3,
            TimeoutSeconds = 300,
            VerboseLogging = false
        };
    }

    /// <summary>
    /// Returns full path of config file (for debugging)
    /// </summary>
    public string GetConfigPath() => _configPath;

    /// <summary>
    /// Checks if config file exists
    /// </summary>
    public bool ConfigExists() => File.Exists(_configPath);
}
