namespace Cutube.Domain.Interfaces;

/// <summary>
/// Reports progress of operations
/// </summary>
public interface IProgressReporter
{
    /// <summary>
    /// Reports progress as a percentage
    /// </summary>
    /// <param name="percentage">Progress percentage (0-100)</param>
    void Report(int percentage);

    /// <summary>
    /// Reports progress as a message
    /// </summary>
    /// <param name="message">Progress message to report</param>
    void Report(string message);
}
