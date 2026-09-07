namespace Cutube.Domain.Models;

/// <summary>
/// Result of a validation operation
/// </summary>
public record ValidationResult
{
    /// <summary>
    /// Whether validation passed
    /// </summary>
    public required bool IsValid { get; init; }

    /// <summary>
    /// Error message if validation failed
    /// </summary>
    public required string? ErrorMessage { get; init; }

    /// <summary>
    /// Creates a successful validation result
    /// </summary>
    public static ValidationResult Success() => new() { IsValid = true, ErrorMessage = null };

    /// <summary>
    /// Creates a failed validation result with an error message
    /// </summary>
    /// <param name="message">Error message describing the validation failure</param>
    public static ValidationResult Failure(string message) => new() { IsValid = false, ErrorMessage = message };
}
