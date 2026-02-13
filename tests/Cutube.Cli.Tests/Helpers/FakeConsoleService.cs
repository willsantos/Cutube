using System.Text;
using Cutube.Cli;

namespace Cutube.Tests.Helpers;

public class FakeConsoleService : IConsoleService
{
    private readonly StringBuilder _output = new();
    private string? _readLineResponse;

    public bool IsOutputRedirected { get; set; }
    public int CursorLeft { get; set; }
    public int CursorTop { get; set; }

    public void Write(string value) => _output.Append(value);

    public void WriteLine(string message) => _output.AppendLine(message);

    public string? ReadLine() => _readLineResponse;

    public void SetReadLineResponse(string? response) => _readLineResponse = response;

    public string GetOutput() => _output.ToString();

    public char? GetLastNonWhitespaceChar()
    {
        var output = _output.ToString();
        for (var i = output.Length - 1; i >= 0; i--)
        {
            if (!char.IsWhiteSpace(output[i]))
            {
                return output[i];
            }
        }

        return null;
    }
}
