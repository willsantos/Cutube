using Cutube.Api.Models;
using Cutube.Api.Queuing;
using Cutube.Api.Queuing.Messages;
using Microsoft.Extensions.Logging;

namespace Cutube.Api.Services;

/// <summary>
/// Serviço para gerenciar a Dead Letter Queue.
/// </summary>
public class DlqService
{
    private readonly IDownloadStatusRepository _repository;
    private readonly IQueueProducer _producer;
    private readonly ILogger<DlqService> _logger;

    public DlqService(
        IDownloadStatusRepository repository,
        IQueueProducer producer,
        ILogger<DlqService> logger)
    {
        _repository = repository;
        _producer = producer;
        _logger = logger;
    }

    /// <summary>
    /// Lista todas as mensagens na DLQ (status DeadLetter).
    /// </summary>
    public async Task<IReadOnlyList<DownloadStatusRecord>> GetDeadLetterMessagesAsync(
        CancellationToken cancellationToken = default)
    {
        return await _repository.GetByStatesAsync(
            new[] { DownloadStatus.DeadLetter },
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Reprocessa uma mensagem da DLQ.
    /// </summary>
    public async Task<bool> RetryAsync(
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Attempting to retry DLQ message: {CorrelationId}",
            correlationId);

        // Buscar o status atual
        var status = await _repository.GetByCorrelationIdAsync(correlationId, cancellationToken);

        if (status == null)
        {
            _logger.LogWarning(
                "DLQ message not found: {CorrelationId}",
                correlationId);
            return false;
        }

        if (status.Status != DownloadStatus.DeadLetter && status.Status != DownloadStatus.Failed)
        {
            _logger.LogWarning(
                "Message {CorrelationId} is not in DLQ state. Current state: {State}",
                correlationId, status.Status);
            return false;
        }

        // Criar nova mensagem para reprocessamento
        var message = new DownloadMessage
        {
            MessageId = Guid.NewGuid().ToString(),
            CorrelationId = correlationId, // Manter mesmo correlation ID para tracking
            Url = status.Url,
            StartTime = null,
            EndTime = null,
            OutputPath = status.OutputPath ?? "/tmp/downloads",
            AudioOnly = status.AudioOnly,
            OutputFilename = status.OutputFilename,
            Priority = "normal",
            CreatedAt = DateTime.UtcNow,
            RetryCount = status.RetryCount + 1,
            Metadata = new Queuing.Messages.DownloadMetadata
            {
                Title = status.Metadata?.Title,
                Duration = status.Metadata?.Duration,
                Thumbnail = status.Metadata?.Thumbnail,
                Channel = status.Metadata?.Channel
            }
        };

        // Republicar na fila
        await _producer.PublishDownloadAsync(message, cancellationToken);

        // Atualizar status
        await _repository.UpdateAsync(correlationId, s =>
        {
            s.Status = DownloadStatus.Queued;
            s.RetryCount++;
            s.ErrorMessage = null;
            s.UpdatedAt = DateTime.UtcNow;
        }, cancellationToken);

        _logger.LogInformation(
            "DLQ message requeued successfully: {CorrelationId}, RetryCount: {RetryCount}",
            correlationId, status.RetryCount + 1);

        return true;
    }

    /// <summary>
    /// Remove uma mensagem da DLQ (descarta permanentemente).
    /// </summary>
    public async Task<bool> DiscardAsync(
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Discarding DLQ message: {CorrelationId}",
            correlationId);

        var status = await _repository.GetByCorrelationIdAsync(correlationId, cancellationToken);

        if (status == null)
        {
            return false;
        }

        // Atualizar status para Cancelled
        await _repository.UpdateAsync(correlationId, s =>
        {
            s.Status = DownloadStatus.Cancelled;
            s.ErrorMessage = "Discarded by user from DLQ";
            s.UpdatedAt = DateTime.UtcNow;
        }, cancellationToken);

        _logger.LogInformation(
            "DLQ message discarded: {CorrelationId}",
            correlationId);

        return true;
    }

    /// <summary>
    /// Reprocessa todas as mensagens da DLQ.
    /// </summary>
    public async Task<BatchRetryResult> RetryAllAsync(
        CancellationToken cancellationToken = default)
    {
        var messages = await GetDeadLetterMessagesAsync(cancellationToken);
        var results = new BatchRetryResult();

        foreach (var message in messages)
        {
            try
            {
                var success = await RetryAsync(message.CorrelationId, cancellationToken);
                if (success)
                    results.SuccessCount++;
                else
                    results.FailedCount++;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to retry message: {CorrelationId}",
                    message.CorrelationId);
                results.FailedCount++;
            }
        }

        results.TotalCount = messages.Count;
        return results;
    }
}

/// <summary>
/// Resultado de reprocessamento em lote.
/// </summary>
public class BatchRetryResult
{
    public int TotalCount { get; set; }
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
}
