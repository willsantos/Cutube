using Cutube.Domain.Interfaces;
using Cutube.Domain.Models;
using System.Text.RegularExpressions;

namespace Cutube.Domain.Services;

/// <summary>
/// Service for retrieving video metadata with validation and sanitization
/// </summary>
public class MetadataService : IVideoMetadataProvider
{
    private readonly IVideoMetadataProvider _provider;
    private readonly IDownloadValidator _validator;

    /// <summary>
    /// Creates a new MetadataService
    /// </summary>
    /// <param name="provider">Underlying metadata provider (e.g., yt-dlp wrapper)</param>
    /// <param name="validator">Validator for URL validation</param>
    public MetadataService(IVideoMetadataProvider provider, IDownloadValidator validator)
    {
        _provider = provider;
        _validator = validator;
    }

    /// <summary>
    /// Retrieves and validates video metadata
    /// </summary>
    /// <param name="url">Video URL to fetch metadata from</param>
    /// <param name="ct">Cancellation token for async operation</param>
    /// <returns>Video metadata with sanitized title</returns>
    /// <exception cref="ArgumentException">Thrown when URL is invalid</exception>
    /// <exception cref="InvalidOperationException">Thrown when metadata cannot be retrieved</exception>
    public async Task<VideoMetadata> GetMetadataAsync(string url, CancellationToken ct = default)
    {
        var validationResult = _validator.ValidateUrl(url);
        if (!validationResult.IsValid)
            throw new ArgumentException(validationResult.ErrorMessage);

        var metadata = await _provider.GetMetadataAsync(url, ct);

        var sanitizedTitle = SanitizeTitle(metadata.Title);

        return metadata with { Title = sanitizedTitle };
    }

    /// <summary>
    /// Sanitizes video title for safe filename usage
    /// </summary>
    /// <param name="title">Original title</param>
    /// <returns>Sanitized title safe for filename usage</returns>
    private static string SanitizeTitle(string title)
    {
        var invalidChars = new char[] { '<', '>', ':', '"', '|', '?', '*' };
        var sanitized = title;

        foreach (var c in invalidChars)
        {
            sanitized = sanitized.Replace(c, ' ');
        }

        sanitized = Regex.Replace(sanitized, @"\s+", " ");

        return sanitized.Trim();
    }
}
