// src/Cutube.Api/Hubs/DownloadHub.cs
using Microsoft.AspNetCore.SignalR;

namespace Cutube.Api.Hubs;

/// <summary>
/// SignalR Hub para comunicação em tempo real de downloads
/// Clients podem entrar/sair de grupos específicos por downloadId
/// </summary>
public class DownloadHub : Hub<IDownloadHubClient>
{
    private readonly ILogger<DownloadHub> _logger;
    private readonly ConnectionTracker _connectionTracker;

    public DownloadHub(
        ILogger<DownloadHub> logger,
        ConnectionTracker connectionTracker)
    {
        _logger = logger;
        _connectionTracker = connectionTracker;
    }

    /// <summary>
    /// Cliente conecta-se e entra no grupo de um download específico
    /// </summary>
    /// <param name="downloadId">ID do download a observar</param>
    public async Task JoinDownloadGroup(string downloadId)
    {
        var groupName = GetDownloadGroupName(downloadId);
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

        // Rastrear conexão
        _connectionTracker.AddConnection(downloadId, Context.ConnectionId);

        _logger.LogInformation(
            "Connection {ConnectionId} joined group {GroupName} for download {DownloadId}",
            Context.ConnectionId, groupName, downloadId);
    }

    /// <summary>
    /// Cliente sai do grupo de um download (para de receber updates)
    /// </summary>
    /// <param name="downloadId">ID do download a parar de observar</param>
    public async Task LeaveDownloadGroup(string downloadId)
    {
        var groupName = GetDownloadGroupName(downloadId);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);

        // Remover rastreamento
        _connectionTracker.RemoveConnection(downloadId, Context.ConnectionId);

        _logger.LogInformation(
            "Connection {ConnectionId} left group {GroupName} for download {DownloadId}",
            Context.ConnectionId, groupName, downloadId);
    }

    /// <summary>
    /// Override chamado quando cliente conecta
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Override chamado quando cliente desconecta
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        // Remover todas as associações desta conexão
        _connectionTracker.RemoveConnectionAll(Context.ConnectionId);

        if (exception is not null)
        {
            _logger.LogError(exception,
                "Client disconnected with error: {ConnectionId}",
                Context.ConnectionId);
        }
        else
        {
            _logger.LogInformation("Client disconnected: {ConnectionId}", Context.ConnectionId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Gera nome de grupo SignalR para um download
    /// </summary>
    public static string GetDownloadGroupName(string downloadId)
    {
        return $"download:{downloadId}";
    }
}
