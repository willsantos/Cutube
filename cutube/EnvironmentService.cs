using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace cutube;

[ExcludeFromCodeCoverage]
public class EnvironmentService : IEnvironmentService
{
    public string GetFolderPath(Environment.SpecialFolder folder) => 
        Environment.GetFolderPath(folder);

    public string? GetEnvironmentVariable(string name) => 
        Environment.GetEnvironmentVariable(name);

    public bool IsWindows() => OperatingSystem.IsWindows();
    public bool IsLinux() => OperatingSystem.IsLinux();
    public bool IsMacOS() => OperatingSystem.IsMacOS();
    
    public string OSArchitecture => RuntimeInformation.OSArchitecture.ToString();
}
