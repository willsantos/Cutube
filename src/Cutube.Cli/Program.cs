using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Cutube.Cli.Configuration;
using Cutube.Domain.Interfaces;
using Cutube.Domain.Models;
using Cutube.Domain.Services;
using Cutube.Cli.Infrastructure;
using Cutube.Cli.Logging;
using Cutube.Cli.ErrorHandling;
using Cutube.Cli.Recovery;
using Cutube.Cli.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Cutube.Cli;

[ExcludeFromCodeCoverage]
public static class Program
{
    public static async Task Main(string[] args)
    {
        using var cts = new CancellationTokenSource();

        Console.CancelKeyPress += (sender, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
            Console.WriteLine("\n⚠️  Cancelando operação...");
        };

        // Parse command line arguments
        var parsedArgs = ParseArgs(args);

        // Handle --resume
        if (parsedArgs.ContainsKey("resume"))
        {
            await RunResumeModeAsync(cts.Token);
            return;
        }

        // Handle config commands
        if (args.Length > 0 && args[0] == "config")
        {
            await RunConfigCommandAsync(args);
            return;
        }

        // Handle download command
        if (args.Length > 0 && args[0] == "download")
        {
            var exitCode = await RunDownloadCommandAsync(args, cts.Token);
            Environment.Exit(exitCode);
            return;
        }

        // Default: interactive mode (no args or unknown command)
        if (args.Length == 0)
        {
            await RunInteractiveModeAsync(cts.Token);
            return;
        }

        // Show help for unknown commands
        ShowHelp();
    }

    private static Dictionary<string, string?> ParseArgs(string[] args)
    {
        var result = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i].StartsWith("--"))
            {
                var key = args[i][2..];
                if (i + 1 < args.Length && !args[i + 1].StartsWith("--"))
                {
                    result[key] = args[i + 1];
                    i++; // Skip next arg as it's the value
                }
                else
                {
                    result[key] = "true";
                }
            }
            else if (args[i].StartsWith("-"))
            {
                var key = args[i][1..];
                if (i + 1 < args.Length && !args[i + 1].StartsWith("-"))
                {
                    result[key] = args[i + 1];
                    i++; // Skip next arg as it's the value
                }
                else
                {
                    result[key] = "true";
                }
            }
        }

        return result;
    }

    private static void ShowHelp()
    {
        Console.WriteLine("Cutube - YouTube video downloader");
        Console.WriteLine();
        Console.WriteLine("Usage:");
        Console.WriteLine("  cutube                    Interactive mode");
        Console.WriteLine("  cutube download <url>     Download a video");
        Console.WriteLine("  cutube config show        Show configuration");
        Console.WriteLine("  cutube config reset       Reset configuration");
        Console.WriteLine("  cutube --resume           Resume interrupted downloads");
        Console.WriteLine();
        Console.WriteLine("Download options:");
        Console.WriteLine("  --api-url, -a <url>       Remote API URL");
        Console.WriteLine("  --local, -l               Force local mode");
        Console.WriteLine("  --output, -o <path>       Output path");
        Console.WriteLine("  --start, -s <time>        Start time (HH:MM:SS)");
        Console.WriteLine("  --end, -e <time>          End time (HH:MM:SS)");
        Console.WriteLine("  --audio, -a               Audio only (MP3)");
        Console.WriteLine("  --verbose, -v             Verbose logging");
    }

    private static async Task RunInteractiveModeAsync(CancellationToken ct)
    {
        var serviceProvider = ConfigureServices();

        try
        {
            var environmentService = new EnvironmentService();
            var consoleService = new ConsoleService();
            var fileService = new FileService();
            using var loggerService = new FileLoggerService(environmentService);
            var errorHandler = new ErrorHandler(loggerService);

            // Criar State Manager para operações de resume
            var stateManager = new DownloadStateManager(environmentService, loggerService);

            // Executar cleanup no startup
            var cleanupService = new StateCleanupService(stateManager, loggerService);
            await cleanupService.CleanupOnStartupAsync();

            // Use Domain-based workflow with DI
            var downloadService = serviceProvider.GetRequiredService<IDownloadService>();
            var metadataService = serviceProvider.GetRequiredService<IMetadataService>();

            using var app = new DomainWorkflow(
                new MenuService(),
                metadataService,
                downloadService,
                consoleService,
                fileService,
                ct
            );

            var result = await app.RunAsync();

            if (result.IsFailure)
            {
                Console.WriteLine($"\n❌ {result.ErrorMessage}");
                Environment.Exit(1);
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("\n✓ Operação cancelada com sucesso.");
            Environment.Exit(1);
        }
    }

    private static async Task RunResumeModeAsync(CancellationToken ct)
    {
        var environmentService = new EnvironmentService();
        var consoleService = new ConsoleService();
        using var loggerService = new FileLoggerService(environmentService);
        var stateManager = new DownloadStateManager(environmentService, loggerService);

        await ResumeDownloadAsync(stateManager, consoleService, ct);
    }

    private static async Task<int> RunDownloadCommandAsync(string[] args, CancellationToken ct)
    {
        var parsedArgs = ParseArgs(args[1..]); // Skip "download" command

        // Get URL (first non-option argument)
        var url = args.Skip(1).FirstOrDefault(a => !a.StartsWith("-"));
        if (string.IsNullOrEmpty(url))
        {
            Console.WriteLine("❌ Error: URL is required");
            return 1;
        }

        // Get options
        var apiUrl = parsedArgs.GetValueOrDefault("api-url") ?? parsedArgs.GetValueOrDefault("a");
        var forceLocal = parsedArgs.ContainsKey("local") || parsedArgs.ContainsKey("l");
        var verbose = parsedArgs.ContainsKey("verbose") || parsedArgs.ContainsKey("v");
        var output = parsedArgs.GetValueOrDefault("output") ?? parsedArgs.GetValueOrDefault("o");
        var start = parsedArgs.GetValueOrDefault("start") ?? parsedArgs.GetValueOrDefault("s");
        var end = parsedArgs.GetValueOrDefault("end") ?? parsedArgs.GetValueOrDefault("e");
        var audio = parsedArgs.ContainsKey("audio") || parsedArgs.ContainsKey("a"); // Note: -a conflicts with api-url

        // Fix for -a ambiguity: if "audio" is explicitly set or if audio is the only -a
        if (args.Contains("--audio"))
        {
            audio = true;
        }

        return await HandleDownloadAsync(url, output, start, end, audio, apiUrl, forceLocal, verbose, ct);
    }

    private static async Task RunConfigCommandAsync(string[] args)
    {
        var configService = new ConfigService();

        if (args.Length > 1 && args[1] == "show")
        {
            var config = await configService.LoadAsync();
            Console.WriteLine("=== Cutube Configuration ===");
            Console.WriteLine($"Config File: {configService.GetConfigPath()}");
            Console.WriteLine($"Exists: {configService.ConfigExists()}");
            Console.WriteLine();
            Console.WriteLine($"API URL: {config.ApiUrl ?? "(not set - local mode)"}");
            Console.WriteLine($"Default Output: {config.DefaultOutputPath}");
            Console.WriteLine($"Max Concurrent: {config.MaxConcurrentDownloads}");
            Console.WriteLine($"Timeout: {config.TimeoutSeconds}s");
            Console.WriteLine($"Verbose Logging: {config.VerboseLogging}");
        }
        else if (args.Length > 1 && args[1] == "reset")
        {
            await configService.SaveAsync(ConfigDefaults.CreateDefault());
            Console.WriteLine("Configuration reset to defaults.");
            Console.WriteLine($"Location: {configService.GetConfigPath()}");
        }
        else
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("  cutube config show    Show configuration");
            Console.WriteLine("  cutube config reset   Reset configuration");
        }
    }

    private static async Task<int> HandleDownloadAsync(
        string url,
        string? output,
        string? start,
        string? end,
        bool audio,
        string? apiUrlFlag,
        bool forceLocal,
        bool verbose,
        CancellationToken ct)
    {
        // 1. Load configuration
        var configService = new ConfigService();
        var config = await configService.LoadAsync();

        // 2. Determine operation mode
        var useApi = DetermineMode(apiUrlFlag, forceLocal, config);

        // 3. Create services
        var serviceProvider = BuildServiceProvider(useApi ? apiUrlFlag ?? config.ApiUrl! : null, config, verbose);

        var downloadService = serviceProvider.GetRequiredService<IDownloadService>();

        // 4. Log the mode
        if (useApi)
        {
            Console.WriteLine($"Mode: API Remote ({apiUrlFlag ?? config.ApiUrl})");
        }
        else
        {
            Console.WriteLine("Mode: Local (Standalone)");
        }

        // 5. Create request
        var request = CreateDownloadRequest(url, output, start, end, audio, config);

        // 6. Execute download
        var progress = new Progress<DownloadProgress>(p =>
        {
            Console.Write($"\rProgress: {p.Percentage:F1}% | " +
                         $"Speed: {FormatSpeed(p.Speed)} | " +
                         $"State: {p.State}");
        });

        try
        {
            var result = await downloadService.DownloadAsync(request, progress, ct);

            if (result.IsFailed)
            {
                Console.WriteLine($"\n❌ Error: {result.Errors.First().Message}");
                return 1;
            }

            Console.WriteLine($"\n✅ Download completed: {result.Value.FilePath}");
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ Unexpected error: {ex.Message}");
            return 1;
        }
    }

    private static bool DetermineMode(string? apiUrlFlag, bool forceLocal, AppConfig config)
    {
        // Flag --local takes precedence
        if (forceLocal)
        {
            return false;
        }

        // Flag --api-url overrides config file
        if (!string.IsNullOrWhiteSpace(apiUrlFlag))
        {
            return true;
        }

        // Use config file
        return config.UseApi;
    }

    private static ServiceProvider BuildServiceProvider(string? apiUrl, AppConfig config, bool verbose)
    {
        var services = new ServiceCollection();

        // Services
        if (apiUrl is not null)
        {
            // API mode
            var apiConfig = new AppConfig { ApiUrl = apiUrl };
            var environmentService = new EnvironmentService();
            var loggerService = new FileLoggerService(environmentService);

            services.AddSingleton<IDownloadService>(sp =>
            {
                return new ApiClient(apiConfig, loggerService);
            });
        }
        else
        {
            // Local mode
            services.AddSingleton<IFileSystem, FileSystem>();
            services.AddSingleton<IVideoMetadataProvider, YtDlpMetadataProvider>();
            services.AddSingleton<IVideoDownloader, YtDlpDownloader>();
            services.AddSingleton<IVideoProcessor, FfmpegProcessor>();
            services.AddSingleton<IValidationService, FluentValidationService>();
            services.AddSingleton<IMetadataService, FluentMetadataService>();
            services.AddSingleton<IDownloadService, FluentDownloadService>();
            services.AddSingleton<IProcessingService, FluentProcessingService>();
        }

        return services.BuildServiceProvider();
    }

    private static DownloadRequest CreateDownloadRequest(
        string url,
        string? output,
        string? start,
        string? end,
        bool audio,
        AppConfig config)
    {
        TimeRange? timeRange = null;

        if (!string.IsNullOrEmpty(start) && !string.IsNullOrEmpty(end))
        {
            timeRange = new TimeRange
            {
                StartSeconds = (int)TimeSpan.Parse(start).TotalSeconds,
                EndSeconds = (int)TimeSpan.Parse(end).TotalSeconds
            };
        }

        return new DownloadRequest
        {
            Url = url,
            OutputPath = output ?? config.DefaultOutputPath,
            TimeRange = timeRange,
            AudioOnly = audio
        };
    }

    private static string FormatSpeed(float bytesPerSecond)
    {
        if (bytesPerSecond < 1024)
            return $"{bytesPerSecond:F1} B/s";
        if (bytesPerSecond < 1024 * 1024)
            return $"{bytesPerSecond / 1024:F1} KB/s";
        if (bytesPerSecond < 1024 * 1024 * 1024)
            return $"{bytesPerSecond / (1024 * 1024):F1} MB/s";
        return $"{bytesPerSecond / (1024 * 1024 * 1024):F1} GB/s";
    }

    /// <summary>
    /// Configures dependency injection container
    /// </summary>
    private static ServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        // Infrastructure Layer
        services.AddSingleton<IFileSystem, FileSystem>();
        services.AddSingleton<IVideoMetadataProvider, YtDlpMetadataProvider>();
        services.AddSingleton<IVideoDownloader, YtDlpDownloader>();
        services.AddSingleton<IVideoProcessor, FfmpegProcessor>();

        // Domain Services (FluentResults-based)
        services.AddSingleton<IValidationService, FluentValidationService>();
        services.AddSingleton<IMetadataService, FluentMetadataService>();
        services.AddSingleton<IDownloadService, FluentDownloadService>();
        services.AddSingleton<IProcessingService, FluentProcessingService>();

        return services.BuildServiceProvider();
    }

    /// <summary>
    /// Lista e retoma downloads interrompidos
    /// </summary>
    private static async Task ResumeDownloadAsync(
        IDownloadStateManager stateManager,
        IConsoleService consoleService,
        CancellationToken ct)
    {
        var activeStates = await stateManager.GetActiveStatesAsync();

        if (activeStates.Count == 0)
        {
            consoleService.WriteLine("✓ Nenhum download para resumir.");
            return;
        }

        consoleService.WriteLine($"\n📋 Downloads interrompidos ({activeStates.Count}):");

        for (int i = 0; i < activeStates.Count; i++)
        {
            var state = activeStates[i];
            consoleService.WriteLine($"\n  [{i + 1}] {state.Url}");
            consoleService.WriteLine($"      Status: {GetStatusEmoji(state.Status)} {state.Status}");
            consoleService.WriteLine($"      Progresso: {state.ProgressPercent}%");
            consoleService.WriteLine($"      Saída: {state.OutputPath}");

            if (!string.IsNullOrEmpty(state.ErrorMessage))
            {
                consoleService.WriteLine($"      Erro: {state.ErrorMessage}");
            }

            consoleService.WriteLine($"      Atualizado: {state.UpdatedAt:yyyy-MM-dd HH:mm}");
        }

        consoleService.Write("\nDigite o número do download para resumir (0 para cancelar): ");
        var input = consoleService.ReadLine();

        if (!int.TryParse(input, out var selection) || selection < 1 || selection > activeStates.Count)
        {
            consoleService.WriteLine("✓ Nenhum download selecionado.");
            return;
        }

        var selectedState = activeStates[selection - 1];
        consoleService.WriteLine($"\n▶ Retomando download: {selectedState.Url}");

        // TODO: Implementar retomada real do download
        // Por enquanto, apenas marcar como retomado
        consoleService.WriteLine("⚠️  Funcionalidade de retomada será implementada na próxima fase.");
        consoleService.WriteLine("   O estado foi identificado, mas o download ainda reinicia do zero.");
    }

    private static string GetStatusEmoji(DownloadStatus status) => status switch
    {
        DownloadStatus.Pending => "⏳",
        DownloadStatus.Downloading => "⬇️ ",
        DownloadStatus.Processing => "⚙️ ",
        DownloadStatus.Completed => "✅",
        DownloadStatus.Failed => "❌",
        DownloadStatus.Cancelled => "⏸️ ",
        _ => "❓"
    };
}
