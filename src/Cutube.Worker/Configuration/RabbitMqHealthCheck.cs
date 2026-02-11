using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Cutube.Worker.Configuration;

/// <summary>
/// Custom health check for RabbitMQ connection
/// </summary>
public class RabbitMqHealthCheck : IHealthCheck
{
    private readonly RabbitMqOptions _options;

    public RabbitMqHealthCheck(IOptions<RabbitMqOptions> options)
    {
        _options = options.Value;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Basic TCP connection check to RabbitMQ
            using var client = new System.Net.Sockets.TcpClient();
            await client.ConnectAsync(_options.Host, _options.Port, cancellationToken);

            return HealthCheckResult.Healthy("RabbitMQ is reachable");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("RabbitMQ is not reachable", ex);
        }
    }
}
