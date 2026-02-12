using System.Net;
using Polly;
using Polly.Extensions.Http;

namespace Cutube.Worker.Configuration;

public static class HttpRetryPolicyFactory
{
    public static IAsyncPolicy<HttpResponseMessage> Create(Action<string>? onRetryLog = null)
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(ShouldRetry)
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (outcome, timespan, retryAttempt, context) =>
                {
                    var statusCode = outcome.Result?.StatusCode;
                    var reason = outcome.Exception?.Message ?? outcome.Result?.ReasonPhrase ?? "Unknown reason";
                    onRetryLog?.Invoke($"Retry {retryAttempt} after {timespan.TotalSeconds}s due to status {(int?)statusCode ?? 0} - {reason}");
                });
    }

    public static bool ShouldRetry(HttpResponseMessage response)
    {
        return response.StatusCode == HttpStatusCode.RequestTimeout || (int)response.StatusCode >= 500;
    }
}
