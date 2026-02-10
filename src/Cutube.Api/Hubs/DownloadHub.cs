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

    public DownloadHub(ILogger<DownloadHub> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Cliente conecta-se e entra no grupo de um download específico
    /// </summary>
    /// <param name="downloadId">ID do download a observar</param>
    public async Task JoinDownloadGroup(string downloadId)
    {
        var groupName = GetDownloadGroupName(downloadId);
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

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
