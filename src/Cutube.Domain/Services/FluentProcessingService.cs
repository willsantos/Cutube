using Cutube.Domain.Interfaces;
using Cutube.Domain.Models;
using FluentResults;

namespace Cutube.Domain.Services;

/// <summary>
/// Implementação do serviço de processamento usando FluentResults
/// </summary>
public class FluentProcessingService : IProcessingService
{
    private readonly IVideoProcessor _processor;

    /// <summary>
    /// Cria uma nova instância de FluentProcessingService
    /// </summary>
    public FluentProcessingService(IVideoProcessor processor)
    {
        _processor = processor;
    }

    /// <inheritdoc/>
    public async Task<Result<ProcessingResult>> ProcessAsync(
        ProcessingRequest request,
        IProgress<ProcessingProgress>? progress = null,
        CancellationToken ct = default)
    {
        try
        {
            var result = await _processor.ProcessAsync(request, progress, ct);

            if (!result.Success)
            {
                return Result.Fail(result.ErrorMessage ?? "Processing failed");
            }

            return Result.Ok(result);
        }
        catch (Exception ex)
        {
            return Result.Fail(new ExceptionalError("Processing failed", ex));
        }
    }
}
