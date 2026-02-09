using FluentResults;

namespace Cutube.Domain.Models;

/// <summary>
/// Resultado de download do domínio (compatível com FluentResults)
/// </summary>
public record DomainDownloadResult
{
    public required string FilePath { get; init; }
    public long Size { get; init; }
    public TimeSpan Duration { get; init; }
}

/// <summary>
/// Extension methods for converting between DownloadResult types
/// </summary>
public static class DownloadResultExtensions
{
    /// <summary>
    /// Converts Infrastructure DownloadResult to Domain DownloadResult
    /// </summary>
    public static DomainDownloadResult ToDomainResult(this DownloadResult infraResult)
    {
        return new DomainDownloadResult
        {
            FilePath = infraResult.OutputPath,
            Size = infraResult.FileSizeBytes,
            Duration = infraResult.Duration
        };
    }

    /// <summary>
    /// Converts Domain DownloadResult to Infrastructure DownloadResult
    /// </summary>
    public static DownloadResult ToInfraResult(this DomainDownloadResult domainResult)
    {
        return new DownloadResult
        {
            OutputPath = domainResult.FilePath,
            FileSizeBytes = domainResult.Size,
            Duration = domainResult.Duration,
            Success = true,
            ErrorMessage = null
        };
    }
}
