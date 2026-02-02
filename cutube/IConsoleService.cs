using System.Diagnostics.CodeAnalysis;

namespace cutube;

public interface IConsoleService
{
    bool IsOutputRedirected { get; }
    int CursorLeft { get; set; }
    int CursorTop { get; set; }
    void Write(string value);
    void WriteLine(string message);
    string? ReadLine();
}

[ExcludeFromCodeCoverage]
public class ConsoleService : IConsoleService
{
    public bool IsOutputRedirected => Console.IsOutputRedirected;
    public int CursorLeft
    {
        get => Console.CursorLeft;
        set => Console.CursorLeft = value;
    }

    public int CursorTop
    {
        get => Console.CursorTop;
        set => Console.CursorTop = value;
    }

    public void Write(string value) => Console.Write(value);
    public void WriteLine(string message) => Console.WriteLine(message);
    public string? ReadLine() => Console.ReadLine();
}
