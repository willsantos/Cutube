using System.Net;
using Polly;
using Polly.Extensions.Http;

namespace Cutube.Worker.Configuration;

public static class HttpRetryPolicyFactory
{
    public static IAsyncPolicy<HttpResponseMessage> CreateRetryPolicy(
        NotificationResilienceOptions options,
        Action<string>? onRetryLog = null)
    {
        var retryCount = Math.Max(0, options.MaxRetries);
        var initialDelaySeconds = Math.Max(1, options.InitialRetryDelaySeconds);

        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(ShouldRetry)
            .WaitAndRetryAsync(
                retryCount: retryCount,
                sleepDurationProvider: retryAttempt =>
                    TimeSpan.FromSeconds(initialDelaySeconds * Math.Pow(2, retryAttempt - 1)),
                onRetry: (outcome, timespan, retryAttempt, context) =>
                {
                    var statusCode = outcome.Result?.StatusCode;
                    var reason = outcome.Exception?.Message ?? outcome.Result?.ReasonPhrase ?? "Unknown reason";
                    onRetryLog?.Invoke($"Retry {retryAttempt} after {timespan.TotalSeconds}s due to status {(int?)statusCode ?? 0} - {reason}");
                });
    }

    public static IAsyncPolicy<HttpResponseMessage> CreateCircuitBreakerPolicy(
        NotificationResilienceOptions options,
        Action<string>? onCircuitLog = null)
    {
        var consecutiveFailures = Math.Max(1, options.ConsecutiveFailuresBeforeBreak);
        var breakSeconds = Math.Max(1, options.CircuitBreakSeconds);

        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(ShouldRetry)
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: consecutiveFailures,
                durationOfBreak: TimeSpan.FromSeconds(breakSeconds),
                onBreak: (outcome, timespan) =>
                {
                    var statusCode = outcome.Result?.StatusCode;
                    var reason = outcome.Exception?.Message ?? outcome.Result?.ReasonPhrase ?? "Unknown reason";
                    onCircuitLog?.Invoke($"Circuit opened for {timespan.TotalSeconds}s due to status {(int?)statusCode ?? 0} - {reason}");
                },
                onReset: () => onCircuitLog?.Invoke("Circuit reset"),
                onHalfOpen: () => onCircuitLog?.Invoke("Circuit half-open: next request is a trial"));
    }

    public static bool ShouldRetry(HttpResponseMessage response)
    {
        return response.StatusCode == HttpStatusCode.RequestTimeout || (int)response.StatusCode >= 500;
    }
}
