using YoutubeDLSharp;

namespace cutube;

public interface IYtDlpService : IDisposable
{
    Task<string> GetVideoTitleAsync(string url);
    Task DownloadWithTimeRangeAsync(
        string url,
        string outputFile,
        string startTime,
        string endTime,
        IProgress<DownloadProgress>? progress = null);
}
