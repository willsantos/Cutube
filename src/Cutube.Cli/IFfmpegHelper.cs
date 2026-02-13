using System.Threading;

namespace Cutube.Cli;

public interface IFfmpegHelper
{
    void ExecuteFfmpeg(string arguments, IProgress<int> progress, CancellationToken ct);
}
