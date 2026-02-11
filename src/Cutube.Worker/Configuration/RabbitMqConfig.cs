namespace Cutube.Worker.Configuration;

/// <summary>
/// Constants for RabbitMQ configuration
/// </summary>
public static class RabbitMqConfig
{
    public const string MainExchange = "cutube.direct";
    public const string DlqExchange = "cutube.dlq";

    public const string DownloadsQueue = "cutube.downloads";
    public const string DownloadsDlqQueue = "cutube.downloads.dlq";

    public const string DownloadRoutingKey = "download";
    public const string DlqRoutingKey = "download.dlq";

    /// <summary>
    /// Queue TTL para mensagens na DLQ (7 dias)
    /// </summary>
    public const long DlqMessageTtlMs = 7 * 24 * 60 * 60 * 1000;

    /// <summary>
    /// Prefetch count (max unacked messages per consumer)
    /// </summary>
    public const ushort PrefetchCount = 3;
}
