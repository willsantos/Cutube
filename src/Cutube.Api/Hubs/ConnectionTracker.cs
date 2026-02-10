// src/Cutube.Api/Hubs/ConnectionTracker.cs
using System.Collections.Concurrent;

namespace Cutube.Api.Hubs;

/// <summary>
/// Rastreia conexões SignalR ativas e seus grupos de downloads
/// Thread-safe para uso em ambiente multi-threaded
/// </summary>
public class ConnectionTracker
{
    private readonly ConcurrentDictionary<string, HashSet<string>> _downloadConnections = new();
    private readonly ConcurrentDictionary<string, HashSet<string>> _connectionDownloads = new();
    private readonly ILogger<ConnectionTracker> _logger;

    public ConnectionTracker(ILogger<ConnectionTracker> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Registra que uma conexão entrou no grupo de um download
    /// </summary>
    public void AddConnection(string downloadId, string connectionId)
    {
        // Adicionar connection ao grupo do download
        _downloadConnections.AddOrUpdate(
            downloadId,
            _ => new HashSet<string> { connectionId },
            (_, connections) =>
            {
                lock (connections)
                {
                    connections.Add(connectionId);
                }
                return connections;
            });

        // Adicionar download à lista da conexão (para disconnect)
        _connectionDownloads.AddOrUpdate(
            connectionId,
            _ => new HashSet<string> { downloadId },
            (_, downloads) =>
            {
                lock (downloads)
                {
                    downloads.Add(downloadId);
                }
                return downloads;
            });

        _logger.LogDebug(
            "Connection {ConnectionId} joined download {DownloadId} (total connections: {Count})",
            connectionId, downloadId, _downloadConnections.TryGetValue(downloadId, out var conns) ? conns.Count : 0);
    }

    /// <summary>
    /// Remove uma conexão do grupo de um download
    /// </summary>
    public void RemoveConnection(string downloadId, string connectionId)
    {
        // Remover connection do grupo do download
        if (_downloadConnections.TryGetValue(downloadId, out var connections))
        {
            lock (connections)
            {
                connections.Remove(connectionId);
            }

            if (connections.Count == 0)
            {
                _downloadConnections.TryRemove(downloadId, out _);
                _logger.LogDebug("Download {DownloadId} has no more active connections", downloadId);
            }
        }

        // Remover download da lista da conexão
        if (_connectionDownloads.TryGetValue(connectionId, out var downloads))
        {
            lock (downloads)
            {
                downloads.Remove(downloadId);
            }

            if (downloads.Count == 0)
            {
                _connectionDownloads.TryRemove(connectionId, out _);
            }
        }

        _logger.LogDebug(
            "Connection {ConnectionId} left download {DownloadId}",
            connectionId, downloadId);
    }

    /// <summary>
    /// Remove todas as associações de uma conexão (quando cliente desconecta)
    /// </summary>
    public void RemoveConnectionAll(string connectionId)
    {
        if (!_connectionDownloads.TryRemove(connectionId, out var downloads))
            return;

        lock (downloads)
        {
            foreach (var downloadId in downloads)
            {
                RemoveConnection(downloadId, connectionId);
            }
        }

        _logger.LogInformation("Connection {ConnectionId} fully removed from tracker", connectionId);
    }

    /// <summary>
    /// Verifica se um download tem conexões ativas
    /// </summary>
    public bool HasActiveConnections(string downloadId)
    {
        return _downloadConnections.TryGetValue(downloadId, out var connections) &&
               connections.Count > 0;
    }

    /// <summary>
    /// Retorna número de conexões ativas para um download
    /// </summary>
    public int GetActiveConnectionCount(string downloadId)
    {
        if (_downloadConnections.TryGetValue(downloadId, out var connections))
        {
            lock (connections)
            {
                return connections.Count;
            }
        }
        return 0;
    }

    /// <summary>
    /// Retorna estatísticas gerais
    /// </summary>
    public (int TotalDownloads, int TotalConnections) GetStats()
    {
        return (_downloadConnections.Count, _connectionDownloads.Count);
    }
}
