using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Cutube.Logging;

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

        try
        {
            using var app = new ProgramWorkflow(
                new MenuService(),
                new YtDlpHelper(),
                new ConsoleService(),
                new FileService(),
                cts.Token,
                loggerService
            );

            await app.RunAsync();
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("\n✓ Operação cancelada com sucesso.");
            Environment.Exit(1);
        }
    }
}
