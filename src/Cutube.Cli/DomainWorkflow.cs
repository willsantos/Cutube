using System.Threading;
using Cutube.Domain.Models;
using Cutube.Domain.Services;
using Cutube.ErrorHandling;
using FluentResults;
using CliResult = Cutube.ErrorHandling.Result;
using CliResultT = Cutube.ErrorHandling.Result<string>;

namespace Cutube.Cli;

/// <summary>
/// Orchestrates the download workflow using Domain services
/// </summary>
public class DomainWorkflow : IDisposable
{
    private readonly IMenuService _menu;
    private readonly IMetadataService _metadataService;
    private readonly IDownloadService _downloadService;
    private readonly IConsoleService _console;
    private readonly IFileService _fileService;
    private readonly CancellationToken _ct;

    /// <summary>
    /// Creates a new DomainWorkflow
    /// </summary>
    public DomainWorkflow(
        IMenuService menu,
        IMetadataService metadataService,
        IDownloadService downloadService,
        IConsoleService console,
        IFileService fileService,
        CancellationToken ct = default)
    {
        _menu = menu;
        _metadataService = metadataService;
        _downloadService = downloadService;
        _console = console;
        _fileService = fileService;
        _ct = ct;
    }

    /// <summary>
    /// Executes the download workflow
    /// </summary>
    public async Task<CliResult> RunAsync()
    {
        try
        {
            // Show menu and collect user input
            var errorHandler = new DefaultErrorHandler();
            var menuResult = _menu.Show(_console, _fileService, errorHandler);
            if (!menuResult.IsSuccess)
                return menuResult;

            var videoUrl = _menu.Url;

            _console.WriteLine("Obtendo informações do vídeo...");

            // Get metadata using Domain service
            var metadataResult = await _metadataService.GetMetadataAsync(videoUrl, _ct);
            if (metadataResult.IsFailed)
            {
                _console.WriteLine($"❌ Erro ao obter metadados: {metadataResult.Errors.First().Message}");
                return CliResult.Failure(ErrorType.Network, metadataResult.Errors.First().Message);
            }

            var metadata = metadataResult.Value;

            // Determine filename
            var fileName = string.IsNullOrWhiteSpace(_menu.CustomFileName)
                ? metadata.Title
                : TitleHelper.FormatTitle(_menu.CustomFileName);

            // Get output directory
            var outputDirResult = GetOutputDirectory();
            if (outputDirResult.IsFailure)
                return outputDirResult;

            var outputDir = outputDirResult.Value;
            var extension = _menu.AudioOnly ? ".mp3" : ".mp4";
            var output = Path.Combine(outputDir, $"{fileName}{extension}");

            // Build download request
            var request = new DownloadRequest
            {
                Url = videoUrl,
                OutputPath = output,
                AudioOnly = _menu.AudioOnly,
                TimeRange = BuildTimeRange()
            };

            // Execute download
            return await DownloadVideoAsync(request);
        }
        catch (OperationCanceledException)
        {
            _console.WriteLine("\n⚠️  Operação cancelada pelo usuário.");
            return CliResult.Success();
        }
        catch (Exception ex)
        {
            _console.WriteLine($"Erro: {ex.Message}");
            return CliResult.Failure(ErrorType.Critical, ex.Message, ex);
        }
    }

    /// <summary>
    /// Gets and validates the output directory
    /// </summary>
    private CliResultT GetOutputDirectory()
    {
        try
        {
            var outputDir = string.IsNullOrWhiteSpace(_menu.OutputDirectory)
                ? Directory.GetCurrentDirectory()
                : NormalizePath(_menu.OutputDirectory);

            if (!_fileService.DirectoryExists(outputDir))
            {
                _console.WriteLine($"⚠️  Diretório '{outputDir}' não existe.");
                _console.Write("Deseja criá-lo? (s/n): ");
                var response = _console.ReadLine()?.ToLower();
                if (response == "s")
                {
                    _fileService.CreateDirectory(outputDir);
                    _console.WriteLine($"✓ Diretório criado: {outputDir}");
                }
                else
                {
                    _console.WriteLine("❌ Operação cancelada.");
                    return CliResultT.Failure(ErrorType.Validation, "Operação cancelada pelo usuário");
                }
            }

            ValidationHelper.ValidateDirectory(outputDir, _fileService);
            return CliResultT.Success(outputDir);
        }
        catch (Exception ex)
        {
            return CliResultT.Failure(ErrorType.FileSystem, ex.Message, ex);
        }
    }

    /// <summary>
    /// Builds a TimeRange from menu input if provided
    /// </summary>
    private TimeRange? BuildTimeRange()
    {
        if (string.IsNullOrWhiteSpace(_menu.Start) || string.IsNullOrWhiteSpace(_menu.End))
            return null;

        return TimeRange.FromStrings(_menu.Start, _menu.End);
    }

    /// <summary>
    /// Downloads video using Domain service
    /// </summary>
    private async Task<CliResult> DownloadVideoAsync(DownloadRequest request)
    {
        try
        {
            var typeLabelInicio = request.AudioOnly ? "áudio" : "vídeo";
            _console.WriteLine($"Iniciando o download e corte do {typeLabelInicio}...");
            _console.WriteLine("Esse processo pode demorar, aguarde...");

            // Create progress reporter
            var progress = new Progress<DownloadProgress>(p =>
            {
                if (p.Percentage > 0)
                {
                    var percentage = p.Percentage * 100;
                    _console.WriteLine($"Progresso: {percentage:F0}%");
                }

                if (!string.IsNullOrEmpty(p.State) && p.State != "downloading")
                {
                    _console.WriteLine($"Estado: {p.State}");
                }
            });

            // Execute download via Domain
            var result = await _downloadService.DownloadAsync(request, progress, _ct);

            if (result.IsFailed)
            {
                var errorMsg = result.Errors.First().Message;
                _console.WriteLine($"❌ Erro no download: {errorMsg}");
                return CliResult.Failure(ErrorType.Network, errorMsg);
            }

            var downloadResult = result.Value;
            var typeLabel = request.AudioOnly ? "Áudio" : "Vídeo";
            _console.WriteLine($"✓ {typeLabel} salvo em: {Path.GetFullPath(downloadResult.FilePath)}");

            return CliResult.Success();
        }
        catch (Exception ex)
        {
            return CliResult.Failure(ErrorType.Network, ex.Message, ex);
        }
    }

    /// <summary>
    /// Normalizes a file path
    /// </summary>
    private string NormalizePath(string path)
    {
        var fullPath = Path.GetFullPath(path);
        fullPath = fullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return fullPath;
    }

    public void Dispose()
    {
        // Nothing to dispose currently
        // Infrastructure services handle their own disposal
    }
}
