using System.Diagnostics;
using Cutube.Domain.Interfaces;
using Cutube.Domain.Models;
using YoutubeDLSharp;
using YoutubeDLSharp.Options;

namespace Cutube.Infrastructure;

/// <summary>
/// Downloads videos using yt-dlp
/// </summary>
public class YtDlpDownloader : IVideoDownloader
{
    private readonly YoutubeDL _ytdl;
    private readonly string _ytDlpPath;

    /// <summary>
    /// Initializes a new instance of YtDlpDownloader
    /// </summary>
    /// <param name="ytDlpPath">Path to yt-dlp executable (null to use system PATH)</param>
    public YtDlpDownloader(string? ytDlpPath = null)
    {
        _ytDlpPath = ytDlpPath ?? "yt-dlp";
        _ytdl = new YoutubeDL
        {
            YoutubeDLPath = _ytDlpPath
        };
    }

    /// <inheritdoc/>
    public async Task<DownloadResult> DownloadAsync(
        DownloadRequest request,
        IProgress<DownloadProgress>? progress = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Url))
            throw new ArgumentException("URL cannot be empty", nameof(request));

        if (string.IsNullOrWhiteSpace(request.OutputPath))
            throw new ArgumentException("Output path cannot be empty", nameof(request));

        // Ensure output directory exists
        var directory = Path.GetDirectoryName(request.OutputPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // Convert Domain progress to YoutubeDLSharp progress
        var ytdlProgress = progress != null
            ? new Progress<DownloadProgress>(p => progress.Report(ConvertProgress(p)))
            : null;

        // Configure download options
        var options = new OptionSet
        {
            Output = request.OutputPath,
            Format = request.AudioOnly ? "bestaudio" : "bestvideo+bestaudio",
            MergeOutputFormat = request.AudioOnly ? DownloadMergeFormat.Mp3 : DownloadMergeFormat.Mp4
        };

        // Add time range if specified
        if (request.TimeRange != null)
        {
            options.DownloadSections = $"{request.TimeRange.StartSeconds}-{request.TimeRange.EndSeconds}";
        }

        // Run download
        var result = await _ytdl.RunVideoDownload(
            request.Url,
            overrideOptions: options,
            progress: ytdlProgress,
            ct: ct
        );

        if (!result.Success)
        {
            throw new InvalidOperationException($"Download failed: {result.ErrorOutput}");
        }

        // Get file info
        var filePath = result.Data;
        var fileInfo = new FileInfo(filePath);

        return new DownloadResult
        {
            FilePath = filePath,
            Size = fileInfo.Length,
            Duration = TimeSpan.FromSeconds(0) // Duration would need to be extracted from metadata
        };
    }

    private static DownloadProgress ConvertProgress(YoutubeDLSharp.DownloadProgress ytdlProgress)
    {
        return new DownloadProgress
        {
            State = ytdlProgress.State ?? "downloading",
            Percentage = ytdlProgress.Progress,
            DownloadedBytes = ytdlProgress.Downloaded,
            TotalBytes = ytdlProgress.Total,
            Speed = ytdlProgress.Speed,
            ErrorMessage = ytdlProgress.Error
        };
    }
}
