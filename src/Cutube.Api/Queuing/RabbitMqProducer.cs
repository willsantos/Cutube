using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Cutube.Api.Queuing.Messages;
using Cutube.Api.Queuing.Exceptions;
using Cutube.Api.Configuration;

namespace Cutube.Api.Queuing;

/// <summary>
/// Implementação de producer para RabbitMQ usando MassTransit.
/// </summary>
public class RabbitMqProducer : IQueueProducer
{
    private const string DownloadsQueue = "cutube.downloads";

    private readonly IBus _bus;
    private readonly RabbitMqOptions _options;
    private readonly ILogger<RabbitMqProducer> _logger;
    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    private bool _isConnected;

    public bool IsConnected => _isConnected;

    public RabbitMqProducer(
        IBus bus,
        IOptions<RabbitMqOptions> options,
        ILogger<RabbitMqProducer> logger)
    {
        _bus = bus;
        _options = options.Value;
        _logger = logger;
        _isConnected = true; // MassTransit gerencia reconexão automaticamente
    }

    /// <inheritdoc/>
    public async Task<string> PublishDownloadAsync(DownloadMessage message, CancellationToken cancellationToken = default)
    {
        // Validar mensagem
        ValidateMessage(message);

        // Gerar correlation ID se não existe
        if (string.IsNullOrEmpty(message.CorrelationId))
        {
            message = message with { CorrelationId = Guid.NewGuid().ToString() };
        }

        try
        {
            _logger.LogInformation(
                "Publishing download message: {MessageId}, CorrelationId: {CorrelationId}, URL: {Url}",
                message.MessageId,
                message.CorrelationId,
                message.Url);

            // Publicar mensagem usando MassTransit
            var endpoint = await _bus.GetSendEndpoint(new Uri($"queue:{DownloadsQueue}"));

            await endpoint.Send(message, cancellationToken);

            _isConnected = true;
            _logger.LogInformation(
                "Message published successfully: {CorrelationId}",
                message.CorrelationId);

            return message.CorrelationId;
        }
        catch (Exception ex)
        {
            _isConnected = false;
            _logger.LogError(ex,
                "Failed to publish message: {CorrelationId}. Error: {Error}",
                message.CorrelationId,
                ex.Message);

            throw new QueuePublishException(
                $"Failed to publish download message: {message.CorrelationId}",
                message.CorrelationId,
                ex);
        }
    }

    private void ValidateMessage(DownloadMessage message)
    {
        if (string.IsNullOrWhiteSpace(message.Url))
        {
            throw new ArgumentException("URL is required.", nameof(message));
        }

        if (string.IsNullOrWhiteSpace(message.OutputPath))
        {
            throw new ArgumentException("Output path is required.", nameof(message));
        }

        // Validar URL
        if (!Uri.TryCreate(message.Url, UriKind.Absolute, out var uri))
        {
            throw new ArgumentException("Invalid URL format.", nameof(message));
        }

        // Validar startTime/endTime
        if (!string.IsNullOrEmpty(message.StartTime) && !string.IsNullOrEmpty(message.EndTime))
        {
            // Validação básica - formato será validado no worker
            if (message.StartTime == message.EndTime)
            {
                throw new ArgumentException("Start time and end time cannot be equal.", nameof(message));
            }
        }
    }
}
