using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace cutube;

public interface IProcessRunner
{
    Task RunAsync(ProcessStartInfo startInfo, Action<string?> onErrorData, CancellationToken ct = default);
}

[ExcludeFromCodeCoverage]
public class ProcessRunner : IProcessRunner
{
    public async Task RunAsync(ProcessStartInfo startInfo, Action<string?> onErrorData, CancellationToken ct = default)
    {
        using var process = new Process();
        process.StartInfo = startInfo;
        process.ErrorDataReceived += (_, args) => onErrorData(args.Data);
        process.Start();
        process.BeginErrorReadLine();
        await process.WaitForExitAsync(ct);
    }
}
