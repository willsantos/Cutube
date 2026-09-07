using Cutube.Domain.Interfaces;
using Cutube.Domain.Models;
using FluentResults;

namespace Cutube.Domain.Services;

/// <summary>
/// Implementação do serviço de metadados usando FluentResults
/// </summary>
public class FluentMetadataService : IMetadataService
{
    private readonly IVideoMetadataProvider _provider;
    private readonly IValidationService _validator;

    /// <summary>
    /// Cria uma nova instância de FluentMetadataService
    /// </summary>
    public FluentMetadataService(
        IVideoMetadataProvider provider,
        IValidationService validator)
    {
        _provider = provider;
        _validator = validator;
    }

    /// <inheritdoc/>
    public async Task<Result<VideoMetadata>> GetMetadataAsync(string url, CancellationToken ct = default)
    {
        // Validar URL primeiro
        var urlValidation = _validator.ValidateUrl(url);
        if (urlValidation.IsFailed)
            return Result.Fail(urlValidation.Errors);

        try
        {
            var metadata = await _provider.GetMetadataAsync(url, ct);

            // Sanitizar título
            var sanitizedTitle = SanitizeTitle(metadata.Title);

            return Result.Ok(metadata with
            {
                Title = sanitizedTitle
            });
        }
        catch (Exception ex)
        {
            return Result.Fail(new ExceptionalError($"Failed to get metadata: {ex.Message}", ex));
        }
    }

    private static string SanitizeTitle(string title)
    {
        var invalidChars = new char[] { '<', '>', ':', '"', '|', '?', '*' };
        var sanitized = title;

        foreach (var c in invalidChars)
        {
            sanitized = sanitized.Replace(c, ' ');
        }

        // Remover espaços duplicados
        var lines = sanitized.Split(' ');
        sanitized = string.Join(" ", lines.Where(l => !string.IsNullOrWhiteSpace(l)));

        return sanitized.Trim();
    }
}
