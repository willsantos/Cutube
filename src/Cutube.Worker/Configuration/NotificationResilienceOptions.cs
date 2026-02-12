namespace Cutube.Worker.Configuration;

public class NotificationResilienceOptions
{
    public const string SectionName = "NotificationResilience";

    public int MaxRetries { get; set; } = 3;

    public int InitialRetryDelaySeconds { get; set; } = 2;

    public int ConsecutiveFailuresBeforeBreak { get; set; } = 5;

    public int CircuitBreakSeconds { get; set; } = 30;
}
