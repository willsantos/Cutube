using System.Threading;
using YoutubeDLSharp;

namespace Cutube.Cli;

public interface IYtDlpService : IDisposable
{
    Task<string> GetVideoTitleAsync(string url, CancellationToken ct = default);
    Task DownloadWithTimeRangeAsync(
        string url,
        string outputFile,
        string startTime,
        string endTime,
        IProgress<DownloadProgress>? progress = null,
        CancellationToken ct = default);
    Task DownloadAudioAsync(
        string url,
        string outputFile,
        string startTime,
        string endTime,
        IProgress<DownloadProgress>? progress = null,
        CancellationToken ct = default);
}
