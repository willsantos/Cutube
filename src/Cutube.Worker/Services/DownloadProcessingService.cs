using Cutube.Contracts.Messages;
using Cutube.Worker.Configuration;
using Cutube.Worker.Services.Models;
using Microsoft.Extensions.Options;
using Serilog.Context;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Cutube.Worker.Services;

/// <summary>
/// Implementation of the download processing service using yt-dlp.
/// </summary>
public partial class DownloadProcessingService : IDownloadProcessingService
{
    private readonly IDownloadStatusNotificationService _notificationService;
    private readonly ILogger<DownloadProcessingService> _logger;
    private readonly WorkerOptions _options;

    public DownloadProcessingService(
        IDownloadStatusNotificationService notificationService,
        ILogger<DownloadProcessingService> logger,
        IOptions<WorkerOptions> options)
    {
        _notificationService = notificationService;
        _logger = logger;
        _options = options.Value;
    }

    public async Task ProcessDownloadAsync(DownloadMessage message, CancellationToken cancellationToken = default)
    {
        var correlationId = message.CorrelationId;
        using var correlationScope = LogContext.PushProperty("CorrelationId", correlationId);
        using var messageScope = LogContext.PushProperty("MessageId", message.MessageId);
        var totalStopwatch = Stopwatch.StartNew();
        var notifyStopwatch = Stopwatch.StartNew();

        _logger.LogInformation("Starting download: {CorrelationId}, URL: {Url}", correlationId, message.Url);

        try
        {
            // Notify processing start
            await _notificationService.NotifyStatusAsync(correlationId, "processing", cancellationToken);
            notifyStopwatch.Stop();
            _logger.LogDebug("Processing notification sent in {ElapsedMs}ms", notifyStopwatch.ElapsedMilliseconds);

            // Create output directory if it doesn't exist
            var outputDirectory = Path.GetDirectoryName(message.OutputPath);
            if (!string.IsNullOrEmpty(outputDirectory) && !Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
                _logger.LogDebug("Created output directory: {Dir}", outputDirectory);
            }

            // Build yt-dlp arguments
            var arguments = BuildYtDlpArguments(message);

            _logger.LogDebug("yt-dlp arguments: {Args}", arguments);

            // Execute yt-dlp
            var exitCode = await ExecuteYtDlpAsync(
                arguments,
                correlationId,
                cancellationToken);

            _logger.LogDebug("yt-dlp execution finished with exit code {ExitCode}", exitCode);

            if (exitCode == 0)
            {
                // Success
                await _notificationService.NotifyStatusAsync(correlationId, "completed", cancellationToken);
                _logger.LogInformation("Download completed: {CorrelationId}", correlationId);
                _logger.LogInformation("Total processing duration: {ElapsedMs}ms", totalStopwatch.ElapsedMilliseconds);
            }
            else if (exitCode == 1 && cancellationToken.IsCancellationRequested)
            {
                // Cancelled by user (CTRL+C on worker, not CTRL+C on yt-dlp)
                await _notificationService.NotifyStatusAsync(correlationId, "cancelled", cancellationToken);
                _logger.LogWarning("Download cancelled: {CorrelationId}", correlationId);
            }
            else
            {
                // Error
                await _notificationService.NotifyStatusAsync(
                    correlationId,
                    "failed",
                    cancellationToken,
                    $"yt-dlp exited with code {exitCode}");

                throw new Exception($"yt-dlp failed with exit code {exitCode}");
            }
        }
        catch (OperationCanceledException)
        {
            await _notificationService.NotifyStatusAsync(correlationId, "cancelled", cancellationToken);
            _logger.LogWarning("Download cancelled by user: {CorrelationId}", correlationId);
            throw;
        }
        catch (Exception ex)
        {
            await _notificationService.NotifyStatusAsync(
                correlationId,
                "failed",
                cancellationToken,
                ex.Message);

            _logger.LogError(ex, "Download failed: {CorrelationId}", correlationId);
            _logger.LogInformation("Total processing duration: {ElapsedMs}ms", totalStopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    private string BuildYtDlpArguments(DownloadMessage message)
    {
        var args = new List<string>();

        // Output filename
        var outputFilename = !string.IsNullOrEmpty(message.OutputFilename)
            ? message.OutputFilename
            : "%(title)s.%(ext)s";

        args.Add("--newline"); // Progress in one line
        args.Add("--no-playlist"); // Don't download entire playlist
        args.Add("--ignore-errors"); // Continue on error
        args.Add("--no-overwrites"); // Don't overwrite existing files

        // Audio only
        if (message.AudioOnly)
        {
            args.Add("-x"); // Audio extraction
            args.Add("--audio-format"); // Audio format
            args.Add("mp3");
        }

        // Time range (cut)
        if (!string.IsNullOrEmpty(message.StartTime) && !string.IsNullOrEmpty(message.EndTime))
        {
            args.Add("--download-sections");
            args.Add($"*{message.StartTime}-{message.EndTime}");
        }

        // Output path
        args.Add("-o");
        args.Add(Path.Combine(message.OutputPath, outputFilename));

        // URL (last argument)
        args.Add(message.Url);

        return string.Join(" ", args);
    }

    private async Task<int> ExecuteYtDlpAsync(
        string arguments,
        string correlationId,
        CancellationToken cancellationToken)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "yt-dlp",
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();

        // Task to read output and parse progress
        var progressTask = ReadProgressAsync(
            process.StandardOutput,
            correlationId,
            cancellationToken);

        // Task to read errors
        var errorTask = ReadErrorsAsync(
            process.StandardError,
            correlationId,
            cancellationToken);

        // Wait for process to finish
        await process.WaitForExitAsync(cancellationToken);

        // Wait for reading tasks to finish
        await Task.WhenAll(progressTask, errorTask);

        return process.ExitCode;
    }

    private async Task ReadProgressAsync(
        StreamReader reader,
        string correlationId,
        CancellationToken cancellationToken)
    {
        var lastUpdate = DateTime.MinValue;
        var updateInterval = TimeSpan.FromMilliseconds(_options.ProgressUpdateInterval);

        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            // Parse yt-dlp progress line
            var progress = ParseYtDlpProgress(line, correlationId);

            if (progress != null)
            {
                _logger.LogDebug(
                    "Download progress: {CorrelationId}, {Progress}%, Speed: {Speed}",
                    correlationId,
                    progress.Progress,
                    progress.Speed);

                // Throttle updates to avoid flooding the API
                var now = DateTime.UtcNow;
                if (now - lastUpdate >= updateInterval || progress.Progress == 100)
                {
                    await _notificationService.NotifyProgressAsync(
                        progress.CorrelationId,
                        progress.Progress,
                        progress.Speed,
                        progress.DownloadedBytes,
                        progress.TotalBytes,
                        progress.Eta,
                        cancellationToken);

                    lastUpdate = now;
                }
            }
        }
    }

    private async Task ReadErrorsAsync(
        StreamReader reader,
        string correlationId,
        CancellationToken cancellationToken)
    {
        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            _logger.LogWarning("yt-dlp stderr [{CorrelationId}]: {Line}", correlationId, line);
        }
    }

    private DownloadProgress? ParseYtDlpProgress(string line, string correlationId)
    {
        // Example yt-dlp line:
        // [download]  45.0% of 100.00MiB at  2.00MiB/s ETA 00:00:05

        var match = YtDlpProgressRegex().Match(line);
        if (!match.Success)
        {
            return null;
        }

        var progress = new DownloadProgress
        {
            CorrelationId = correlationId,
            Progress = int.Parse(match.Groups["percent"].Value, CultureInfo.InvariantCulture),
            Speed = ParseSpeed(match.Groups["speed"].Value),
            DownloadedBytes = ParseBytes(match.Groups["downloaded"].Value, match.Groups["downloadedUnit"].Value),
            Eta = match.Groups["eta"].Value
        };

        return progress;
    }

    private double ParseSpeed(string speed)
    {
        // Example: "2.00MiB/s", "500.00KiB/s"
        var match = SpeedRegex().Match(speed);
        if (!match.Success)
        {
            return 0;
        }

        var value = double.Parse(match.Groups["value"].Value, CultureInfo.InvariantCulture);
        var unit = match.Groups["unit"].Value.ToUpperInvariant();

        return unit switch
        {
            "MIB/S" => value * 1024 * 1024,
            "KIB/S" => value * 1024,
            "GIB/S" => value * 1024 * 1024 * 1024,
            _ => value
        };
    }

    private long ParseBytes(string value, string unit)
    {
        var num = double.Parse(value, CultureInfo.InvariantCulture);
        var unitUpper = unit.ToUpperInvariant();

        return unitUpper switch
        {
            "MIB" => (long)(num * 1024 * 1024),
            "KIB" => (long)(num * 1024),
            "GIB" => (long)(num * 1024 * 1024 * 1024),
            _ => (long)num
        };
    }

    [GeneratedRegex(@"\[download\]\s+(?<percent>\d+\.?\d*)% of (?<downloaded>[\d.]+)(?<downloadedUnit>MiB|GiB|KiB) at (?<speed>[\d.]+\s*[KMG]iB/s) ETA (?<eta>[\d:]+)")]
    private static partial Regex YtDlpProgressRegex();

    [GeneratedRegex(@"(?<value>[\d.]+)\s*(?<unit>KiB/s|MiB/s|GiB/s)")]
    private static partial Regex SpeedRegex();
}
