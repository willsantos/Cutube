using System.Diagnostics;
using System.Text.RegularExpressions;
using Cutube.Domain.Interfaces;
using Cutube.Domain.Models;

namespace Cutube.Infrastructure;

/// <summary>
/// Processes videos using FFmpeg (cutting, audio extraction, format conversion)
/// </summary>
public class FfmpegProcessor : IVideoProcessor
{
    private readonly string _ffmpegPath;

    /// <summary>
    /// Initializes a new instance of FfmpegProcessor
    /// </summary>
    /// <param name="ffmpegPath">Path to ffmpeg executable (null to use system PATH)</param>
    public FfmpegProcessor(string? ffmpegPath = null)
    {
        _ffmpegPath = ffmpegPath ?? "ffmpeg";
    }

    /// <inheritdoc/>
    public bool IsAvailable()
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = _ffmpegPath,
                Arguments = "-version",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            });
            process?.WaitForExit(5000);
            return process?.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<ProcessingResult> ProcessAsync(
        ProcessingRequest request,
        IProgress<ProcessingProgress>? progress = null,
        CancellationToken ct = default)
    {
        if (!File.Exists(request.InputPath))
            throw new FileNotFoundException($"Input file not found: {request.InputPath}");

        if (string.IsNullOrWhiteSpace(request.OutputPath))
            throw new ArgumentException("Output path cannot be empty", nameof(request));

        // Build FFmpeg arguments
        var arguments = BuildArguments(request);

        var processInfo = new ProcessStartInfo
        {
            FileName = _ffmpegPath,
            Arguments = arguments,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardError = true,
            RedirectStandardOutput = false
        };

        try
        {
            using var process = Process.Start(processInfo);
            if (process == null)
                throw new InvalidOperationException("Failed to start FFmpeg process");

            // Parse progress from stderr
            var duration = TimeSpan.Zero;
            var durationRegex = new Regex(@"Duration: (\d+):(\d+):(\d+)\.(\d+)");
            var progressRegex = new Regex(@"time=(\d+):(\d+):(\d+)\.(\d+)");

            // Read stderr line by line for progress
            var reader = process.StandardError;
            string? line;

            while ((line = await reader.ReadLineAsync(ct)) != null)
            {
                if (ct.IsCancellationRequested)
                {
                    process.Kill(entireProcessTree: true);
                    ct.ThrowIfCancellationRequested();
                }

                // Extract duration
                if (line.Contains("Duration"))
                {
                    var match = durationRegex.Match(line);
                    if (match.Success)
                    {
                        duration = new TimeSpan(
                            0,
                            int.Parse(match.Groups[1].Value),
                            int.Parse(match.Groups[2].Value),
                            int.Parse(match.Groups[3].Value),
                            int.Parse(match.Groups[4].Value) * 10
                        );
                    }
                }

                // Extract progress
                if (line.Contains("time="))
                {
                    var match = progressRegex.Match(line);
                    if (match.Success && duration > TimeSpan.Zero)
                    {
                        var currentTime = new TimeSpan(
                            0,
                            int.Parse(match.Groups[1].Value),
                            int.Parse(match.Groups[2].Value),
                            int.Parse(match.Groups[3].Value),
                            int.Parse(match.Groups[4].Value) * 10
                        );

                        var percentage = (currentTime.TotalMilliseconds / duration.TotalMilliseconds) * 100;

                        progress?.Report(new ProcessingProgress
                        {
                            Percentage = (int)percentage,
                            CurrentOperation = "Processing"
                        });
                    }
                }
            }

            await process.WaitForExitAsync(ct);

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException($"FFmpeg failed with exit code {process.ExitCode}");
            }

            // Get output file info
            var fileInfo = new FileInfo(request.OutputPath);
            return new ProcessingResult
            {
                Success = true,
                OutputPath = request.OutputPath,
                FileSizeBytes = fileInfo.Length,
                ErrorMessage = null
            };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"FFmpeg processing failed: {ex.Message}", ex);
        }
    }

    private string BuildArguments(ProcessingRequest request)
    {
        var args = new List<string>();

        // Overwrite output file without asking (before input to avoid prompts)
        args.Add("-y");

        // Input file
        args.Add("-i");
        args.Add($"\"{request.InputPath}\"");

        // Time range
        if (request.TimeRange != null)
        {
            args.Add("-ss");
            args.Add(request.TimeRange.StartSeconds.ToString());
            args.Add("-to");
            args.Add(request.TimeRange.EndSeconds.ToString());
        }

        // Codec and format settings
        if (request.AudioOnly)
        {
            // Audio extraction: no video, MP3 with high quality
            args.Add("-vn");
            args.Add("-c:a");
            args.Add("libmp3lame");
            args.Add("-q:a");
            args.Add("2");
        }
        else
        {
            // Video: copy streams when possible (fast), re-encode if cutting
            if (request.TimeRange != null)
            {
                args.Add("-c:v");
                args.Add("libx264");
                args.Add("-c:a");
                args.Add("aac");
            }
            else
            {
                args.Add("-c");
                args.Add("copy");
            }
        }

        // Output file
        args.Add($"\"{request.OutputPath}\"");

        return string.Join(" ", args);
    }
}
