using Cutube.Contracts.Messages;
using Cutube.Worker.Handlers;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Cutube.Worker.Consumers;

/// <summary>
/// Consumer dedicado para a Dead Letter Queue.
/// Processa mensagens que falharam permanentemente.
/// </summary>
public class DlqConsumer : IConsumer<Fault<DownloadMessage>>
{
    private readonly DeadLetterHandler _dlqHandler;
    private readonly ILogger<DlqConsumer> _logger;

    public DlqConsumer(
        DeadLetterHandler dlqHandler,
        ILogger<DlqConsumer> logger)
    {
        _dlqHandler = dlqHandler;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<Fault<DownloadMessage>> context)
    {
        var fault = context.Message;
        var message = fault.Message;

        _logger.LogInformation(
            "Processing message from DLQ: {CorrelationId}",
            message.CorrelationId);

        var faultInfo = fault.Exceptions.FirstOrDefault();
        var faultException = new Exception(faultInfo?.Message ?? "Unknown fault");

        await _dlqHandler.HandleAsync(context, faultException);

        // Acknowledge a mensagem (remover da DLQ após processar)
        // A mensagem é mantida no repositório da API para reprocessamento manual
    }
}
