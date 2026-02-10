using System.Threading.Channels;
using Cutube.Domain.Models;
using Microsoft.Extensions.Logging;

namespace Cutube.Api.Services;

/// <summary>
/// In-memory download queue using System.Threading.Channels
/// </summary>
public class DownloadQueue : IDownloadQueue
{
    private readonly Channel<(string Id, DownloadRequest Request)> _channel;
    private readonly ILogger<DownloadQueue> _logger;
    private readonly Dictionary<string, CancellationTokenSource?> _cancellationTokens = new();

    public DownloadQueue(ILogger<DownloadQueue> logger)
    {
        var options = new UnboundedChannelOptions { SingleReader = true };
        _channel = Channel.CreateUnbounded<(string, DownloadRequest)>(options);
        _logger = logger;
    }

    public async Task<string> EnqueueAsync(DownloadRequest request, CancellationToken ct)
    {
        var downloadId = Guid.NewGuid().ToString("N");
        _cancellationTokens[downloadId] = CancellationTokenSource.CreateLinkedTokenSource(ct);
        await _channel.Writer.WriteAsync((downloadId, request), ct);
        _logger.LogInformation("Download {DownloadId} enqueued for URL {Url}", downloadId, request.Url);
        return downloadId;
    }

    public Task CancelAsync(string downloadId, CancellationToken ct)
    {
        if (_cancellationTokens.TryGetValue(downloadId, out var cts))
        {
            cts?.Cancel();
            _logger.LogInformation("Download {DownloadId} cancel requested", downloadId);
            return Task.CompletedTask;
        }

        _logger.LogWarning("Download {DownloadId} not found for cancellation", downloadId);
        return Task.CompletedTask;
    }

    public async IAsyncEnumerable<(string Id, DownloadRequest Request)> DequeueAllAsync(
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach (var item in _channel.Reader.ReadAllAsync(ct))
        {
            yield return item;
        }
    }
}
