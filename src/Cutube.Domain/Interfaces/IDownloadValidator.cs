using Cutube.Domain.Models;

namespace Cutube.Domain.Interfaces;

/// <summary>
/// Validates download-related inputs
/// </summary>
public interface IDownloadValidator
{
    /// <summary>
    /// Validates a URL
    /// </summary>
    /// <param name="url">URL to validate</param>
    /// <returns>Validation result with success status and optional error message</returns>
    ValidationResult ValidateUrl(string url);

    /// <summary>
    /// Validates a filename
    /// </summary>
    /// <param name="name">Filename to validate</param>
    /// <returns>Validation result with success status and optional error message</returns>
    ValidationResult ValidateFileName(string name);

    /// <summary>
    /// Validates time range strings
    /// </summary>
    /// <param name="start">Start time string (e.g., "1:30", "90s")</param>
    /// <param name="end">End time string (e.g., "5:00", "300s")</param>
    /// <returns>Validation result with success status and optional error message</returns>
    ValidationResult ValidateTimeRange(string start, string end);

    /// <summary>
    /// Validates a directory path
    /// </summary>
    /// <param name="path">Directory path to validate</param>
    /// <returns>Validation result with success status and optional error message</returns>
    ValidationResult ValidateDirectory(string path);
}
