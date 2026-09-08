using Cutube.Domain.Models;

namespace Cutube.Domain.Interfaces;

/// <summary>
/// Processes videos (cutting, audio extraction, format conversion)
/// </summary>
public interface IVideoProcessor
{
    /// <summary>
    /// Verifica se o processador está utilizável (ex.: ffmpeg presente e executável)
    /// </summary>
    bool IsAvailable();

    /// <summary>
    /// Processes a video file according to the request parameters
    /// </summary>
    /// <param name="request">Processing request with input/output paths and time range</param>
    /// <param name="progress">Optional progress reporter for processing status</param>
    /// <param name="ct">Cancellation token for async operation</param>
    /// <returns>Processing result with success status and output file information</returns>
    /// <exception cref="FileNotFoundException">Thrown when input file doesn't exist</exception>
    /// <exception cref="ArgumentException">Thrown when time range is invalid</exception>
    /// <exception cref="OperationCanceledException">Thrown when operation is cancelled</exception>
    Task<ProcessingResult> ProcessAsync(
        ProcessingRequest request,
        IProgress<ProcessingProgress>? progress = null,
        CancellationToken ct = default);
}
