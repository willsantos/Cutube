namespace Cutube.Contracts.Configuration;

public class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    public string Host { get; set; } = "localhost";

    public int Port { get; set; } = 5672;

    public string VirtualHost { get; set; } = "/";

    public string UserName { get; set; } = "cutube";

    public string Password { get; set; } = "cutube123";

    public int RetryCount { get; set; } = 5;

    public int Timeout { get; set; } = 30;
}
