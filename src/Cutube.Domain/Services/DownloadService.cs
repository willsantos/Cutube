using Cutube.Domain.Interfaces;
using Cutube.Domain.Models;

namespace Cutube.Domain.Services;

/// <summary>
/// Service for downloading videos with optional time range processing
/// </summary>
public class DownloadService : IVideoDownloader
{
    private readonly IVideoDownloader _downloader;
    private readonly IVideoProcessor _processor;
    private readonly IDownloadValidator _validator;

    /// <summary>
    /// Creates a new DownloadService
    /// </summary>
    /// <param name="downloader">Underlying video downloader (e.g., yt-dlp wrapper)</param>
    /// <param name="processor">Video processor for time range extraction</param>
    /// <param name="validator">Validator for download parameters</param>
    public DownloadService(
        IVideoDownloader downloader,
        IVideoProcessor processor,
        IDownloadValidator validator)
    {
        _downloader = downloader;
        _processor = processor;
        _validator = validator;
    }

    /// <summary>
    /// Downloads a video with optional time range processing
    /// </summary>
    /// <param name="request">Download request with URL, output path, and options</param>
    /// <param name="progress">Optional progress reporter for download status</param>
    /// <param name="ct">Cancellation token for async operation</param>
    /// <returns>Download result with success status, output path, and file size</returns>
    /// <exception cref="ArgumentException">Thrown when request parameters are invalid</exception>
    /// <exception cref="OperationCanceledException">Thrown when operation is cancelled</exception>
    public async Task<DownloadResult> DownloadAsync(
        DownloadRequest request,
        IProgress<DownloadProgress>? progress = null,
        CancellationToken ct = default)
    {
        var urlValidation = _validator.ValidateUrl(request.Url);
        if (!urlValidation.IsValid)
            throw new ArgumentException(urlValidation.ErrorMessage);

        var outputDir = Path.GetDirectoryName(request.OutputPath) ?? Directory.GetCurrentDirectory();
        var dirValidation = _validator.ValidateDirectory(outputDir);
        if (!dirValidation.IsValid)
            throw new ArgumentException(dirValidation.ErrorMessage);

        if (request.TimeRange == null)
        {
            return await _downloader.DownloadAsync(request, progress, ct);
        }
        else
        {
            return await DownloadWithTimeRangeAsync(request, progress, ct);
        }
    }

    /// <summary>
    /// Downloads a video with time range processing
    /// </summary>
    /// <param name="request">Download request with time range</param>
    /// <param name="progress">Optional progress reporter</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Download result with processed video</returns>
    private async Task<DownloadResult> DownloadWithTimeRangeAsync(
        DownloadRequest request,
        IProgress<DownloadProgress>? progress,
        CancellationToken ct)
    {
        // Use .mp4 extension so yt-dlp doesn't rename the file
        var tempFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.mp4");
        string? actualDownloadedFile = null;

        try
        {
            var fullDownload = await _downloader.DownloadAsync(
                request with { OutputPath = tempFile },
                progress,
                ct
            );

            if (!fullDownload.Success)
                return fullDownload;

            // Use the actual path returned by the downloader (yt-dlp may change the extension)
            actualDownloadedFile = fullDownload.OutputPath;

            var processingRequest = new ProcessingRequest
            {
                InputPath = actualDownloadedFile,
                OutputPath = request.OutputPath,
                TimeRange = request.TimeRange,
                AudioOnly = request.AudioOnly
            };

            var processingResult = await _processor.ProcessAsync(processingRequest, null, ct);

            CleanupFile(tempFile);
            CleanupFile(actualDownloadedFile);

            return new DownloadResult
            {
                Success = processingResult.Success,
                OutputPath = processingResult.OutputPath,
                FileSizeBytes = processingResult.FileSizeBytes,
                Duration = TimeSpan.FromSeconds(request.TimeRange!.DurationSeconds),
                ErrorMessage = processingResult.ErrorMessage
            };
        }
        catch (OperationCanceledException)
        {
            CleanupFile(tempFile);
            CleanupFile(actualDownloadedFile);
            CleanupFile(request.OutputPath);
            throw;
        }
    }

    private static void CleanupFile(string? path)
    {
        try
        {
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
                File.Delete(path);
        }
        catch
        {
            // Ignore cleanup errors
        }
    }
}
