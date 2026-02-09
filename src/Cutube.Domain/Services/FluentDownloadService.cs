using Cutube.Domain.Interfaces;
using Cutube.Domain.Models;
using FluentResults;

namespace Cutube.Domain.Services;

/// <summary>
/// Implementação do serviço principal de download usando FluentResults
/// </summary>
public class FluentDownloadService : IDownloadService
{
    private readonly IVideoDownloader _downloader;
    private readonly IVideoProcessor _processor;
    private readonly IValidationService _validator;

    /// <summary>
    /// Cria uma nova instância de FluentDownloadService
    /// </summary>
    public FluentDownloadService(
        IVideoDownloader downloader,
        IVideoProcessor processor,
        IValidationService validator)
    {
        _downloader = downloader;
        _processor = processor;
        _validator = validator;
    }

    /// <inheritdoc/>
    public async Task<Result<DomainDownloadResult>> DownloadAsync(
        DownloadRequest request,
        IProgress<DownloadProgress>? progress = null,
        CancellationToken ct = default)
    {
        // 1. Validar URL
        var urlValidation = _validator.ValidateUrl(request.Url);
        if (urlValidation.IsFailed)
            return Result.Fail(urlValidation.Errors);

        // 2. Garantir diretório de output existe
        var outputDir = Path.GetDirectoryName(request.OutputPath) ?? Directory.GetCurrentDirectory();
        if (!Directory.Exists(outputDir))
        {
            Directory.CreateDirectory(outputDir);
        }

        // 3. Se não tem timerange nem audio-only, download direto
        if (request.TimeRange == null && !request.AudioOnly)
        {
            return await DownloadDirectAsync(request, progress, ct);
        }

        // 4. Caso contrário, download + processamento
        return await DownloadWithProcessingAsync(request, progress, ct);
    }

    private async Task<Result<DomainDownloadResult>> DownloadDirectAsync(
        DownloadRequest request,
        IProgress<DownloadProgress>? progress,
        CancellationToken ct)
    {
        try
        {
            var result = await _downloader.DownloadAsync(request, progress, ct);

            if (!result.Success)
            {
                return Result.Fail(result.ErrorMessage ?? "Download failed");
            }

            return Result.Ok(result.ToDomainResult());
        }
        catch (Exception ex)
        {
            return Result.Fail(new ExceptionalError($"Download failed: {ex.Message}", ex));
        }
    }

    private async Task<Result<DomainDownloadResult>> DownloadWithProcessingAsync(
        DownloadRequest request,
        IProgress<DownloadProgress>? progress,
        CancellationToken ct)
    {
        var tempFile = Path.GetTempFileName();

        try
        {
            // Download completo para arquivo temporário
            var downloadRequest = request with { OutputPath = tempFile, TimeRange = null, AudioOnly = false };
            var downloadResult = await _downloader.DownloadAsync(downloadRequest, progress, ct);

            if (!downloadResult.Success)
            {
                return Result.Fail(downloadResult.ErrorMessage ?? "Download failed");
            }

            // Processar (corte/conversão)
            var processingRequest = new ProcessingRequest
            {
                InputPath = tempFile,
                OutputPath = request.OutputPath,
                TimeRange = request.TimeRange!, // Non-null because we checked earlier
                AudioOnly = request.AudioOnly
            };

            var processingProgress = ConvertProgress(progress);
            var processResult = await _processor.ProcessAsync(processingRequest, processingProgress, ct);

            if (!processResult.Success)
            {
                return Result.Fail(processResult.ErrorMessage ?? "Processing failed");
            }

            // Deletar arquivo temporário
            if (File.Exists(tempFile))
                File.Delete(tempFile);

            // Calcular duração final
            var duration = request.TimeRange != null
                ? TimeSpan.FromSeconds(request.TimeRange.DurationSeconds)
                : downloadResult.Duration;

            return Result.Ok(new DomainDownloadResult
            {
                FilePath = processResult.OutputPath,
                Size = processResult.FileSizeBytes,
                Duration = duration
            });
        }
        catch (OperationCanceledException)
        {
            // Cleanup em caso de cancelamento
            if (File.Exists(tempFile))
                File.Delete(tempFile);
            if (File.Exists(request.OutputPath))
                File.Delete(request.OutputPath);
            throw;
        }
        catch (Exception ex)
        {
            return Result.Fail(new ExceptionalError($"Download with processing failed: {ex.Message}", ex));
        }
    }

    private IProgress<ProcessingProgress>? ConvertProgress(IProgress<DownloadProgress>? downloadProgress)
    {
        if (downloadProgress == null)
            return null;

        return new Progress<ProcessingProgress>(p =>
        {
            downloadProgress.Report(new DownloadProgress
            {
                State = "processing",
                Percentage = (float)p.Percentage / 100.0f,
                DownloadedBytes = 0,
                TotalBytes = 0,
                Speed = 0,
                ErrorMessage = null
            });
        });
    }
}
