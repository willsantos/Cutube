using Cutube.Domain.Models;
using FluentResults;

namespace Cutube.Domain.Services;

/// <summary>
/// Implementação do serviço de validação usando FluentResults
/// </summary>
public class FluentValidationService : IValidationService
{
    private static readonly string[] ValidSchemes = { "http", "https" };
    private static readonly string[] ValidHosts = { "youtube.com", "youtu.be", "vimeo.com" };

    /// <inheritdoc/>
    public Result<string> ValidateUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return Result.Fail("URL cannot be empty");

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return Result.Fail("Invalid URL format");

        if (!ValidSchemes.Contains(uri.Scheme.ToLowerInvariant()))
            return Result.Fail($"URL scheme must be one of: {string.Join(", ", ValidSchemes)}");

        var host = uri.Host.ToLowerInvariant();
        var isValidHost = ValidHosts.Any(h => host.Contains(h));

        if (!isValidHost)
            return Result.Fail($"URL host not supported. Supported: {string.Join(", ", ValidHosts)}");

        return Result.Ok(url);
    }

    /// <inheritdoc/>
    public Result<DownloadRequest> Validate(DownloadRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Url))
            return Result.Fail("URL is required");

        var urlValidation = ValidateUrl(request.Url);
        if (urlValidation.IsFailed)
            return Result.Fail(urlValidation.Errors);

        // Validar TimeRange se presente
        if (request.TimeRange != null)
        {
            if (request.TimeRange.StartSeconds >= request.TimeRange.EndSeconds)
                return Result.Fail("TimeRange: Start must be less than End");

            if (request.TimeRange.StartSeconds < 0)
                return Result.Fail("TimeRange: Start cannot be negative");
        }

        return Result.Ok(request);
    }
}
