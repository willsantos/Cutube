using System.Diagnostics;
using cutube;

namespace Cutube.Tests.Helpers;

public class FakeProcessRunner : IProcessRunner
{
    private readonly IEnumerable<string?> _stderrLines;

    public FakeProcessRunner(IEnumerable<string?> stderrLines)
    {
        _stderrLines = stderrLines;
    }

    public ProcessStartInfo? LastStartInfo { get; private set; }

    public void Run(ProcessStartInfo startInfo, Action<string?> onErrorData)
    {
        LastStartInfo = startInfo;
        foreach (var line in _stderrLines)
        {
            onErrorData(line);
        }
    }
}
