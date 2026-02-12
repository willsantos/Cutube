using Cutube.Contracts.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using System.Net.Sockets;

namespace Cutube.Worker.Health;

public class RabbitMqConnectionHealthCheck : IHealthCheck
{
    private readonly RabbitMqOptions _rabbitMqOptions;

    public RabbitMqConnectionHealthCheck(IOptions<RabbitMqOptions> rabbitMqOptions)
    {
        _rabbitMqOptions = rabbitMqOptions.Value;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        using var tcpClient = new TcpClient();
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(Math.Max(1, _rabbitMqOptions.Timeout)));

        try
        {
            await tcpClient.ConnectAsync(_rabbitMqOptions.Host, _rabbitMqOptions.Port, timeoutCts.Token);

            return HealthCheckResult.Healthy(
                $"RabbitMQ reachable at {_rabbitMqOptions.Host}:{_rabbitMqOptions.Port}");
        }
        catch (OperationCanceledException)
        {
            return HealthCheckResult.Unhealthy(
                $"Timed out connecting to RabbitMQ at {_rabbitMqOptions.Host}:{_rabbitMqOptions.Port}");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                $"Failed to connect to RabbitMQ at {_rabbitMqOptions.Host}:{_rabbitMqOptions.Port}",
                ex);
        }
    }
}
