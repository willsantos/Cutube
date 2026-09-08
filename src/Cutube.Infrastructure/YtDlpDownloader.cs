using System.Diagnostics;
using Cutube.Domain.Interfaces;
using Cutube.Domain.Models;
using YoutubeDLSharp;
using YoutubeDLSharp.Options;
using YtdlDownloadProgress = YoutubeDLSharp.DownloadProgress;

namespace Cutube.Infrastructure;

/// <summary>
/// Downloads videos using yt-dlp
/// </summary>
public class YtDlpDownloader : IVideoDownloader
{
    private readonly YoutubeDL _ytdl;
    private readonly string _ytDlpPath;

    /// <summary>
    /// Initializes a new instance of YtDlpDownloader using auto-resolved yt-dlp path
    /// </summary>
    public YtDlpDownloader()
    {
        _ytDlpPath = YtDlpPathResolver.Resolve();
        _ytdl = new YoutubeDL
        {
            YoutubeDLPath = _ytDlpPath
        };
    }

    /// <summary>
    /// Initializes a new instance of YtDlpDownloader with explicit path
    /// </summary>
    /// <param name="ytDlpPath">Path to yt-dlp executable</param>
    public YtDlpDownloader(string ytDlpPath)
    {
        _ytDlpPath = ytDlpPath;
        _ytdl = new YoutubeDL
        {
            YoutubeDLPath = _ytDlpPath
        };
    }

    /// <inheritdoc/>
    public async Task<DownloadResult> DownloadAsync(
        Cutube.Domain.Models.DownloadRequest request,
        IProgress<Cutube.Domain.Models.DownloadProgress>? progress = null,
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
        IProgress<YtdlDownloadProgress>? ytdlProgress = null;
        if (progress != null)
        {
            ytdlProgress = new Progress<YtdlDownloadProgress>(p => progress.Report(ConvertProgress(p)));
        }

        // Configure download options
        var options = new OptionSet
        {
            Output = request.OutputPath,
            Format = request.AudioOnly ? "bestaudio" : "bestvideo+bestaudio",
            MergeOutputFormat = DownloadMergeFormat.Mp4
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

        // yt-dlp pode sair com código 0 e imprimir o caminho final mesmo quando
        // a mesclagem falha (ex.: ffmpeg ausente) — validar que o arquivo existe
        // para dar um erro acionável em vez de "Could not find file"
        var filePath = result.Data;
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            throw new InvalidOperationException(
                $"yt-dlp finalizou com sucesso mas o arquivo final não foi criado ('{filePath}'). " +
                "Causa mais comum: FFmpeg ausente, necessário para mesclar vídeo+áudio. " +
                "Instale o FFmpeg (Windows: winget install Gyan.FFmpeg) e tente novamente.");
        }

        // Get file info
        var fileInfo = new FileInfo(filePath);

        return new DownloadResult
        {
            Success = true,
            OutputPath = filePath,
            FileSizeBytes = fileInfo.Length,
            Duration = TimeSpan.FromSeconds(0), // Duration would need to be extracted from metadata
            ErrorMessage = null
        };
    }

    private static Cutube.Domain.Models.DownloadProgress ConvertProgress(YtdlDownloadProgress ytdlProgress)
    {
        return new Cutube.Domain.Models.DownloadProgress
        {
            State = ytdlProgress.State.ToString() ?? "downloading",
            Percentage = ytdlProgress.Progress,
            DownloadedBytes = 0, // Not directly available from YtdlDownloadProgress
            TotalBytes = 0,      // Not directly available from YtdlDownloadProgress
            Speed = 0,           // Speed property name might be different
            ErrorMessage = null  // Error property name might be different
        };
    }
}
