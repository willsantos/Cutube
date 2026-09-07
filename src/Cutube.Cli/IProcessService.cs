namespace Cutube.Cli;

public interface IProcessService
{
    IProcessWrapper Start(System.Diagnostics.ProcessStartInfo startInfo);
}

public interface IProcessWrapper
{
    Task<int> WaitForExitAsync();
    Task<string> StandardOutputReadToEndAsync();
    int ExitCode { get; }
}
