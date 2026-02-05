using Cutube.Domain.Interfaces;
using Cutube.Domain.Models;
using cutube;

namespace Cutube.Domain.Services;

/// <summary>
/// Validates download-related inputs
/// </summary>
public class ValidationService : IDownloadValidator
{
    private static readonly string[] ValidYouTubeHosts = new[]
    {
        "youtube.com",
        "www.youtube.com",
        "m.youtube.com",
        "youtu.be"
    };

    /// <summary>
    /// Validates a URL
    /// </summary>
    /// <param name="url">URL to validate</param>
    /// <returns>Validation result with success status and optional error message</returns>
    public ValidationResult ValidateUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return ValidationResult.Failure("URL não pode ser vazia");

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return ValidationResult.Failure("URL inválida");

        if (uri.Scheme != "http" && uri.Scheme != "https")
            return ValidationResult.Failure("URL deve usar http ou https");

        var host = uri.Host.ToLowerInvariant();
        if (!ValidYouTubeHosts.Contains(host))
            return ValidationResult.Failure("URL deve ser do YouTube");

        return ValidationResult.Success();
    }

    /// <summary>
    /// Validates a filename
    /// </summary>
    /// <param name="name">Filename to validate</param>
    /// <returns>Validation result with success status and optional error message</returns>
    public ValidationResult ValidateFileName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return ValidationResult.Success();

        var invalidChars = new[] { '<', '>', ':', '"', '|', '?', '*', '/', '\\' };
        if (name.Any(c => invalidChars.Contains(c)))
            return ValidationResult.Failure("Nome contém caracteres inválidos");

        if (name.Contains('/') || name.Contains('\\'))
            return ValidationResult.Failure("Nome não pode conter caminho de diretório");

        return ValidationResult.Success();
    }

    /// <summary>
    /// Validates time range strings
    /// </summary>
    /// <param name="start">Start time string (e.g., "1:30", "90s")</param>
    /// <param name="end">End time string (e.g., "5:00", "300s")</param>
    /// <returns>Validation result with success status and optional error message</returns>
    public ValidationResult ValidateTimeRange(string start, string end)
    {
        try
        {
            var startSec = TimeHelper.ParseToSeconds(start);
            var endSec = TimeHelper.ParseToSeconds(end);

            if (startSec <= 0)
                return ValidationResult.Failure("Tempo de início deve ser maior que zero");

            if (endSec <= startSec)
                return ValidationResult.Failure("Tempo de fim deve ser maior que o início");

            return ValidationResult.Success();
        }
        catch (FormatException ex)
        {
            return ValidationResult.Failure($"Formato de tempo inválido: {ex.Message}");
        }
    }

    /// <summary>
    /// Validates a directory path
    /// </summary>
    /// <param name="path">Directory path to validate</param>
    /// <returns>Validation result with success status and optional error message</returns>
    public ValidationResult ValidateDirectory(string path)
    {
        if (!Directory.Exists(path))
            return ValidationResult.Failure("Diretório não existe");

        try
        {
            var testFile = Path.Combine(path, Guid.NewGuid().ToString());
            File.WriteAllText(testFile, "test");
            File.Delete(testFile);
            return ValidationResult.Success();
        }
        catch (UnauthorizedAccessException)
        {
            return ValidationResult.Failure("Sem permissão de escrita no diretório");
        }
        catch (Exception)
        {
            return ValidationResult.Failure("Diretório inválido ou sem permissão");
        }
    }
}
