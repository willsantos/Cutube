using System.Diagnostics.CodeAnalysis;

namespace Cutube.Api.DTOs;

/// <summary>
/// Response for download creation
/// </summary>
[ExcludeFromCodeCoverage]
public class CreateDownloadResponse
{
    /// <summary>
    /// ID do download (igual ao correlation ID).
    /// </summary>
    public required string DownloadId { get; init; }

    /// <summary>
    /// Correlation ID para tracking end-to-end.
    /// </summary>
    public required string CorrelationId { get; init; }

    /// <summary>
    /// Status do download: "queued".
    /// </summary>
    public required string Status { get; init; }

    /// <summary>
    /// Mensagem descritiva.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Timestamp de enfileiramento.
    /// </summary>
    public DateTime EnqueuedAt { get; init; }

    /// <summary>
    /// URL para consultar status do download.
    /// </summary>
    public string StatusUrl => $"/api/downloads/{DownloadId}";
}
