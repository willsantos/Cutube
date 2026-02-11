using System.Text;
using System.Text.Json;

namespace Cutube.Worker.Services;

/// <summary>
/// Implementation of status notification service via HTTP callbacks to the API.
/// </summary>
public class DownloadStatusNotificationService : IDownloadStatusNotificationService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<DownloadStatusNotificationService> _logger;

    public DownloadStatusNotificationService(
        HttpClient httpClient,
        ILogger<DownloadStatusNotificationService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task NotifyStatusAsync(
        string correlationId,
        string status,
        CancellationToken cancellationToken = default,
        string? errorMessage = null)
    {
        var url = $"/api/downloads/{correlationId}/status";

        var payload = new
        {
            state = status,
            errorMessage = errorMessage
        };

        try
        {
            var content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PatchAsync(url, content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Failed to notify status: {CorrelationId}, Status: {StatusCode}",
                    correlationId,
                    response.StatusCode);
            }
            else
            {
                _logger.LogDebug(
                    "Status notified: {CorrelationId}, Status: {Status}",
                    correlationId,
                    status);
            }
        }
        catch (Exception ex)
        {
            // Don't throw - notification failure should not break download
            _logger.LogError(ex,
                "Error notifying status: {CorrelationId}",
                correlationId);
        }
    }

    public async Task NotifyProgressAsync(
        string correlationId,
        int progress,
        double speed,
        long downloadedBytes,
        long? totalBytes = null,
        string? eta = null,
        CancellationToken cancellationToken = default)
    {
        var url = $"/api/downloads/{correlationId}/progress";

        var payload = new
        {
            progress,
            speed,
            downloadedBytes,
            totalBytes,
            eta
        };

        try
        {
            var content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync(url, content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogDebug(
                    "Failed to notify progress: {CorrelationId}, StatusCode: {StatusCode}",
                    correlationId,
                    response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            // Don't throw - notification failure should not break download
            _logger.LogDebug(ex,
                "Error notifying progress: {CorrelationId}",
                correlationId);
        }
    }

    public async Task NotifyDeadLetterAsync(
        string correlationId,
        string errorMessage,
        int retryCount,
        CancellationToken cancellationToken = default)
    {
        var url = $"/api/downloads/{correlationId}/status";

        var payload = new
        {
            state = "deadletter",
            errorMessage,
            retryCount
        };

        try
        {
            var content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PatchAsync(url, content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Failed to notify dead letter status: {CorrelationId}, Status: {StatusCode}",
                    correlationId,
                    response.StatusCode);
            }
            else
            {
                _logger.LogInformation(
                    "Dead letter status notified: {CorrelationId}, RetryCount: {RetryCount}",
                    correlationId,
                    retryCount);
            }
        }
        catch (Exception ex)
        {
            // Don't throw - notification failure should not break download
            _logger.LogError(ex,
                "Error notifying dead letter status: {CorrelationId}",
                correlationId);
        }
    }
}
