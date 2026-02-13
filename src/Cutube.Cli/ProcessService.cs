using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Cutube.Cli;

[ExcludeFromCodeCoverage]
public class ProcessService : IProcessService
{
    public IProcessWrapper Start(ProcessStartInfo startInfo)
    {
        var process = Process.Start(startInfo);
        return new ProcessWrapper(process ?? throw new InvalidOperationException("Failed to start process"));
    }
}

[ExcludeFromCodeCoverage]
public class ProcessWrapper : IProcessWrapper
{
    private readonly Process _process;

    public ProcessWrapper(Process process)
    {
        _process = process;
    }

    public async Task<int> WaitForExitAsync()
    {
        await _process.WaitForExitAsync();
        return _process.ExitCode;
    }
    
    public Task<string> StandardOutputReadToEndAsync() => 
        _process.StandardOutput.ReadToEndAsync();

    public int ExitCode => _process.ExitCode;
}
