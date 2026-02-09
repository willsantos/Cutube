# Fase 2.1: Refatoração para Domain Layer

**Status:** 🎯 Planejamento
**Épico:** Cutube-2i6 (Épico 2: Arquitetura Híbrida CLI + API)
**Duração:** 4-5 dias
**Responsável:** Backend Developer
**Prioridade:** 🔥 Alta (bloqueia demais fases)
**Dependência:** ✅ Épico 1 completo

---

## Objetivo

Extrair toda lógica de domínio do projeto CLI para um projeto separado (Class Library), eliminando dependências de Console e tornando o código reutilizável entre CLI e API.

**Benefícios:**
- Código de domínio isolado e testável
- CLI se torna "thin client" do domínio
- API pode reutilizar toda lógica de negócio
- Preparação para sistema distribuído (Épico 3)

---

## Visão Arquitetural

```
┌─────────────────────────────────────────────┐
│         CLI (Presentation Layer)            │
│  - Program.cs                               │
│  - Menu.cs                                  │
│  - ProgramWorkflow.cs                       │
└──────────────────┬──────────────────────────┘
                   │ Domain calls
                   ▼
┌─────────────────────────────────────────────┐
│         Domain Layer (NEW)                  │
│  Cutube.Domain Class Library                │
│                                             │
│  /Interfaces                                │
│    - IVideoMetadataProvider                 │
│    - IVideoDownloader                       │
│    - IVideoProcessor                        │
│    - IFileSystem                            │
│    - IDownloadValidator                     │
│                                             │
│  /Models                                    │
│    - DownloadRequest                        │
│    - DownloadProgress                       │
│    - VideoMetadata                          │
│    - TimeRange                              │
│                                             │
│  /Services                                  │
│    - DownloadService                        │
│    - MetadataService                        │
│    - ValidationService                      │
│    - ProcessingService                      │
└──────────────────┬──────────────────────────┘
                   │ External dependencies
                   ▼
┌─────────────────────────────────────────────┐
│         Infrastructure Layer                │
│  - YtDlpDownloader (implements IVideoDownloader)    │
│  - FfmpegProcessor (implements IVideoProcessor)     │
│  - FileSystem (implements IFileSystem)              │
└─────────────────────────────────────────────┘
```

---

## Tarefas

### 2.1.1 Criar projeto Cutube.Domain

**Estimativa:** 2 horas
**Arquivos:**
- `src/Cutube.Domain/Cutube.Domain.csproj`

**Comandos:**
```bash
# Criar solution folder
mkdir -p src

# Criar projeto Class Library
dotnet new classlib -n Cutube.Domain -o src/Cutube.Domain -f net10.0

# Adicionar ao solution
dotnet sln cutube.sln add src/Cutube.Domain/Cutube.Domain.csproj

# Configurar namespace
# Editar .csproj para garantir <RootNamespace>Cutube.Domain</RootNamespace>
```

**Configuração do .csproj:**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <RootNamespace>Cutube.Domain</RootNamespace>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="FluentResults" Version="4.0.0" />
  </ItemGroup>
</Project>
```

**Checklist:**
- [ ] Criar solution folder `src/`
- [ ] Criar projeto Class Library com .NET 10
- [ ] Adicionar ao `cutube.sln`
- [ ] Configurar namespaces (Cutube.Domain.*)
- [ ] Build sem warnings

**Critérios de aceito:**
- ✅ Projeto cria com `dotnet build`
- ✅ Sem dependências de UI (System.Console, etc.)
- ✅ Namespace configurado corretamente
- ✅ FluentResults adicionado para Result pattern

---

### 2.1.2 Criar abstrações para dependências externas

**Estimativa:** 4 horas
**Arquivos:**
```
Cutube.Domain/Interfaces/
  ├── IVideoMetadataProvider.cs    - Extrair metadados de vídeo
  ├── IVideoDownloader.cs          - Download de vídeo
  ├── IVideoProcessor.cs            - Processamento (ffmpeg)
  ├── IFileSystem.cs                - Operações de arquivo
  ├── IProgressReporter.cs          - Reportar progresso genérico
  └── IDownloadValidator.cs         - Validações de entrada
```

**Interfaces a criar:**

```csharp
// IVideoMetadataProvider.cs
using Cutube.Domain.Models;
using FluentResults;

namespace Cutube.Domain.Interfaces;

/// <summary>
/// Responsável por extrair metadados de vídeos de URLs (YouTube, etc.)
/// </summary>
public interface IVideoMetadataProvider
{
    /// <summary>
    /// Obtém metadados completos de um vídeo
    /// </summary>
    /// <param name="url">URL do vídeo</param>
    /// <param name="ct">Token de cancelamento</param>
    /// <returns>Result com VideoMetadata ou erro</returns>
    Task<Result<VideoMetadata>> GetMetadataAsync(string url, CancellationToken ct = default);
}

// IVideoDownloader.cs
using Cutube.Domain.Models;
using FluentResults;

namespace Cutube.Domain.Interfaces;

/// <summary>
/// Responsável por baixar vídeos de URLs
/// </summary>
public interface IVideoDownloader
{
    /// <summary>
    /// Baixa vídeo com progresso
    /// </summary>
    /// <param name="request">Requisição de download</param>
    /// <param name="progress">Reporter de progresso</param>
    /// <param name="ct">Token de cancelamento</param>
    /// <returns>Result com DownloadResult ou erro</returns>
    Task<Result<DownloadResult>> DownloadAsync(
        DownloadRequest request,
        IProgress<DownloadProgress> progress,
        CancellationToken ct = default);
}

// IVideoProcessor.cs
using Cutube.Domain.Models;
using FluentResults;

namespace Cutube.Domain.Interfaces;

/// <summary>
/// Responsável por processar vídeos (corte, conversão, extração de áudio)
/// </summary>
public interface IVideoProcessor
{
    /// <summary>
    /// Processa vídeo (corte, conversão para MP3, etc)
    /// </summary>
    /// <param name="request">Requisição de processamento</param>
    /// <param name="progress">Reporter de progresso</param>
    /// <param name="ct">Token de cancelamento</param>
    /// <returns>Result com ProcessingResult ou erro</returns>
    Task<Result<ProcessingResult>> ProcessAsync(
        ProcessingRequest request,
        IProgress<ProcessingProgress> progress,
        CancellationToken ct = default);
}

// IFileSystem.cs
using FluentResults;

namespace Cutube.Domain.Interfaces;

/// <summary>
/// Abstração para operações de sistema de arquivos
/// </summary>
public interface IFileSystem
{
    /// <summary>
    /// Cria diretório recursivamente
    /// </summary>
    Task<Result<string>> CreateDirectoryAsync(string path);

    /// <summary>
    /// Verifica se arquivo existe
    /// </summary>
    Task<Result<bool>> FileExistsAsync(string path);

    /// <summary>
    /// Obtém tamanho do arquivo em bytes
    /// </summary>
    Task<Result<long>> GetFileSizeAsync(string path);

    /// <summary>
    /// Obtém espaço disponível em disco
    /// </summary>
    Task<Result<long>> GetAvailableDiskSpaceAsync(string path);

    /// <summary>
    /// Deleta arquivo se existir
    /// </summary>
    Task<Result> DeleteFileAsync(string path);
}

// IDownloadValidator.cs
using Cutube.Domain.Models;
using FluentResults;

namespace Cutube.Domain.Interfaces;

/// <summary>
/// Valida requisições de download
/// </summary>
public interface IDownloadValidator
{
    /// <summary>
    /// Valida requisição de download
    /// </summary>
    /// <param name="request">Requisição a validar</param>
    /// <returns>Success se válido, Failure com erro se inválido</returns>
    Result<DownloadRequest> Validate(DownloadRequest request);
}
```

**Checklist:**
- [ ] Criar todas interfaces em `Cutube.Domain/Interfaces/`
- [ ] Documentar XML comments para cada método/interface
- [ ] Compilar sem erros
- [ ] Organizar using statements

**Critérios de aceito:**
- ✅ Todas interfaces definidas
- ✅ Sem dependência de System.Console
- ✅ Compila sem erros
- ✅ XML documentation completa

---

### 2.1.3 Criar models de domínio

**Estimativa:** 3 horas
**Arquivos:**
```
Cutube.Domain/Models/
  ├── DownloadRequest.cs
  ├── DownloadProgress.cs
  ├── DownloadResult.cs
  ├── VideoMetadata.cs
  ├── ProcessingRequest.cs
  ├── ProcessingProgress.cs
  ├── ProcessingResult.cs
  ├── TimeRange.cs
  └── Enums/
      └── DownloadStatus.cs
```

**Models:**

```csharp
// DownloadRequest.cs
namespace Cutube.Domain.Models;

/// <summary>
/// Requisição de download de vídeo
/// </summary>
public record DownloadRequest
{
    public required string Url { get; init; }
    public string? OutputPath { get; init; }
    public TimeRange? TimeRange { get; init; }
    public bool AudioOnly { get; init; }
    public string? CustomFilename { get; init; }

    public string GetOutputPath()
    {
        return OutputPath ?? ".";
    }
}

// DownloadProgress.cs
namespace Cutube.Domain.Models;

/// <summary>
/// Progresso de download em tempo real
/// </summary>
public record DownloadProgress
{
    public required string DownloadId { get; init; }
    public double Percentage { get; init; }
    public long DownloadedBytes { get; init; }
    public long TotalBytes { get; init; }
    public double Speed { get; init; }      // bytes/s
    public TimeSpan? Eta { get; init; }
    public DownloadStatus Status { get; init; }
    public string? ErrorMessage { get; init; }
}

// DownloadResult.cs
namespace Cutube.Domain.Models;

/// <summary>
/// Resultado de download concluído
/// </summary>
public record DownloadResult
{
    public required string FilePath { get; init; }
    public long Size { get; init; }
    public TimeSpan Duration { get; init; }
}

// VideoMetadata.cs
namespace Cutube.Domain.Models;

/// <summary>
/// Metadados completos de vídeo
/// </summary>
public record VideoMetadata
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public required string Uploader { get; init; }
    public required TimeSpan Duration { get; init; }
    public required string ThumbnailUrl { get; init; }
    public long? ViewCount { get; init; }
    public DateTime? UploadDate { get; init; }
    public VideoFormat[] Formats { get; init; } = [];
}

/// <summary>
/// Formato de vídeo disponível
/// </summary>
public record VideoFormat
{
    public required string FormatId { get; init; }
    public required string Extension { get; init; }
    public string? Resolution { get; init; }
    public long? FileSize { get; init; }
}

// TimeRange.cs
namespace Cutube.Domain.Models;

/// <summary>
/// Intervalo de tempo para corte de vídeo
/// </summary>
public record TimeRange
{
    public required TimeSpan Start { get; init; }
    public required TimeSpan End { get; init; }

    public TimeSpan Duration => End - Start;
}

// ProcessingRequest.cs
namespace Cutube.Domain.Models;

/// <summary>
/// Requisição de processamento de vídeo
/// </summary>
public record ProcessingRequest
{
    public required string InputPath { get; init; }
    public required string OutputPath { get; init; }
    public TimeRange? TimeRange { get; init; }
    public bool AudioOnly { get; init; }
}

// ProcessingProgress.cs
namespace Cutube.Domain.Models;

/// <summary>
/// Progresso de processamento (ffmpeg)
/// </summary>
public record ProcessingProgress
{
    public required double Percentage { get; init; }
    public required TimeSpan CurrentTime { get; init; }
    public required TimeSpan TotalTime { get; init; }
    public double Speed { get; init; } // fps ou multiplicador
}

// ProcessingResult.cs
namespace Cutube.Domain.Models;

/// <summary>
/// Resultado de processamento concluído
/// </summary>
public record ProcessingResult
{
    public required string OutputPath { get; init; }
    public long Size { get; init; }
    public TimeSpan Duration { get; init; }
}

// Enums/DownloadStatus.cs
namespace Cutube.Domain.Models;

/// <summary>
/// Status de download
/// </summary>
public enum DownloadStatus
{
    Queued,
    Downloading,
    Processing,
    Completed,
    Failed,
    Cancelled
}
```

**Checklist:**
- [ ] Criar todos models em `Cutube.Domain/Models/`
- [ ] Usar records para imutabilidade
- [ ] Validar required properties (init-only setters)
- [ ] Criar pasta Enums para tipos enumerados
- [ ] XML documentation em todos models

**Critérios de aceito:**
- ✅ Models compilam sem erros
- ✅ Records para imutabilidade
- ✅ Required properties funcionando
- ✅ Namespace Cutube.Domain.Models

---

### 2.1.4 Criar serviços de domínio

**Estimativa:** 8 horas
**Arquivos:**
```
Cutube.Domain/Services/
  ├── IDownloadService.cs              - Interface principal
  ├── DownloadService.cs               - Orquestra download + processamento
  ├── IMetadataService.cs              - Interface
  ├── MetadataService.cs               - Extrai metadados
  ├── IValidationService.cs            - Interface
  ├── ValidationService.cs             - Valida requests
  └── IProcessingService.cs            - Interface
      └── ProcessingService.cs         - Wrapper para IVideoProcessor
```

**Interfaces principais:**

```csharp
// IDownloadService.cs
using Cutube.Domain.Models;
using FluentResults;

namespace Cutube.Domain.Services;

/// <summary>
/// Serviço principal de download (orquestra downloader + processor)
/// </summary>
public interface IDownloadService
{
    /// <summary>
    /// Executa download completo (download + processamento se necessário)
    /// </summary>
    Task<Result<DownloadResult>> DownloadAsync(
        DownloadRequest request,
        IProgress<DownloadProgress> progress,
        CancellationToken ct = default);
}

// IMetadataService.cs
using Cutube.Domain.Models;
using FluentResults;

namespace Cutube.Domain.Services;

/// <summary>
/// Serviço de metadados de vídeo
/// </summary>
public interface IMetadataService
{
    Task<Result<VideoMetadata>> GetMetadataAsync(string url, CancellationToken ct = default);
}

// IValidationService.cs
using Cutube.Domain.Models;
using FluentResults;

namespace Cutube.Domain.Services;

/// <summary>
/// Serviço de validação de requests
/// </summary>
public interface IValidationService
{
    Result<DownloadRequest> Validate(DownloadRequest request);
    Result<string> ValidateUrl(string url);
}
```

**Implementações:**

```csharp
// DownloadService.cs
using Cutube.Domain.Interfaces;
using Cutube.Domain.Models;
using FluentResults;

namespace Cutube.Domain.Services;

/// <summary>
/// Serviço principal que orquestra download e processamento
/// </summary>
public class DownloadService : IDownloadService
{
    private readonly IVideoDownloader _downloader;
    private readonly IVideoProcessor _processor;
    private readonly IValidationService _validator;
    private readonly IFileSystem _fileSystem;

    public DownloadService(
        IVideoDownloader downloader,
        IVideoProcessor processor,
        IValidationService validator,
        IFileSystem fileSystem)
    {
        _downloader = downloader;
        _processor = processor;
        _validator = validator;
        _fileSystem = fileSystem;
    }

    public async Task<Result<DownloadResult>> DownloadAsync(
        DownloadRequest request,
        IProgress<DownloadProgress> progress,
        CancellationToken ct = default)
    {
        // 1. Validar request
        var validation = _validator.Validate(request);
        if (validation.IsFailure)
            return Result.Fail(validation.Errors);

        // 2. Verificar espaço em disco (mínimo 100MB)
        var spaceCheck = await _fileSystem.GetAvailableDiskSpaceAsync(request.OutputPath ?? ".");
        if (spaceCheck.IsFailure || spaceCheck.Value < 100_000_000)
            return Result.Fail("Insufficient disk space (min 100MB required)");

        // 3. Garantir diretório de output existe
        if (!string.IsNullOrEmpty(request.OutputPath))
        {
            var dirResult = await _fileSystem.CreateDirectoryAsync(request.OutputPath);
            if (dirResult.IsFailure)
                return Result.Fail(dirResult.Errors);
        }

        // 4. Download
        var downloadResult = await _downloader.DownloadAsync(request, progress, ct);
        if (downloadResult.IsFailure)
            return Result.Fail(downloadResult.Errors);

        // 5. Processamento (ffmpeg) se necessário (timerange ou audio-only)
        if (request.TimeRange is not null || request.AudioOnly)
        {
            var processingRequest = new ProcessingRequest
            {
                InputPath = downloadResult.Value.FilePath,
                OutputPath = GetOutputPath(request),
                TimeRange = request.TimeRange,
                AudioOnly = request.AudioOnly
            };

            var processResult = await _processor.ProcessAsync(
                processingRequest,
                ConvertProgress(progress),
                ct);

            if (processResult.IsFailure)
                return Result.Fail(processResult.Errors);

            // Deletar arquivo original após processamento
            await _fileSystem.DeleteFileAsync(downloadResult.Value.FilePath);

            return Result.Ok(new DownloadResult
            {
                FilePath = processResult.Value.OutputPath,
                Size = processResult.Value.Size,
                Duration = processResult.Value.Duration
            });
        }

        return downloadResult;
    }

    private string GetOutputPath(DownloadRequest request)
    {
        var outputDir = request.OutputPath ?? ".";
        var filename = request.CustomFilename ?? GenerateFilename(request);
        return Path.Combine(outputDir, filename);
    }

    private string GenerateFilename(DownloadRequest request)
    {
        // Lógica de geração de nome de arquivo
        var extension = request.AudioOnly ? ".mp3" : ".mp4";
        return $"video_{Guid.NewGuid():N}{extension}";
    }

    private IProgress<ProcessingProgress> ConvertProgress(IProgress<DownloadProgress> downloadProgress)
    {
        return new Progress<ProcessingProgress>(p =>
        {
            downloadProgress.Report(new DownloadProgress
            {
                DownloadId = "", // será preenchido pelo caller
                Percentage = p.Percentage,
                Status = DownloadStatus.Processing,
                DownloadedBytes = 0,
                TotalBytes = 0,
                Speed = p.Speed,
                Eta = null
            });
        });
    }
}

// ValidationService.cs
using Cutube.Domain.Models;
using FluentResults;

namespace Cutube.Domain.Services;

/// <summary>
/// Serviço de validação de requests
/// </summary>
public class ValidationService : IValidationService
{
    private static readonly string[] ValidSchemes = { "http", "https" };
    private static readonly string[] ValidHosts = { "youtube.com", "youtu.be", "vimeo.com" };

    public Result<DownloadRequest> Validate(DownloadRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Url))
            return Result.Fail("URL is required");

        var urlValidation = ValidateUrl(request.Url);
        if (urlValidation.IsFailure)
            return Result.Fail(urlValidation.Errors);

        // Validar TimeRange se presente
        if (request.TimeRange is not null)
        {
            if (request.TimeRange.Start >= request.TimeRange.End)
                return Result.Fail("TimeRange: Start must be less than End");

            if (request.TimeRange.Start < TimeSpan.Zero)
                return Result.Fail("TimeRange: Start cannot be negative");
        }

        return Result.Ok(request);
    }

    public Result<string> ValidateUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return Result.Fail("Invalid URL format");

        if (!ValidSchemes.Contains(uri.Scheme.ToLowerInvariant()))
            return Result.Fail($"URL scheme must be one of: {string.Join(", ", ValidSchemes)}");

        var host = uri.Host.ToLowerInvariant();
        var isValidHost = ValidHosts.Any(h => host.Contains(h));

        if (!isValidHost)
            return Result.Fail($"URL host not supported. Supported: {string.Join(", ", ValidHosts)}");

        return Result.Ok(url);
    }
}

// MetadataService.cs
using Cutube.Domain.Interfaces;
using Cutube.Domain.Models;
using FluentResults;

namespace Cutube.Domain.Services;

/// <summary>
/// Serviço de metadados (wrapper simples)
/// </summary>
public class MetadataService : IMetadataService
{
    private readonly IVideoMetadataProvider _provider;

    public MetadataService(IVideoMetadataProvider provider)
    {
        _provider = provider;
    }

    public async Task<Result<VideoMetadata>> GetMetadataAsync(string url, CancellationToken ct = default)
    {
        return await _provider.GetMetadataAsync(url, ct);
    }
}

// ProcessingService.cs
using Cutube.Domain.Interfaces;
using Cutube.Domain.Models;
using FluentResults;

namespace Cutube.Domain.Services;

/// <summary>
/// Serviço de processamento (wrapper simples)
/// </summary>
public class ProcessingService : IProcessingService
{
    private readonly IVideoProcessor _processor;

    public ProcessingService(IVideoProcessor processor)
    {
        _processor = processor;
    }

    public async Task<Result<ProcessingResult>> ProcessAsync(
        ProcessingRequest request,
        IProgress<ProcessingProgress> progress,
        CancellationToken ct = default)
    {
        return await _processor.ProcessAsync(request, progress, ct);
    }
}
```

**Checklist:**
- [ ] Criar interfaces de serviços (IDownloadService, IMetadataService, IValidationService)
- [ ] Implementar DownloadService (orquestra downloader + processor)
- [ ] Implementar ValidationService (valida URL, TimeRange)
- [ ] Implementar MetadataService (wrapper para IVideoMetadataProvider)
- [ ] Implementar ProcessingService (wrapper para IVideoProcessor)
- [ ] Injeção de dependências via construtor
- [ ] Result pattern com FluentResults
- [ ] XML documentation completa

**Critérios de aceito:**
- ✅ Lógica de domínio orquestrada corretamente
- ✅ Services têm 0 dependência de Console/System
- ✅ Validações implementadas
- ✅ Build sem warnings

---

### 2.1.5 Criar projeto de testes Cutube.Domain.Tests

**Estimativa:** 4 horas
**Arquivos:**
```
Cutube.Domain.Tests/
  ├── Services/
  │   ├── DownloadServiceTests.cs
  │   ├── MetadataServiceTests.cs
  │   └── ValidationServiceTests.cs
  └── Models/
      └── DownloadRequestTests.cs
```

**Comandos:**
```bash
# Criar projeto de testes
dotnet new xunit -n Cutube.Domain.Tests -o tests/Cutube.Domain.Tests

# Adicionar ao solution
dotnet sln cutube.sln add tests/Cutube.Domain.Tests/Cutube.Domain.Tests.csproj

# Adicionar referência ao projeto de domínio
dotnet add tests/Cutube.Domain.Tests/Cutube.Domain.Tests.csproj reference src/Cutube.Domain/Cutube.Domain.csproj

# Adicionar Moq
dotnet add tests/Cutube.Domain.Tests/Cutube.Domain.Tests.csproj package Moq
dotnet add tests/Cutube.Domain.Tests/Cutube.Domain.Tests.csproj package FluentAssertions
```

**Exemplo de testes:**

```csharp
// ValidationServiceTests.cs
using Cutube.Domain.Models;
using Cutube.Domain.Services;
using FluentAssertions;
using Xunit;

namespace Cutube.Domain.Tests.Services;

public class ValidationServiceTests
{
    private readonly ValidationService _validator = new();

    [Fact]
    public void Validate_WithValidUrl_ReturnsSuccess()
    {
        // Arrange
        var request = new DownloadRequest
        {
            Url = "https://youtube.com/watch?v=test"
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithEmptyUrl_ReturnsFailure()
    {
        // Arrange
        var request = new DownloadRequest
        {
            Url = ""
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.First().Message.Should().Be("URL is required");
    }

    [Theory]
    [InlineData("invalid-url")]
    [InlineData("ftp://example.com")]
    [InlineData("https://invalidhost.com/video")]
    public void Validate_WithInvalidUrl_ReturnsFailure(string url)
    {
        // Arrange
        var request = new DownloadRequest { Url = url };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsFailed.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithInvalidTimeRange_ReturnsFailure()
    {
        // Arrange
        var request = new DownloadRequest
        {
            Url = "https://youtube.com/watch?v=test",
            TimeRange = new TimeRange
            {
                Start = TimeSpan.FromMinutes(5),
                End = TimeSpan.FromMinutes(3) // End < Start
            }
        };

        // Act
        var result = _validator.Validate(request);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.First().Message.Should().Contain("Start must be less than End");
    }
}

// DownloadServiceTests.cs
using Cutube.Domain.Interfaces;
using Cutube.Domain.Models;
using Cutube.Domain.Services;
using FluentResults;
using Moq;
using Xunit;

namespace Cutube.Domain.Tests.Services;

public class DownloadServiceTests
{
    private readonly Mock<IVideoDownloader> _downloaderMock = new();
    private readonly Mock<IVideoProcessor> _processorMock = new();
    private readonly Mock<IValidationService> _validatorMock = new();
    private readonly Mock<IFileSystem> _fileSystemMock = new();

    [Fact]
    public async Task DownloadAsync_WithInvalidRequest_ReturnsFailure()
    {
        // Arrange
        _validatorMock
            .Setup(v => v.Validate(It.IsAny<DownloadRequest>()))
            .Returns(Result.Fail("Invalid URL"));

        var service = new DownloadService(
            _downloaderMock.Object,
            _processorMock.Object,
            _validatorMock.Object,
            _fileSystemMock.Object);

        var request = new DownloadRequest { Url = "invalid" };

        // Act
        var result = await service.DownloadAsync(request, progress: null!);

        // Assert
        result.IsFailed.Should().BeTrue();
        _downloaderMock.Verify(d => d.DownloadAsync(
            It.IsAny<DownloadRequest>(),
            It.IsAny<IProgress<DownloadProgress>>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DownloadAsync_WithInsufficientDiskSpace_ReturnsFailure()
    {
        // Arrange
        _validatorMock
            .Setup(v => v.Validate(It.IsAny<DownloadRequest>()))
            .Returns(Result.Ok(It.IsAny<DownloadRequest>()));

        _fileSystemMock
            .Setup(f => f.GetAvailableDiskSpaceAsync(It.IsAny<string>()))
            .ReturnsAsync(Result.Ok(50_000_000L)); // 50MB (menos que 100MB)

        var service = new DownloadService(
            _downloaderMock.Object,
            _processorMock.Object,
            _validatorMock.Object,
            _fileSystemMock.Object);

        var request = new DownloadRequest { Url = "https://youtube.com/watch?v=test" };

        // Act
        var result = await service.DownloadAsync(request, progress: null!);

        // Assert
        result.IsFailed.Should().BeTrue();
        result.Errors.First().Message.Should().Contain("Insufficient disk space");
    }

    [Fact]
    public async Task DownloadAsync_WithValidRequest_Succeeds()
    {
        // Arrange
        _validatorMock
            .Setup(v => v.Validate(It.IsAny<DownloadRequest>()))
            .Returns(Result.Ok(It.IsAny<DownloadRequest>()));

        _fileSystemMock
            .Setup(f => f.GetAvailableDiskSpaceAsync(It.IsAny<string>()))
            .ReturnsAsync(Result.Ok(1_000_000_000L)); // 1GB

        _downloaderMock
            .Setup(d => d.DownloadAsync(
                It.IsAny<DownloadRequest>(),
                It.IsAny<IProgress<DownloadProgress>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok(new DownloadResult
            {
                FilePath = "/path/to/video.mp4",
                Size = 100_000_000,
                Duration = TimeSpan.FromMinutes(10)
            }));

        var service = new DownloadService(
            _downloaderMock.Object,
            _processorMock.Object,
            _validatorMock.Object,
            _fileSystemMock.Object);

        var request = new DownloadRequest { Url = "https://youtube.com/watch?v=test" };

        // Act
        var result = await service.DownloadAsync(request, progress: null!);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.FilePath.Should().Be("/path/to/video.mp4");
    }
}
```

**Checklist:**
- [ ] Criar projeto xUnit
- [ ] Adicionar Moq e FluentAssertions
- [ ] Escrever testes para ValidationService (URL válida/inválida, TimeRange)
- [ ] Escrever testes para DownloadService (mock de dependências)
- [ ] Escrever testes para MetadataService
- [ ] Cobertura > 80%

**Critérios de aceito:**
- ✅ Todos testes passam: `dotnet test`
- ✅ Cobertura > 80%
- ✅ Testes executam em < 5 segundos
- ✅Mocks usados corretamente (sem dependências externas)

---

### 2.1.6 Refatorar CLI para usar Domain

**Estimativa:** 6 horas
**Arquivos:**
- `cutube/ProgramWorkflow.cs` - refatorar para chamar Domain
- `cutube/Menu.cs` - remover lógica de negócio
- `cutube/Program.cs` - injeção de dependências
- `cutube/cutube.csproj` - adicionar referência a Cutube.Domain
- `cutube/Infrastructure/` - implementações concretas (YtDlpDownloader, etc)

**Configuração do .csproj:**
```xml
<ItemGroup>
  <ProjectReference Include="../src/Cutube.Domain/Cutube.Domain.csproj" />
</ItemGroup>
```

**Refatoração - ProgramWorkflow.cs:**

```csharp
// ANTES (lógica de negócio no CLI)
public class ProgramWorkflow
{
    public async Task ExecuteAsync(string url, TimeRange? range, ...)
    {
        // Muita lógica aqui...
        var ytdlp = new YtDlpHelper(...);
        await ytdlp.DownloadAsync(...);

        if (range is not null)
        {
            var ffmpeg = new FfmpegHelper(...);
            await ffmpeg.ProcessAsync(...);
        }
    }
}

// DEPOIS (thin client do domínio)
using Cutube.Domain.Models;
using Cutube.Domain.Services;
using FluentResults;

namespace Cutube;

public class ProgramWorkflow
{
    private readonly IDownloadService _downloadService;
    private readonly IProgress<DownloadProgress> _progress;

    public ProgramWorkflow(IDownloadService downloadService, IProgress<DownloadProgress> progress)
    {
        _downloadService = downloadService;
        _progress = progress;
    }

    public async Task<Result<DownloadResult>> ExecuteAsync(
        string url,
        TimeRange? range,
        bool audioOnly,
        string? outputPath,
        string? customFilename,
        CancellationToken ct)
    {
        var request = new DownloadRequest
        {
            Url = url,
            TimeRange = range,
            AudioOnly = audioOnly,
            OutputPath = outputPath,
            CustomFilename = customFilename
        };

        return await _downloadService.DownloadAsync(request, _progress, ct);
    }
}
```

**Implementações concretas (Infrastructure):**

```csharp
// cutube/Infrastructure/YtDlpDownloader.cs
using Cutube.Domain.Interfaces;
using Cutube.Domain.Models;
using FluentResults;

namespace Cutube.Infrastructure;

public class YtDlpDownloader : IVideoDownloader
{
    private readonly IFileSystem _fileSystem;

    public YtDlpDownloader(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    public async Task<Result<DownloadResult>> DownloadAsync(
        DownloadRequest request,
        IProgress<DownloadProgress> progress,
        CancellationToken ct = default)
    {
        // Implementação existente do YtDlpHelper
        // Mover lógica de YtDlpHelper.cs para aqui
        // Adaptar para usar IVideoMetadataProvider se necessário

        var downloadId = Guid.NewGuid().ToString("N");
        var outputPath = request.GetOutputPath();

        // Chamar yt-dlp via CLI
        // Reportar progresso via IProgress<DownloadProgress>

        return Result.Ok(new DownloadResult
        {
            FilePath = downloadedFilePath,
            Size = fileSize,
            Duration = duration
        });
    }
}

// cutube/Infrastructure/FfmpegProcessor.cs
using Cutube.Domain.Interfaces;
using Cutube.Domain.Models;
using FluentResults;

namespace Cutube.Infrastructure;

public class FfmpegProcessor : IVideoProcessor
{
    public async Task<Result<ProcessingResult>> ProcessAsync(
        ProcessingRequest request,
        IProgress<ProcessingProgress> progress,
        CancellationToken ct = default)
    {
        // Implementação existente do FfmpegHelper
        // Mover lógica de FfmpegHelper.cs para aqui

        return Result.Ok(new ProcessingResult
        {
            OutputPath = processedFilePath,
            Size = fileSize,
            Duration = duration
        });
    }
}

// cutube/Infrastructure/FileSystem.cs
using Cutube.Domain.Interfaces;
using FluentResults;

namespace Cutube.Infrastructure;

public class FileSystem : IFileSystem
{
    public Task<Result<string>> CreateDirectoryAsync(string path)
    {
        try
        {
            Directory.CreateDirectory(path);
            return Task.FromResult(Result.Ok(path));
        }
        catch (Exception ex)
        {
            return Task.FromResult(Result.Fail<string>(ex.Message));
        }
    }

    public Task<Result<bool>> FileExistsAsync(string path)
    {
        return Task.FromResult(Result.Ok(File.Exists(path)));
    }

    public Task<Result<long>> GetFileSizeAsync(string path)
    {
        try
        {
            var info = new FileInfo(path);
            return Task.FromResult(Result.Ok(info.Length));
        }
        catch (Exception ex)
        {
            return Task.FromResult(Result.Fail<long>(ex.Message));
        }
    }

    public Task<Result<long>> GetAvailableDiskSpaceAsync(string path)
    {
        try
        {
            var driveInfo = new DriveInfo(path);
            return Task.FromResult(Result.Ok(driveInfo.AvailableFreeSpace));
        }
        catch (Exception ex)
        {
            return Task.FromResult(Result.Fail<long>(ex.Message));
        }
    }

    public Task<Result> DeleteFileAsync(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
            return Task.FromResult(Result.Ok());
        }
        catch (Exception ex)
        {
            return Task.FromResult(Result.Fail(ex.Message));
        }
    }
}
```

**Injeção de dependências no Program.cs:**

```csharp
using Cutube;
using Cutube.Domain.Services;
using Cutube.Infrastructure;
using Cutube.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;

var serviceProvider = new ServiceCollection()
    // Infrastructure
    .AddSingleton<IFileSystem, FileSystem>()
    .AddSingleton<IVideoDownloader, YtDlpDownloader>()
    .AddSingleton<IVideoProcessor, FfmpegProcessor>()

    // Domain Services
    .AddSingleton<IValidationService, ValidationService>()
    .AddSingleton<IDownloadService, DownloadService>()
    .AddSingleton<IMetadataService, MetadataService>()

    // CLI
    .AddSingleton<ProgramWorkflow>()

    .BuildServiceProvider();

var workflow = serviceProvider.GetRequiredService<ProgramWorkflow>();

// Usar workflow...
```

**Checklist:**
- [ ] Adicionar referência a Cutube.Domain
- [ ] Criar pasta Infrastructure no CLI
- [ ] Mover YtDlpHelper → YtDlpDownloader (implementa IVideoDownloader)
- [ ] Mover FfmpegHelper → FfmpegProcessor (implementa IVideoProcessor)
- [ ] Criar FileSystem (implementa IFileSystem)
- [ ] Refatorar ProgramWorkflow para usar IDownloadService
- [ ] Converter Menu para apenas coletar inputs
- [ ] Injetar dependências no Program.cs (DI manual ou MS.DI)
- [ ] Remover lógica de negócio do CLI
- [ ] Testar E2E: CLI funciona igual antes

**Critérios de aceito:**
- ✅ CLI funciona exatamente como antes
- ✅ 100% dos testes passam
- ✅ Build sem warnings
- ✅ Zero lógica de negócio no CLI (apresentação apenas)
- ✅ Infrastructure layer implementa interfaces do Domain

---

### 2.1.7 Testes E2E para CLI

**Estimativa:** 3 horas
**Arquivos:**
```
Cutube.Tests/Integration/
  └── CliEndToEndTests.cs
```

**Cenários de teste:**

```csharp
// CliEndToEndTests.cs
using Xunit;
using Xunit.Abstractions;
using System.Diagnostics;

namespace Cutube.Tests.Integration;

public class CliEndToEndTests
{
    private readonly ITestOutputHelper _output;

    public CliEndToEndTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact(Skip = "Requires yt-dlp installed")]
    public async Task Download_CompleteUrl_Succeeds()
    {
        // Arrange
        var testUrl = "https://www.youtube.com/watch?v=jNQXAC9IVRw"; // "Me at the zoo"
        var outputPath = Path.Combine(Path.GetTempPath(), $"cutube_test_{Guid.NewGuid():N}");

        // Act
        var result = await RunCutubeAsync(new[] { testUrl, "-o", outputPath });

        // Assert
        result.ExitCode.Should().Be(0);
        result.Output.Should().Contain("Download completed");

        // Cleanup
        if (Directory.Exists(outputPath))
            Directory.Delete(outputPath, recursive: true);
    }

    [Fact(Skip = "Requires yt-dlp installed")]
    public async Task Download_WithTimeRange_Succeeds()
    {
        // Arrange
        var testUrl = "https://www.youtube.com/watch?v=jNQXAC9IVRw";
        var outputPath = Path.Combine(Path.GetTempPath(), $"cutube_test_{Guid.NewGuid():N}");

        // Act
        var result = await RunCutubeAsync(new[] {
            testUrl,
            "-s", "00:00:10",  // start
            "-e", "00:00:20",  // end
            "-o", outputPath
        });

        // Assert
        result.ExitCode.Should().Be(0);
        result.Output.Should().Contain("Processing completed");

        // Cleanup
        if (Directory.Exists(outputPath))
            Directory.Delete(outputPath, recursive: true);
    }

    [Fact]
    public async Task Download_InvalidUrl_ReturnsError()
    {
        // Arrange
        var invalidUrl = "not-a-valid-url";

        // Act
        var result = await RunCutubeAsync(new[] { invalidUrl });

        // Assert
        result.ExitCode.Should().NotBe(0);
        result.Output.Should().Contain("Invalid URL");
    }

    [Fact(Skip = "Requires yt-dlp installed")]
    public async Task Download_AudioOnly_Succeeds()
    {
        // Arrange
        var testUrl = "https://www.youtube.com/watch?v=jNQXAC9IVRw";
        var outputPath = Path.Combine(Path.GetTempPath(), $"cutube_test_{Guid.NewGuid():N}");

        // Act
        var result = await RunCutubeAsync(new[] { testUrl, "--audio-only", "-o", outputPath });

        // Assert
        result.ExitCode.Should().Be(0);
        result.Output.Should().Contain("Download completed");

        // Verify MP3 file created
        var mp3Files = Directory.GetFiles(outputPath, "*.mp3");
        mp3Files.Should().NotBeEmpty();

        // Cleanup
        if (Directory.Exists(outputPath))
            Directory.Delete(outputPath, recursive: true);
    }

    private async Task<(int ExitCode, string Output)> RunCutubeAsync(string[] args)
    {
        var processStartInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"run --project cutube/cutube.csproj -- {string.Join(" ", args)}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        using var process = Process.Start(processStartInfo);
        await process!.WaitForExitAsync();

        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();

        _output.WriteLine($"STDOUT: {output}");
        _output.WriteLine($"STDERR: {error}");

        return (process.ExitCode, output + Environment.NewLine + error);
    }
}
```

**Checklist:**
- [ ] Criar testes de integração em Cutube.Tests
- [ ] Testar CLI com yt-dlp real (ou mock com Skip)
- [ ] Testar todas funcionalidades (download, timerange, audio-only)
- [ ] Testar validações (URL inválida)
- [ ] Testar cancelamento (CTRL+C)

**Critérios de aceito:**
- ✅ Todos testes passam
- ✅ CLI funcionalidade preservada
- ✅ Testes podem ser executados com `dotnet test`

---

## Qualidade Gates - Fase 2.1

**ANTES de passar para Fase 2.2, TODOS os itens abaixo devem ser concluídos:**

- [ ] **dotnet build** - **ZERO warnings** em todos projetos
- [ ] **dotnet test** - **100% pass** em todos projetos
- [ ] Cobertura de testes > 80% (Cutube.Domain.Tests)
- [ ] CLI funciona **idêntico ao antes** (testes E2E passam)
- [ ] Code review aprovado
- [ ] Documentação XML completa em interfaces/services públicos

---

## Cronograma Detalhado

| Tarefa | Estimativa | Dependencies | Blocker |
|--------|-----------|--------------|---------|
| 2.1.1 Criar projeto Cutube.Domain | 2h | Épico 1 | Não |
| 2.1.2 Criar interfaces | 4h | 2.1.1 | Não |
| 2.1.3 Criar models | 3h | 2.1.1 | Não |
| 2.1.4 Criar serviços | 8h | 2.1.2, 2.1.3 | **Sim** |
| 2.1.5 Criar testes Domain | 4h | 2.1.4 | **Sim** |
| 2.1.6 Refatorar CLI | 6h | 2.1.4 | **Sim** |
| 2.1.7 Testes E2E CLI | 3h | 2.1.6 | **Sim** |

**Total:** 30 horas (4-5 dias)

---

## Tecnologias

- **.NET 10** - Framework
- **xUnit** - Testes unitários
- **Moq** - Mocking framework
- **FluentAssertions** - Asserts fluentes
- **FluentResults** - Result pattern

---

## Próximos Passos

Após completar Fase 2.1:

1. ✅ **Fase 2.2**: Criar REST API
   - Implementar endpoints para expor Domain
   - Configurar Minimal API
   - Swagger/OpenAPI

2. 🎯 **Code Review**
   - Revisar separação de responsabilidades
   - Validar arquitetura em camadas

3. 📝 **Documentação**
   - Atualizar README com nova estrutura
   - Documentar interfaces públicas
