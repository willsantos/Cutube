using System.Diagnostics.CodeAnalysis;

namespace cutube;

[ExcludeFromCodeCoverage]
public static class Program
{
    public static async Task Main()
    {
        using var app = new ProgramWorkflow(
            new MenuService(),
            new YtDlpHelper(),
            new ConsoleService()
        );

        await app.RunAsync();
    }
}
