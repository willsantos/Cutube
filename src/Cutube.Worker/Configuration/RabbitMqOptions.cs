namespace Cutube.Worker.Configuration;

/// <summary>
/// Configuration options for RabbitMQ connection and settings
/// </summary>
public class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    /// <summary>
    /// RabbitMQ host (default: localhost)
    /// </summary>
    public string Host { get; set; } = "localhost";

    /// <summary>
    /// AMQP port (default: 5672)
    /// </summary>
    public int Port { get; set; } = 5672;

    /// <summary>
    /// Virtual host (default: /)
    /// </summary>
    public string VirtualHost { get; set; } = "/";

    /// <summary>
    /// Username for authentication
    /// </summary>
    public string UserName { get; set; } = "cutube";

    /// <summary>
    /// Password for authentication
    /// </summary>
    public string Password { get; set; } = "cutube123";

    /// <summary>
    /// Retry count for connection attempts
    /// </summary>
    public int RetryCount { get; set; } = 5;

    /// <summary>
    /// Connection timeout in seconds
    /// </summary>
    public int Timeout { get; set; } = 30;
}
