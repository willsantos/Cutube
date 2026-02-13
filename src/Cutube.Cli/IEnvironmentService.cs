namespace cutube;

public interface IEnvironmentService
{
    string GetFolderPath(Environment.SpecialFolder folder);
    string? GetEnvironmentVariable(string name);
    bool IsWindows();
    bool IsLinux();
    bool IsMacOS();
    string OSArchitecture { get; }
}
