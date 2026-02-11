using Cutube.Api.Queuing.Messages;
using Cutube.Worker.Handlers;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Cutube.Worker.Consumers;

/// <summary>
/// Consumer dedicado para a Dead Letter Queue.
/// Processa mensagens que falharam permanentemente.
/// </summary>
public class DlqConsumer : IConsumer<DownloadMessage>
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

    public async Task Consume(ConsumeContext<DownloadMessage> context)
    {
        _logger.LogInformation(
            "Processing message from DLQ: {CorrelationId}",
            context.Message.CorrelationId);

        // Obter informações de erro do header MassTransit
        var faultMessage = context.Headers.Get<string>("MT-Fault-Message");
        var faultException = new Exception(faultMessage ?? "Unknown fault");

        await _dlqHandler.HandleAsync(context, faultException);

        // Acknowledge a mensagem (remover da DLQ após processar)
        // A mensagem é mantida no repositório da API para reprocessamento manual
    }
}
