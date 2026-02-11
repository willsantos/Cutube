namespace Cutube.Api.Queuing;

/// <summary>
/// Interface para publicação de mensagens na fila RabbitMQ.
/// </summary>
public interface IQueueProducer
{
    /// <summary>
    /// Publica uma mensagem de download na fila.
    /// </summary>
    /// <param name="message">Mensagem de download.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Correlation ID para tracking.</returns>
    Task<string> PublishDownloadAsync(Queuing.Messages.DownloadMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifica se a conexão com RabbitMQ está ativa.
    /// </summary>
    bool IsConnected { get; }
}
