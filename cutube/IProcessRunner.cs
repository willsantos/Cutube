using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace cutube;

public interface IProcessRunner
{
    void Run(ProcessStartInfo startInfo, Action<string?> onErrorData);
}

[ExcludeFromCodeCoverage]
public class ProcessRunner : IProcessRunner
{
    public void Run(ProcessStartInfo startInfo, Action<string?> onErrorData)
    {
        using var process = new Process();
        process.StartInfo = startInfo;
        process.ErrorDataReceived += (_, args) => onErrorData(args.Data);
        process.Start();
        process.BeginErrorReadLine();
        process.WaitForExit();
    }
}
