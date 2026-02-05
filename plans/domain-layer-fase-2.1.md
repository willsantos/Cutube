# Plano de Implementação - Domain Layer (Fase 2.1)

## Objetivo
Implementar arquitetura em camadas com Domain Layer isolado, criando projeto Cutube.Domain com interfaces, models e serviços de domínio reutilizáveis por CLI e futura REST API.

## Problema Atual
- Lógica de domínio misturada com CLI (YtDlpHelper, FfmpegHelper, ValidationHelper)
- Dificulta reuso por REST API
- Difícil testar sem dependências de Console
- Sem separação clara entre domínio e infraestrutura

## Solução
Criar Cutube.Domain como Class Library .NET 10 com:
- Interfaces para isolar dependências externas (yt-dlp, ffmpeg, filesystem)
- Models/DTOs de domínio puro (sem dependências de infraestrutura)
- Services com lógica de domínio extraída do CLI
- Injeção de dependências para testabilidade

## Branch
`feature/domain-layer-setup`

## Tarefas Incluídas
- ✅ **Cutube-a9l**: Criar projeto Cutube.Domain
- ✅ **Cutube-js1**: Criar abstrações para dependências externas
- ✅ **Cutube-3ds**: Criar models de domínio
- ✅ **Cutube-0q7**: Criar serviços de domínio

---

## Estrutura Final

```
src/Cutube.Domain/
├── Cutube.Domain.csproj
├── Interfaces/
│   ├── IVideoMetadataProvider.cs
│   ├── IVideoDownloader.cs
│   ├── IVideoProcessor.cs
│   ├── IFileSystem.cs
│   ├── IProgressReporter.cs
│   └── IDownloadValidator.cs
├── Models/
│   ├── DownloadRequest.cs
│   ├── DownloadProgress.cs
│   ├── DownloadResult.cs
│   ├── VideoMetadata.cs
│   ├── ProcessingRequest.cs
│   ├── ProcessingProgress.cs
│   ├── ProcessingResult.cs
│   ├── ValidationResult.cs
│   └── TimeRange.cs
└── Services/
    ├── DownloadService.cs
    ├── MetadataService.cs
    ├── ValidationService.cs
    └── ProcessingService.cs
```

---

## Commits Planejados

### 1. `feat: create Cutube.Domain project structure`
**Arquivo:** `src/Cutube.Domain/Cutube.Domain.csproj` (NOVO)

**Ações:**
- Criar solution folder `src/` no cutube.sln
- Criar projeto Class Library com .NET 10
- Configurar root namespace: `Cutube.Domain`
- Adicionar ao cutube.sln
- Build sem warnings

**.csproj:**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <RootNamespace>Cutube.Domain</RootNamespace>
  </PropertyGroup>
</Project>
```

**Verificação:**
```bash
dotnet build src/Cutube.Domain/Cutube.Domain.csproj
# Esperado: sucesso, sem warnings
```

---

### 2. `feat: add domain interfaces for external dependencies`
**Arquivos:** `src/Cutube.Domain/Interfaces/*.cs` (NOVOS)

**Interfaces a criar:**

#### `IVideoMetadataProvider.cs`
```csharp
namespace Cutube.Domain.Interfaces;

public interface IVideoMetadataProvider
{
    Task<VideoMetadata> GetMetadataAsync(string url, CancellationToken ct = default);
}
```

#### `IVideoDownloader.cs`
```csharp
namespace Cutube.Domain.Interfaces;

public interface IVideoDownloader
{
    Task<DownloadResult> DownloadAsync(
        DownloadRequest request,
        IProgress<DownloadProgress>? progress = null,
        CancellationToken ct = default);
}
```

#### `IVideoProcessor.cs`
```csharp
namespace Cutube.Domain.Interfaces;

public interface IVideoProcessor
{
    Task<ProcessingResult> ProcessAsync(
        ProcessingRequest request,
        IProgress<ProcessingProgress>? progress = null,
        CancellationToken ct = default);
}
```

#### `IFileSystem.cs`
```csharp
namespace Cutube.Domain.Interfaces;

public interface IFileSystem
{
    bool Exists(string path);
    void Delete(string path);
    Task WriteAllBytesAsync(string path, byte[] data);
    DateTime GetLastWriteTime(string path);
    bool DirectoryExists(string path);
    bool HasWritePermission(string path);
    void CreateDirectory(string path);
}
```

#### `IProgressReporter.cs`
```csharp
namespace Cutube.Domain.Interfaces;

public interface IProgressReporter
{
    void Report(int percentage);
    void Report(string message);
}
```

#### `IDownloadValidator.cs`
```csharp
namespace Cutube.Domain.Interfaces;

public interface IDownloadValidator
{
    ValidationResult ValidateUrl(string url);
    ValidationResult ValidateFileName(string name);
    ValidationResult ValidateTimeRange(string start, string end);
    ValidationResult ValidateDirectory(string path);
}
```

**Documentação XML:**
- Todas interfaces com `<summary>` XML comments
- `<param>` para parâmetros
- `<returns>` para retornos
- `<exception>` para exceções

---

### 3. `feat: add domain models and DTOs`
**Arquivos:** `src/Cutube.Domain/Models/*.cs` (NOVOS)

**Models a criar:**

#### `VideoMetadata.cs`
```csharp
namespace Cutube.Domain.Models;

public record VideoMetadata
{
    public required string Url { get; init; }
    public required string Title { get; init; }
    public required string? Uploader { get; init; }
    public required TimeSpan Duration { get; init; }
    public required DateTime UploadDate { get; init; }
    public required string ThumbnailUrl { get; init; }
}
```

#### `DownloadRequest.cs`
```csharp
namespace Cutube.Domain.Models;

public record DownloadRequest
{
    public required string Url { get; init; }
    public required string OutputPath { get; init; }
    public TimeRange? TimeRange { get; init; }
    public bool AudioOnly { get; init; }
}
```

#### `DownloadProgress.cs`
```csharp
namespace Cutube.Domain.Models;

public record DownloadProgress
{
    public required string State { get; init; }
    public required float Percentage { get; init; }
    public required long DownloadedBytes { get; init; }
    public required long TotalBytes { get; init; }
    public required float Speed { get; init; }
    public required string? ErrorMessage { get; init; }
}
```

#### `DownloadResult.cs`
```csharp
namespace Cutube.Domain.Models;

public record DownloadResult
{
    public required bool Success { get; init; }
    public required string OutputPath { get; init; }
    public required long FileSizeBytes { get; init; }
    public required TimeSpan Duration { get; init; }
    public required string? ErrorMessage { get; init; }
}
```

#### `ProcessingRequest.cs`
```csharp
namespace Cutube.Domain.Models;

public record ProcessingRequest
{
    public required string InputPath { get; init; }
    public required string OutputPath { get; init; }
    public required TimeRange TimeRange { get; init; }
    public bool AudioOnly { get; init; }
}
```

#### `ProcessingProgress.cs`
```csharp
namespace Cutube.Domain.Models;

public record ProcessingProgress
{
    public required int Percentage { get; init; }
    public required string CurrentOperation { get; init; }
}
```

#### `ProcessingResult.cs`
```csharp
namespace Cutube.Domain.Models;

public record ProcessingResult
{
    public required bool Success { get; init; }
    public required string OutputPath { get; init; }
    public required long FileSizeBytes { get; init; }
    public required string? ErrorMessage { get; init; }
}
```

#### `TimeRange.cs`
```csharp
namespace Cutube.Domain.Models;

public record TimeRange
{
    public required int StartSeconds { get; init; }
    public required int EndSeconds { get; init; }

    public int DurationSeconds => EndSeconds - StartSeconds;

    public TimeRange(int startSeconds, int endSeconds)
    {
        if (startSeconds <= 0)
            throw new ArgumentException("Start must be greater than 0", nameof(startSeconds));
        if (endSeconds <= startSeconds)
            throw new ArgumentException("End must be greater than start", nameof(endSeconds));

        StartSeconds = startSeconds;
        EndSeconds = endSeconds;
    }

    public static TimeRange FromStrings(string start, string end)
    {
        // Usa TimeHelper existente
        var startSec = TimeHelper.ParseToSeconds(start);
        var endSec = TimeHelper.ParseToSeconds(end);
        return new TimeRange(startSec, endSec);
    }
}
```

#### `ValidationResult.cs`
```csharp
namespace Cutube.Domain.Models;

public record ValidationResult
{
    public required bool IsValid { get; init; }
    public required string? ErrorMessage { get; init; }

    public static ValidationResult Success() => new() { IsValid = true };
    public static ValidationResult Failure(string message) => new() { IsValid = false, ErrorMessage = message };
}
```

**Validação em constructors:**
- TimeRange lança ArgumentException se inválido
- Records para imutabilidade
- Propriedades required para validação em compile-time

---

### 4. `feat: add ValidationService`
**Arquivo:** `src/Cutube.Domain/Services/ValidationService.cs` (NOVO)

**Extraído de:** `cutube/ValidationHelper.cs`

```csharp
using Cutube.Domain.Interfaces;
using Cutube.Domain.Models;

namespace Cutube.Domain.Services;

public class ValidationService : IDownloadValidator
{
    private static readonly string[] ValidYouTubeHosts =
    [
        "youtube.com",
        "www.youtube.com",
        "m.youtube.com",
        "youtu.be"
    ];

    public ValidationResult ValidateUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return ValidationResult.Failure("URL não pode ser vazia");

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return ValidationResult.Failure("URL inválida");

        if (uri.Scheme != "http" && uri.Scheme != "https")
            return ValidationResult.Failure("URL deve usar http ou https");

        var host = uri.Host.ToLowerInvariant();
        if (!ValidYouTubeHosts.Contains(host))
            return ValidationResult.Failure("URL deve ser do YouTube");

        return ValidationResult.Success();
    }

    public ValidationResult ValidateFileName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return ValidationResult.Success(); // Nome vazio é válido (opcional)

        var invalidChars = ['<', '>', ':', '"', '|', '?', '*', '/', '\\'];
        if (name.Any(c => invalidChars.Contains(c)))
            return ValidationResult.Failure("Nome contém caracteres inválidos");

        if (name.Contains('/') || name.Contains('\\'))
            return ValidationResult.Failure("Nome não pode conter caminho de diretório");

        return ValidationResult.Success();
    }

    public ValidationResult ValidateTimeRange(string start, string end)
    {
        try
        {
            var startSec = TimeHelper.ParseToSeconds(start);
            var endSec = TimeHelper.ParseToSeconds(end);

            if (startSec <= 0)
                return ValidationResult.Failure("Tempo de início deve ser maior que zero");

            if (endSec <= startSec)
                return ValidationResult.Failure("Tempo de fim deve ser maior que o início");

            return ValidationResult.Success();
        }
        catch (FormatException ex)
        {
            return ValidationResult.Failure($"Formato de tempo inválido: {ex.Message}");
        }
    }

    public ValidationResult ValidateDirectory(string path, IFileSystem fileSystem)
    {
        if (!fileSystem.DirectoryExists(path))
            return ValidationResult.Failure("Diretório não existe");

        if (!fileSystem.HasWritePermission(path))
            return ValidationResult.Failure("Sem permissão de escrita no diretório");

        return ValidationResult.Success();
    }
}
```

**Nota:** TimeHelper continuará no namespace `cutube` por enquanto (será movido em refatoração futura).

---

### 5. `feat: add MetadataService`
**Arquivo:** `src/Cutube.Domain/Services/MetadataService.cs` (NOVO)

**Extraído de:** `cutube/YtDlpHelper.cs` (método `GetVideoTitleAsync`)

```csharp
using Cutube.Domain.Interfaces;
using Cutube.Domain.Models;

namespace Cutube.Domain.Services;

public class MetadataService : IVideoMetadataProvider
{
    private readonly IVideoMetadataProvider _provider;

    public MetadataService(IVideoMetadataProvider provider)
    {
        _provider = provider;
    }

    public async Task<VideoMetadata> GetMetadataAsync(string url, CancellationToken ct = default)
    {
        var validationResult = ValidateUrl(url);
        if (!validationResult.IsValid)
            throw new ArgumentException(validationResult.ErrorMessage);

        var metadata = await _provider.GetMetadataAsync(url, ct);

        // Sanitizar título para uso como filename
        var sanitizedTitle = SanitizeTitle(metadata.Title);

        return metadata with { Title = sanitizedTitle };
    }

    private ValidationResult ValidateUrl(string url)
    {
        var validator = new ValidationService();
        return validator.ValidateUrl(url);
    }

    private string SanitizeTitle(string title)
    {
        // Remover caracteres inválidos de filename
        var invalidChars = new char[] { '<', '>', ':', '"', '|', '?', '*' };
        var sanitized = title;

        foreach (var c in invalidChars)
        {
            sanitized = sanitized.Replace(c, ' ');
        }

        // Remover espaços duplicados
        sanitized = System.Text.RegularExpressions.Regex.Replace(sanitized, @"\s+", " ");

        return sanitized.Trim();
    }
}
```

**Nota:** Implementação concreta de `IVideoMetadataProvider` (yt-dlp wrapper) será feita em projeto de infraestrutura futuro.

---

### 6. `feat: add DownloadService`
**Arquivo:** `src/Cutube.Domain/Services/DownloadService.cs` (NOVO)

**Extraído de:** `cutube/YtDlpHelper.cs` (métodos `DownloadAsync`, `DownloadWithTimeRangeAsync`, `DownloadAudioAsync`)

```csharp
using Cutube.Domain.Interfaces;
using Cutube.Domain.Models;

namespace Cutube.Domain.Services;

public class DownloadService : IVideoDownloader
{
    private readonly IVideoDownloader _downloader;
    private readonly IVideoProcessor _processor;
    private readonly IFileSystem _fileSystem;
    private readonly IDownloadValidator _validator;

    public DownloadService(
        IVideoDownloader downloader,
        IVideoProcessor processor,
        IFileSystem fileSystem,
        IDownloadValidator validator)
    {
        _downloader = downloader;
        _processor = processor;
        _fileSystem = fileSystem;
        _validator = validator;
    }

    public async Task<DownloadResult> DownloadAsync(
        DownloadRequest request,
        IProgress<DownloadProgress>? progress = null,
        CancellationToken ct = default)
    {
        // Validar request
        var urlValidation = _validator.ValidateUrl(request.Url);
        if (!urlValidation.IsValid)
            throw new ArgumentException(urlValidation.ErrorMessage);

        var dirValidation = _validator.ValidateDirectory(
            Path.GetDirectoryName(request.OutputPath) ?? Directory.GetCurrentDirectory(),
            _fileSystem
        );
        if (!dirValidation.IsValid)
            throw new ArgumentException(dirValidation.ErrorMessage);

        // Download completo ou com recorte de tempo
        if (request.TimeRange == null)
        {
            return await _downloader.DownloadAsync(request, progress, ct);
        }
        else
        {
            return await DownloadWithTimeRangeAsync(request, progress, ct);
        }
    }

    private async Task<DownloadResult> DownloadWithTimeRangeAsync(
        DownloadRequest request,
        IProgress<DownloadProgress>? progress,
        CancellationToken ct)
    {
        // Download completo para temp
        var tempFile = Path.GetTempFileName();

        try
        {
            // 1. Download completo
            var fullDownload = await _downloader.DownloadAsync(
                request with { OutputPath = tempFile },
                progress,
                ct
            );

            if (!fullDownload.Success)
                return fullDownload;

            // 2. Processar (cortar ou extrair áudio)
            var processingRequest = new ProcessingRequest
            {
                InputPath = tempFile,
                OutputPath = request.OutputPath,
                TimeRange = request.TimeRange.Value,
                AudioOnly = request.AudioOnly
            };

            var processingResult = await _processor.ProcessAsync(processingRequest, progress, ct);

            // 3. Limpar temp
            _fileSystem.Delete(tempFile);

            return new DownloadResult
            {
                Success = processingResult.Success,
                OutputPath = processingResult.OutputPath,
                FileSizeBytes = processingResult.FileSizeBytes,
                Duration = request.TimeRange.Value.DurationSeconds,
                ErrorMessage = processingResult.ErrorMessage
            };
        }
        catch (OperationCanceledException)
        {
            _fileSystem.Delete(tempFile);
            _fileSystem.Delete(request.OutputPath);
            throw;
        }
    }
}
```

---

### 7. `feat: add ProcessingService`
**Arquivo:** `src/Cutube.Domain/Services/ProcessingService.cs` (NOVO)

**Extraído de:** `cutube/FfmpegHelper.cs` (método `ExecuteFfmpeg`)

```csharp
using Cutube.Domain.Interfaces;
using Cutube.Domain.Models;

namespace Cutube.Domain.Services;

public class ProcessingService : IVideoProcessor
{
    private readonly IVideoProcessor _processor;
    private readonly IFileSystem _fileSystem;

    public ProcessingService(IVideoProcessor processor, IFileSystem fileSystem)
    {
        _processor = processor;
        _fileSystem = fileSystem;
    }

    public async Task<ProcessingResult> ProcessAsync(
        ProcessingRequest request,
        IProgress<ProcessingProgress>? progress = null,
        CancellationToken ct = default)
    {
        if (!_fileSystem.Exists(request.InputPath))
            throw new FileNotFoundException("Arquivo de entrada não encontrado", request.InputPath);

        // Validar time range
        if (request.TimeRange.StartSeconds >= request.TimeRange.EndSeconds)
            throw new ArgumentException("Time range inválido");

        // Delegar para implementação concreta (FFmpeg wrapper)
        var result = await _processor.ProcessAsync(request, progress, ct);

        // Validar resultado
        if (!result.Success || !_fileSystem.Exists(result.OutputPath))
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
```

**Nota:** Implementação concreta de `IVideoProcessor` (FFmpeg wrapper) será feita em projeto de infraestrutura futuro.

---

### 8. `refactor: update solution to include src/ folder`
**Arquivo:** `cutube.sln` (MODIFICAR)

**Adicionar solution folder e projeto:**

```xml
Project("{2150E333-8FDC-42A3-9474-1A3956D46DE8}") = "src", "src", "{GUID-SRC}"
EndProject
Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "Cutube.Domain", "src\Cutube.Domain\Cutube.Domain.csproj", "{GUID-DOMAIN}"
EndProject
```

**Verificação:**
```bash
dotnet sln list
# Deve mostrar src/Cutube.Domain/Cutube.Domain.csproj
```

---

## Critérios de Aceite

### Projeto Cutube.Domain
- [ ] Projeto Class Library .NET 10 criado
- [ ] Adicionado ao cutube.sln em solution folder `src/`
- [ ] Namespace configurado: `Cutube.Domain.*`
- [ ] Build sem warnings: `dotnet build src/Cutube.Domain`
- [ ] Sem dependências de Console/CLI

### Interfaces (Cutube-js1)
- [ ] `IVideoMetadataProvider.cs` criado com XML comments
- [ ] `IVideoDownloader.cs` criado com XML comments
- [ ] `IVideoProcessor.cs` criado com XML comments
- [ ] `IFileSystem.cs` criado com XML comments
- [ ] `IProgressReporter.cs` criado com XML comments
- [ ] `IDownloadValidator.cs` criado com XML comments
- [ ] Todas interfaces compilam sem erros

### Models (Cutube-3ds)
- [ ] `VideoMetadata.cs` record criado
- [ ] `DownloadRequest.cs` record criado
- [ ] `DownloadProgress.cs` record criado
- [ ] `DownloadResult.cs` record criado
- [ ] `ProcessingRequest.cs` record criado
- [ ] `ProcessingProgress.cs` record criado
- [ ] `ProcessingResult.cs` record criado
- [ ] `TimeRange.cs` record criado com validação em constructor
- [ ] `ValidationResult.cs` record criado
- [ ] Todos models compilam sem erros
- [ ] Validação funcionando em TimeRange constructor

### Services (Cutube-0q7)
- [ ] `ValidationService.cs` criado (extraído de ValidationHelper)
- [ ] `MetadataService.cs` criado (extraído de YtDlpHelper)
- [ ] `DownloadService.cs` criado (extraído de YtDlpHelper)
- [ ] `ProcessingService.cs` criado (extraído de FfmpegHelper)
- [ ] Todos services com injeção de dependências via constructor
- [ ] Services sem dependência de Console/CLI
- [ ] Services compilam sem erros

### Qualidade
- [ ] Zero warnings no build: `dotnet build`
- [ ] Zero erros de compilação
- [ ] XML comments em todas interfaces públicas
- [ ] Records para imutabilidade onde aplicável
- [ ] Validação em constructors de models

---

## Notas de Implementação

### TimeHelper
- `TimeHelper` continua em `cutube/` namespace por enquanto
- Será movido para `Cutube.Domain` em refatoração futura
- `Cutube.Domain.Models.TimeRange` usa `TimeHelper.ParseToSeconds()`

### Dependências Externas
- Implementações concretas (yt-dlp, FFmpeg) ficarão em projeto de infraestrutura futuro
- Domain Layer define apenas interfaces e modelos
- CLI injetará implementações concretas em fase futura (Cutube-8i9)

### Path Sanitization
- `MetadataService.SanitizeTitle()` remove caracteres inválidos de filename
- Usa lógica de `TitleHelper.FormatTitle()` existente
- Será extraído de `TitleHelper` em refatoração futura

### Validação
- `ValidationService` é estático, sem estado
- Pode ser usado como singleton ou DI scoped
- Retorna `ValidationResult` em vez de lançar exceções (mais testável)

---

## Breaking Changes

**Nenhum breaking change**
- CLI continua funcionando sem modificações
- Domain Layer é código novo, paralelo ao existente
- Refatoração do CLI para usar Domain será feita em Cutube-8i9

---

## Próximos Passos (Futuros)

Após este plano, as seguintes tarefas dependem do Domain Layer:

- **Cutube-3ow**: Criar projeto de testes Cutube.Domain.Tests
- **Cutube-8i9**: Refatorar CLI para usar Domain Services
- **Cutube-8i6**: Testes E2E para CLI (com Domain)
- **Cutube-lsx**: REST API (reusa Domain Layer)

---

## Testes Manuais Sugeridos

### Compilação
```bash
dotnet build src/Cutube.Domain/Cutube.Domain.csproj
# Esperado: sucesso, sem warnings

dotnet build cutube.sln
# Esperado: sucesso, sem warnings
```

### Verificação de Namespace
```bash
# Verificar se não há referências a Console
grep -r "Console\." src/Cutube.Domain/
# Esperado: nenhum resultado

grep -r "namespace Cutube.Domain" src/Cutube.Domain/
# Esperado: matches em todos arquivos
```

### Validação de Models
```csharp
// Deve lançar ArgumentException
var range = new TimeRange(0, 10);

// Deve funcionar
var range = new TimeRange(10, 20);
Assert.That(range.DurationSeconds, Is.EqualTo(10));
```

---

## Estimativa

**Total:** ~17 horas
- Cutube-a9l (setup projeto): 2h
- Cutube-js1 (interfaces): 4h
- Cutube-3ds (models): 3h
- Cutube-0q7 (services): 8h

---

## Dependências

**Nenhuma**
- Todas as tarefas podem ser feitas independentemente
- Lógica existente no CLI serve como referência
- Não modifica código CLI ainda (será feito em Cutube-8i9)
