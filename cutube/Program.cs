using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Cutube.Logging;
using Cutube.ErrorHandling;

namespace cutube;

[ExcludeFromCodeCoverage]
public static class Program
{
    public static async Task Main()
    {
        using var cts = new CancellationTokenSource();
        
        Console.CancelKeyPress += (sender, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
            Console.WriteLine("\n⚠️  Cancelando operação...");
        };

        var environmentService = new EnvironmentService();
        using var loggerService = new FileLoggerService(environmentService);
        var errorHandler = new ErrorHandler(loggerService);

        try
        {
            using var app = new ProgramWorkflow(
                new MenuService(),
                new YtDlpHelper(),
                new ConsoleService(),
                new FileService(),
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
}
