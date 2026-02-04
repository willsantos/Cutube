namespace Cutube.ErrorHandling;

public class RetryPolicy
{
    public int MaxRetries { get; }
    public TimeSpan InitialDelay { get; }
    public TimeSpan MaxDelay { get; }
    public Func<Exception, bool> ShouldRetryPredicate { get; }

    public RetryPolicy(
        int maxRetries = 3,
        TimeSpan? initialDelay = null,
        TimeSpan? maxDelay = null,
        Func<Exception, bool>? shouldRetryPredicate = null)
    {
        MaxRetries = maxRetries;
        InitialDelay = initialDelay ?? TimeSpan.FromSeconds(1);
        MaxDelay = maxDelay ?? TimeSpan.FromSeconds(30);
        ShouldRetryPredicate = shouldRetryPredicate ?? DefaultRetryPredicate;
    }

    private static bool DefaultRetryPredicate(Exception ex)
    {
        return ex is HttpRequestException
            or TimeoutException
            or IOException;
    }

    public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation)
    {
        var delay = InitialDelay;
        Exception? lastException = null;

        for (int attempt = 0; attempt <= MaxRetries; attempt++)
        {
            try
            {
                return await operation();
            }
            catch (Exception ex) when (ShouldRetryPredicate(ex))
            {
                lastException = ex;
                if (attempt < MaxRetries)
                {
                    await Task.Delay(delay);
                    delay = TimeSpan.FromMilliseconds(
                        Math.Min(delay.TotalMilliseconds * 2, MaxDelay.TotalMilliseconds)
                    );
                }
            }
        }

        throw lastException!;
    }
}
