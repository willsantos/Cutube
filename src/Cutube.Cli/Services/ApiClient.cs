using System.Net.Http.Json;
using Cutube.Cli.Configuration;
using Cutube.Domain.Models;
using Cutube.Domain.Services;
using Cutube.Cli.Recovery;
using Cutube.Cli.Logging;
using FluentResults;

namespace Cutube.Cli.Services;

/// <summary>
/// HTTP client for communicating with remote Cutube API
/// Implements IDownloadService to work as a drop-in replacement for local downloads
/// </summary>
public sealed class ApiClient : IDownloadService, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly AppConfig _config;
    private readonly ILoggerService _logger;

    public ApiClient(AppConfig config, ILoggerService logger)
    {
        _config = config;
        _logger = logger;

        // Configure HttpClient
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(config.ApiUrl!),
            Timeout = TimeSpan.FromSeconds(config.TimeoutSeconds)
        };

        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Cutube-CLI/2.0");
    }

    /// <summary>
    /// Downloads a video via the remote API
    /// </summary>
    public async Task<Result<DomainDownloadResult>> DownloadAsync(
        DownloadRequest request,
        IProgress<DownloadProgress>? progress = null,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInfo("Sending download to API: {Url}", ("Url", request.Url));

            // 1. Create download via API
            var createResponse = await _httpClient.PostAsJsonAsync(
                "/api/downloads",
                new
                {
                    url = request.Url,
                    outputPath = request.OutputPath,
                    startTime = request.TimeRange?.StartSeconds.ToString(),
                    endTime = request.TimeRange?.EndSeconds.ToString(),
                    audioOnly = request.AudioOnly
                },
                ct
            );

            if (!createResponse.IsSuccessStatusCode)
            {
                var errorContent = await createResponse.Content.ReadAsStringAsync(ct);
                return Result.Fail($"API returned error {createResponse.StatusCode}: {errorContent}");
            }

            var createData = await createResponse.Content.ReadFromJsonAsync<CreateDownloadResponse>(ct);
            var downloadId = createData!.DownloadId;

            _logger.LogInfo("Download created in API: {DownloadId}", ("DownloadId", downloadId));

            // 2. Poll progress
            var lastProgress = 0f;

            while (!ct.IsCancellationRequested)
            {
                await Task.Delay(1000, ct); // Poll every 1s

                var statusResponse = await _httpClient.GetAsync($"/api/downloads/{downloadId}", ct);

                if (!statusResponse.IsSuccessStatusCode)
                {
                    return Result.Fail($"Error fetching status: {statusResponse.StatusCode}");
                }

                var status = await statusResponse.Content.ReadFromJsonAsync<DownloadDetails>(ct);

                // Report progress
                if (status!.Progress > lastProgress)
                {
                    progress?.Report(new DownloadProgress
                    {
                        State = status.Status,
                        Percentage = (float)status.Progress,
                        Speed = (float)status.Speed,
                        DownloadedBytes = status.DownloadedBytes ?? 0,
                        TotalBytes = status.TotalBytes ?? 0,
                        ErrorMessage = status.ErrorMessage
                    });

                    lastProgress = (float)status.Progress;
                }

                // Check if completed
                if (status.Status.Equals("completed", StringComparison.OrdinalIgnoreCase) ||
                    status.Status.Equals("failed", StringComparison.OrdinalIgnoreCase) ||
                    status.Status.Equals("cancelled", StringComparison.OrdinalIgnoreCase))
                {
                    if (status.Status.Equals("completed", StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogInfo("Download completed: {DownloadId}", ("DownloadId", downloadId));

                        return Result.Ok(new DomainDownloadResult
                        {
                            FilePath = status.FilePath ?? "unknown",
                            Size = status.TotalBytes ?? 0,
                            Duration = TimeSpan.Zero // API doesn't return duration in status
                        });
                    }
                    else if (status.Status.Equals("failed", StringComparison.OrdinalIgnoreCase))
                    {
                        return Result.Fail($"Download failed: {status.ErrorMessage}");
                    }
                    else
                    {
                        return Result.Fail("Download was cancelled");
                    }
                }
            }

            return Result.Fail("Download cancelled by user");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning("Connection error with API: {Message}", ("Message", ex.Message));
            return Result.Fail($"Connection error with API: {ex.Message}");
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            return Result.Fail("Timeout connecting to API");
        }
        catch (TaskCanceledException)
        {
            return Result.Fail("Download cancelled");
        }
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}

/// <summary>
/// Response from API when creating a download
/// </summary>
public record CreateDownloadResponse(
    string DownloadId,
    string Status,
    string? Message
);

/// <summary>
/// Download details from API status endpoint
/// </summary>
public record DownloadDetails(
    string DownloadId,
    string Url,
    string Status,
    double Progress,
    double Speed,
    string? Eta,
    long? DownloadedBytes,
    long? TotalBytes,
    string? FilePath,
    string? ErrorMessage
);
