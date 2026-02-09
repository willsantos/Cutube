using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Cutube.Domain.Interfaces;
using Cutube.Domain.Models;
using Cutube.Domain.Services;
using Cutube.Infrastructure;
using Cutube.Logging;
using Cutube.ErrorHandling;
using Cutube.Recovery;
using Microsoft.Extensions.DependencyInjection;

namespace cutube;

[ExcludeFromCodeCoverage]
public static class Program
{
    public static async Task Main(string[] args)
    {
        // Configure Dependency Injection
        var serviceProvider = ConfigureServices();

        using var cts = new CancellationTokenSource();

        Console.CancelKeyPress += (sender, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
            Console.WriteLine("\n⚠️  Cancelando operação...");
        };

        try
        {
            // TODO: Verify if it's --resume command and handle accordingly
            // For now, using existing workflow temporarily
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

            // Verificar se é comando --resume
            if (args.Contains("--resume"))
            {
                await ResumeDownloadAsync(stateManager, consoleService, cts.Token);
                return;
            }

            // TODO: This will be replaced with DI-based workflow
            // For now, keep existing functionality
            using var app = new ProgramWorkflow(
                new MenuService(),
                new YtDlpHelper(
                    fileService,
                    new HttpClientService(),
                    environmentService,
                    new ProcessService(),
                    consoleService,
                    false,
                    errorHandler,
                    loggerService,
                    stateManager
                ),
                consoleService,
                fileService,
                cts.Token,
                loggerService,
                errorHandler
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

        // Domain Services
        services.AddSingleton<IDownloadValidator, ValidationService>();

        // TODO: Add DownloadService, MetadataService, ProcessingService when implemented

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
