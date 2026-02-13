using Cutube.Cli.Logging;

namespace Cutube.Cli.ErrorHandling;

/// <summary>
/// Retry policy with exponential backoff
/// </summary>
public class RetryPolicy
{
    public int MaxRetries { get; }
    public TimeSpan InitialDelay { get; }
    public TimeSpan MaxDelay { get; }
    public Func<Exception, bool> ShouldRetryPredicate { get; }
    private readonly ILoggerService? _logger;

    public RetryPolicy(
        int maxRetries = 3,
        TimeSpan? initialDelay = null,
        TimeSpan? maxDelay = null,
        Func<Exception, bool>? shouldRetryPredicate = null,
        ILoggerService? logger = null)
    {
        if (maxRetries < 0)
            throw new ArgumentException("MaxRetries must be >= 0", nameof(maxRetries));

        MaxRetries = maxRetries;
        InitialDelay = initialDelay ?? TimeSpan.FromSeconds(1);
        MaxDelay = maxDelay ?? TimeSpan.FromSeconds(30);
        ShouldRetryPredicate = shouldRetryPredicate ?? DefaultRetryPredicate;
        _logger = logger;
    }

    /// <summary>
    /// Default predicate: retry for network and IO errors
    /// </summary>
    private static bool DefaultRetryPredicate(Exception ex)
    {
        return ex is HttpRequestException
            or TimeoutException
            or IOException
            or System.Net.WebException;
    }

    /// <summary>
    /// Executes operation with automatic retry
    /// </summary>
    public async Task<T> ExecuteAsync<T>(
        Func<Task<T>> operation,
        CancellationToken ct = default)
    {
        var delay = InitialDelay;
        Exception? lastException = null;

        for (int attempt = 0; attempt <= MaxRetries; attempt++)
        {
            try
            {
                if (attempt > 0)
                {
                    // Log retry attempt
                    _logger?.LogDebug($"Retry attempt {attempt}/{MaxRetries} after {delay.TotalSeconds}s");
                }

                return await operation();
            }
            catch (Exception ex) when (ShouldRetryPredicate(ex))
            {
                lastException = ex;

                if (attempt == MaxRetries)
                {
                    // Last attempt failed, throw exception
                    throw new Exception(
                        $"Operation failed after {MaxRetries} retries",
                        lastException);
                }

                // Wait with exponential backoff
                await Task.Delay(delay, ct);

                // Calculate next delay (exponential backoff)
                delay = TimeSpan.FromMilliseconds(
                    Math.Min(
                        delay.TotalMilliseconds * 2,
                        MaxDelay.TotalMilliseconds
                    )
                );
            }
        }

        // Should never reach here (throws in loop above)
        throw lastException ?? new InvalidOperationException("Retry logic error");
    }

    /// <summary>
    /// Sync version of ExecuteAsync
    /// </summary>
    public T Execute<T>(Func<T> operation)
    {
        return ExecuteAsync(() => Task.Run(operation)).GetAwaiter().GetResult();
    }
}
