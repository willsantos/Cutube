// src/Cutube.Api/Hubs/IDownloadHubClient.cs
using Microsoft.AspNetCore.SignalR;

namespace Cutube.Api.Hubs;

/// <summary>
/// Interface typed para métodos do cliente SignalR
/// Define quais métodos o cliente pode receber do servidor
/// </summary>
public interface IDownloadHubClient
{
    /// <summary>
    /// Cliente recebe evento quando download inicia
    /// </summary>
    Task DownloadStarted(string downloadId, DownloadStartedEvent data);

    /// <summary>
    /// Cliente recebe atualização de progresso
    /// </summary>
    Task DownloadProgress(string downloadId, DownloadProgressEvent data);

    /// <summary>
    /// Cliente recebe evento quando download completa processamento
    /// </summary>
    Task DownloadCompleted(string downloadId, DownloadCompletedEvent data);

    /// <summary>
    /// Cliente recebe evento quando download falha
    /// </summary>
    Task DownloadFailed(string downloadId, DownloadFailedEvent data);

    /// <summary>
    /// Cliente recebe evento quando download é cancelado
    /// </summary>
    Task DownloadCancelled(string downloadId);

    /// <summary>
    /// Cliente recebe evento quando status do download muda (monitor)
    /// </summary>
    Task DownloadStatusChanged(string correlationId, string newStatus);
}
