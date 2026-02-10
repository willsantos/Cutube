using System.Diagnostics.CodeAnalysis;

namespace Cutube.Api.DTOs;

/// <summary>
/// Response for download creation
/// </summary>
[ExcludeFromCodeCoverage]
public class CreateDownloadResponse
{
    /// <summary>
    /// Unique identifier for the download
    /// </summary>
    public required string DownloadId { get; init; }

    /// <summary>
    /// Current status of the download
    /// </summary>
    public required string Status { get; init; }

    /// <summary>
    /// Optional message
    /// </summary>
    public string? Message { get; init; }
}
