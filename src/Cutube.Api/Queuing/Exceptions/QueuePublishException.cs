namespace Cutube.Api.Queuing.Exceptions;

/// <summary>
/// Exceção lançada quando falha ao publicar mensagem na fila.
/// </summary>
public class QueuePublishException : Exception
{
    /// <summary>
    /// Correlation ID da mensagem que falhou.
    /// </summary>
    public string CorrelationId { get; }

    public QueuePublishException(string message, string correlationId)
        : base(message)
    {
        CorrelationId = correlationId;
    }

    public QueuePublishException(string message, string correlationId, Exception innerException)
        : base(message, innerException)
    {
        CorrelationId = correlationId;
    }
}
