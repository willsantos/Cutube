// tests/Cutube.Api.Tests/SignalR/TestDownloadHubClient.cs
using Cutube.Api.Hubs;

namespace Cutube.Api.Tests.SignalR;

/// <summary>
/// Mock client para testar SignalR Hub
/// Simula o comportamento de um cliente real conectado
/// </summary>
public class TestDownloadHubClient : IDownloadHubClient
{
    private readonly Dictionary<string, List<object>> _receivedEvents = new();

    public IReadOnlyDictionary<string, List<object>> ReceivedEvents => _receivedEvents;

    // IDownloadHubClient implementation (chamado pelo servidor)
    public Task DownloadStarted(string downloadId, DownloadStartedEvent data)
    {
        AddEvent("DownloadStarted", new { downloadId, data });
        return Task.CompletedTask;
    }

    public Task DownloadProgress(string downloadId, DownloadProgressEvent data)
    {
        AddEvent("DownloadProgress", new { downloadId, data });
        return Task.CompletedTask;
    }

    public Task DownloadCompleted(string downloadId, DownloadCompletedEvent data)
    {
        AddEvent("DownloadCompleted", new { downloadId, data });
        return Task.CompletedTask;
    }

    public Task DownloadFailed(string downloadId, DownloadFailedEvent data)
    {
        AddEvent("DownloadFailed", new { downloadId, data });
        return Task.CompletedTask;
    }

    public Task DownloadCancelled(string downloadId)
    {
        AddEvent("DownloadCancelled", new { downloadId });
        return Task.CompletedTask;
    }

    private void AddEvent(string eventType, object data)
    {
        if (!_receivedEvents.ContainsKey(eventType))
            _receivedEvents[eventType] = new List<object>();

        _receivedEvents[eventType].Add(data);
    }

    /// <summary>
    /// Limpa todos eventos recebidos
    /// </summary>
    public void Clear()
    {
        _receivedEvents.Clear();
    }

    /// <summary>
    /// Retorna número de eventos de um tipo recebidos
    /// </summary>
    public int GetEventCount(string eventType)
    {
        return _receivedEvents.TryGetValue(eventType, out var events) ? events.Count : 0;
    }
}
