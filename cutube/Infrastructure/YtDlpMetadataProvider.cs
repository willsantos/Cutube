using System.Diagnostics;
using System.Text.Json;
using Cutube.Domain.Interfaces;
using Cutube.Domain.Models;

namespace Cutube.Infrastructure;

/// <summary>
/// Provides video metadata using yt-dlp
/// </summary>
public class YtDlpMetadataProvider : IVideoMetadataProvider
{
    private readonly string _ytDlpPath;

    /// <summary>
    /// Initializes a new instance of YtDlpMetadataProvider
    /// </summary>
    /// <param name="ytDlpPath">Path to yt-dlp executable (null to use system PATH)</param>
    public YtDlpMetadataProvider(string? ytDlpPath = null)
    {
        _ytDlpPath = ytDlpPath ?? "yt-dlp";
    }

    /// <inheritdoc/>
    public async Task<VideoMetadata> GetMetadataAsync(string url, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentException("URL cannot be empty", nameof(url));

        var processInfo = new ProcessStartInfo
        {
            FileName = _ytDlpPath,
            Arguments = $"--dump-json --no-playlist \"{url}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(processInfo);
        if (process == null)
            throw new InvalidOperationException("Failed to start yt-dlp process");

        var output = await process.StandardOutput.ReadToEndAsync(ct);
        await process.WaitForExitAsync(ct);

        if (process.ExitCode != 0)
        {
            var error = await process.StandardError.ReadToEndAsync(ct);
            throw new InvalidOperationException($"Failed to get metadata: {error}");
        }

        try
        {
            var jsonData = JsonSerializer.Deserialize<JsonElement>(output);
            return ParseVideoMetadata(jsonData, url);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("Failed to parse yt-dlp JSON output", ex);
        }
    }

    private static VideoMetadata ParseVideoMetadata(JsonElement json, string url)
    {
        var title = json.GetProperty("title").GetString() ?? "Unknown";
        var uploader = json.GetProperty("uploader").GetString() ?? json.GetProperty("channel").GetString();
        var duration = json.GetProperty("duration").GetInt32();
        var uploadDateStr = json.GetProperty("upload_date").GetString() ?? "";

        // Parse upload date (format: YYYYMMDD)
        DateTime uploadDate = DateTime.TryParseExact(
            uploadDateStr,
            "yyyyMMdd",
            null,
            System.Globalization.DateTimeStyles.None,
            out var date) ? date : DateTime.MinValue;

        // Get thumbnail
        string thumbnailUrl = "https://via.placeholder.com/320x180?text=No+Thumbnail";
        if (json.TryGetProperty("thumbnail", out var thumbElem))
        {
            thumbnailUrl = thumbElem.GetString() ?? thumbnailUrl;
        }

        // Sanitize title for filename usage
        var sanitizedTitle = string.Join("_", title.Split(Path.GetInvalidFileNameChars()));

        return new VideoMetadata
        {
            Url = url,
            Title = sanitizedTitle,
            Uploader = uploader,
            Duration = TimeSpan.FromSeconds(duration),
            UploadDate = uploadDate,
            ThumbnailUrl = thumbnailUrl
        };
    }
}
