using Cutube.Domain.Interfaces;
using Cutube.Domain.Models;

namespace Cutube.Domain.Services;

/// <summary>
/// Service for processing videos (cutting, audio extraction)
/// </summary>
public class ProcessingService : IVideoProcessor
{
    private readonly IVideoProcessor _processor;

    /// <summary>
    /// Creates a new ProcessingService
    /// </summary>
    /// <param name="processor">Underlying video processor (e.g., FFmpeg wrapper)</param>
    public ProcessingService(IVideoProcessor processor)
    {
        _processor = processor;
    }

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
    public async Task<ProcessingResult> ProcessAsync(
        ProcessingRequest request,
        IProgress<ProcessingProgress>? progress = null,
        CancellationToken ct = default)
    {
        if (!File.Exists(request.InputPath))
            throw new FileNotFoundException("Arquivo de entrada não encontrado", request.InputPath);

        if (request.TimeRange.StartSeconds >= request.TimeRange.EndSeconds)
            throw new ArgumentException("Time range inválido");

        var result = await _processor.ProcessAsync(request, progress, ct);

        if (!result.Success || !File.Exists(result.OutputPath))
        {
            return new ProcessingResult
            {
                Success = false,
                OutputPath = request.OutputPath,
                FileSizeBytes = 0,
                ErrorMessage = result.ErrorMessage ?? "Falha no processamento: arquivo de saída não criado"
            };
        }

        return result;
    }
}
