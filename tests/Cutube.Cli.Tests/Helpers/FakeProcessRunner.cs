using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Cutube.Cli;

namespace Cutube.Tests.Helpers;

public class FakeProcessRunner : IProcessRunner
{
    private readonly IEnumerable<string?> _stderrLines;

    public FakeProcessRunner(IEnumerable<string?> stderrLines)
    {
        _stderrLines = stderrLines;
    }

    public ProcessStartInfo? LastStartInfo { get; private set; }

    public Task RunAsync(ProcessStartInfo startInfo, Action<string?> onErrorData, CancellationToken ct = default)
    {
        LastStartInfo = startInfo;
        foreach (var line in _stderrLines)
        {
            onErrorData(line);
        }
        return Task.CompletedTask;
    }
}
