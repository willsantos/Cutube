using MassTransit;

namespace Cutube.Worker.Configuration;

/// <summary>
/// Extension methods to configure RabbitMQ queues with MassTransit
/// </summary>
public static class QueueDeclarationExtensions
{
    /// <summary>
    /// Declara a fila principal com DLQ configuration
    /// </summary>
    public static void ConfigureDownloadsQueue(this IReceiveEndpointConfigurator endpoint)
    {
        endpoint.ConfigureConsumeTopology = false;

        // Configurar como Quorum Queue para melhor confiabilidade
        if (endpoint is IRabbitMqReceiveEndpointConfigurator rabbitEndpoint)
        {
            rabbitEndpoint.SetQuorumQueue();
        }
    }
}
