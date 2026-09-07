using Cutube.Domain.Models;
using FluentResults;

namespace Cutube.Domain.Services;

/// <summary>
/// Principal serviço de download que orquestra downloader e processor
/// </summary>
public interface IDownloadService
{
    /// <summary>
    /// Executa download completo (download + processamento se necessário)
    /// </summary>
    /// <param name="request">Requisição de download</param>
    /// <param name="progress">Reporter de progresso</param>
    /// <param name="ct">Token de cancelamento</param>
    /// <returns>Result com DomainDownloadResult ou erro</returns>
    Task<Result<DomainDownloadResult>> DownloadAsync(
        DownloadRequest request,
        IProgress<DownloadProgress>? progress = null,
        CancellationToken ct = default);
}

/// <summary>
/// Serviço de metadados de vídeo
/// </summary>
public interface IMetadataService
{
    /// <summary>
    /// Obtém metadados de um vídeo
    /// </summary>
    /// <param name="url">URL do vídeo</param>
    /// <param name="ct">Token de cancelamento</param>
    /// <returns>Result com VideoMetadata ou erro</returns>
    Task<Result<VideoMetadata>> GetMetadataAsync(string url, CancellationToken ct = default);
}

/// <summary>
/// Serviço de validação de requisições
/// </summary>
public interface IValidationService
{
    /// <summary>
    /// Valida uma URL
    /// </summary>
    /// <param name="url">URL para validar</param>
    /// <returns>Result de validação</returns>
    Result<string> ValidateUrl(string url);

    /// <summary>
    /// Valida uma requisição de download
    /// </summary>
    /// <param name="request">Requisição para validar</param>
    /// <returns>Result de validação</returns>
    Result<DownloadRequest> Validate(DownloadRequest request);
}

/// <summary>
/// Serviço de processamento de vídeo
/// </summary>
public interface IProcessingService
{
    /// <summary>
    /// Processa um vídeo (corte, conversão, extração de áudio)
    /// </summary>
    /// <param name="request">Requisição de processamento</param>
    /// <param name="progress">Reporter de progresso</param>
    /// <param name="ct">Token de cancelamento</param>
    /// <returns>Result com ProcessingResult ou erro</returns>
    Task<Result<ProcessingResult>> ProcessAsync(
        ProcessingRequest request,
        IProgress<ProcessingProgress>? progress = null,
        CancellationToken ct = default);
}
