using System.Text;
using System.Text.Json;
using Polly.CircuitBreaker;

namespace Cutube.Worker.Services;

/// <summary>
/// Implementation of status notification service via HTTP callbacks to the API.
/// </summary>
public class DownloadStatusNotificationService : IDownloadStatusNotificationService
{
    private static readonly System.Diagnostics.Metrics.Meter Meter = new("Cutube.Worker.Notifications", "1.0.0");
    private static readonly System.Diagnostics.Metrics.Counter<long> SuccessCounter =
        Meter.CreateCounter<long>("cutube.worker.notifications.success");
    private static readonly System.Diagnostics.Metrics.Counter<long> FailureCounter =
        Meter.CreateCounter<long>("cutube.worker.notifications.failure");

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

        await SendAsync(correlationId, "status", url, HttpMethod.Patch, payload, cancellationToken);
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

        await SendAsync(correlationId, "progress", url, HttpMethod.Post, payload, cancellationToken);
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

        await SendAsync(correlationId, "deadletter", url, HttpMethod.Patch, payload, cancellationToken);
    }

    private async Task SendAsync(
        string correlationId,
        string notificationType,
        string url,
        HttpMethod method,
        object payload,
        CancellationToken cancellationToken)
    {
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
            ["NotificationType"] = notificationType
        });

        try
        {
            var content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json");

            using var request = new HttpRequestMessage(method, url) { Content = content };
            using var response = await _httpClient.SendAsync(request, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                SuccessCounter.Add(1);
                _logger.LogDebug("Notification sent successfully with status code {StatusCode}", response.StatusCode);
                return;
            }

            FailureCounter.Add(1);

            if ((int)response.StatusCode >= 400 && (int)response.StatusCode < 500)
            {
                _logger.LogWarning(
                    "Permanent notification failure (no retry). Status code: {StatusCode}",
                    response.StatusCode);
                return;
            }

            _logger.LogError("Notification failed after retry policy. Status code: {StatusCode}", response.StatusCode);
        }
        catch (BrokenCircuitException ex)
        {
            FailureCounter.Add(1);
            _logger.LogWarning(ex, "Notification skipped because circuit breaker is open");
        }
        catch (Exception ex)
        {
            FailureCounter.Add(1);
            _logger.LogError(ex, "Unexpected error while notifying API");
        }
    }
}
