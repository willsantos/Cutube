namespace Cutube.Worker.Configuration;

/// <summary>
/// Configurações da política de retry.
/// </summary>
public class RetryPolicyOptions
{
    public const string SectionName = "RetryPolicy";

    /// <summary>
    /// Número máximo de tentativas (default: 3).
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Delay inicial em segundos (default: 5).
    /// </summary>
    public int InitialDelaySeconds { get; set; } = 5;

    /// <summary>
    /// Multiplicador para backoff exponencial (default: 6).
    /// </summary>
    public double BackoffMultiplier { get; set; } = 6;

    /// <summary>
    /// Delay máximo em segundos (default: 120 = 2min).
    /// </summary>
    public int MaxDelaySeconds { get; set; } = 120;

    /// <summary>
    /// Calcula o delay para uma tentativa específica.
    /// </summary>
    public TimeSpan GetDelayForAttempt(int attemptNumber)
    {
        if (attemptNumber <= 1)
            return TimeSpan.FromSeconds(InitialDelaySeconds);

        var delay = InitialDelaySeconds * Math.Pow(BackoffMultiplier, attemptNumber - 1);
        var clampedDelay = Math.Min(delay, MaxDelaySeconds);

        return TimeSpan.FromSeconds(clampedDelay);
    }
}
