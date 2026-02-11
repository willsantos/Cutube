namespace Cutube.Api.Configuration;

/// <summary>
/// Opções de configuração para RabbitMQ.
/// </summary>
public class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    /// <summary>
    /// Host do RabbitMQ.
    /// </summary>
    public string Host { get; set; } = "localhost";

    /// <summary>
    /// Porta do RabbitMQ.
    /// </summary>
    public int Port { get; set; } = 5672;

    /// <summary>
    /// Virtual host.
    /// </summary>
    public string VirtualHost { get; set; } = "/";

    /// <summary>
    /// Nome de usuário.
    /// </summary>
    public string UserName { get; set; } = "cutube";

    /// <summary>
    /// Senha.
    /// </summary>
    public string Password { get; set; } = "cutube123";

    /// <summary>
    /// Número de tentativas de retry em caso de falha.
    /// </summary>
    public int RetryCount { get; set; } = 5;

    /// <summary>
    /// Timeout em segundos.
    /// </summary>
    public int Timeout { get; set; } = 30;
}
