# Épico 2: Arquitetura Híbrida CLI + API

**Status:** 🎯 Planejamento
**Criado em:** 04/02/2026
**Estimativa:** 21-28 dias
**Dependência:** ✅ Épico 1 completo

---

## Objetivo Geral

Separar a lógica de domínio da apresentação, permitindo que CLI e Web coexistam como interfaces diferentes do mesmo núcleo de lógica.

**Benefícios:**
- 🎯 CLI se torna "thin client" do domínio
- 🌐 Web interface reutiliza toda lógica de negócio
- 📦 Código mais testável e maintainable
- 🔄 Preparação para sistema distribuído (Épico 3)

---

## Visão Arquitetural

```
┌─────────────────────────────────────────────┐
│            Presentation Layer               │
├─────────────────┬───────────────────────────┤
│   CLI (Console) │   Web UI (Next.js)        │
└────────┬────────┴───────────┬───────────────┘
         │                    │
         │    HTTP/REST       │
         │    + WebSocket     │
         ▼                    ▼
┌─────────────────────────────────────────────┐
│           Application Layer                 │
│         (ASP.NET Core API)                  │
└──────────────────┬──────────────────────────┘
                   │
                   │ Domain calls
                   ▼
┌─────────────────────────────────────────────┐
│            Domain Layer                     │
│        (Cutube.Domain Class Library)        │
│  - Services                                 │
│  - Models                                   │
│  - Interfaces                               │
│  - Business Logic                           │
└──────────────────┬──────────────────────────┘
                   │
                   │ External dependencies
                   ▼
┌─────────────────────────────────────────────┐
│         Infrastructure Layer                │
│  - yt-dlp                                   │
│  - ffmpeg                                   │
│  - File System                              │
└─────────────────────────────────────────────┘
```

---

## Fase 2.1: Refatoração para Domain Layer

**Duração:** 4-5 dias
**Responsável:** Backend Developer
**Prioridade:** 🔥 Alta (bloqueia demais fases)

### Objetivo
Extrair toda lógica de domínio do projeto CLI para um projeto separado (Class Library), eliminando dependências de Console e tornando o código reutilizável.

### Tarefas

#### 2.1.1 Criar projeto Cutube.Domain
**Estimativa:** 2 horas
**Arquivos:**
- `src/Cutube.Domain/Cutube.Domain.csproj`

**Checklist:**
- [ ] Criar solution folder `src/`
- [ ] Criar projeto Class Library com .NET 10
- [ ] Adicionar ao `cutube.sln`
- [ ] Configurar namespaces (Cutube.Domain.*)
- [ ] Build sem warnings

**Critérios de aceito:**
- ✅ Projeto cria com `dotnet build`
- ✅ Sem dependências de UI
- ✅ Namespace configurado corretamente

---

#### 2.1.2 Criar abstrações para dependências externas
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
public interface IVideoMetadataProvider
{
    Task<Result<VideoMetadata>> GetMetadataAsync(string url, CancellationToken ct = default);
}

// IVideoDownloader.cs
public interface IVideoDownloader
{
    Task<Result<DownloadResult>> DownloadAsync(
        DownloadRequest request,
        IProgress<DownloadProgress> progress,
        CancellationToken ct = default);
}

// IVideoProcessor.cs
public interface IVideoProcessor
{
    Task<Result<ProcessingResult>> ProcessAsync(
        ProcessingRequest request,
        IProgress<ProcessingProgress> progress,
        CancellationToken ct = default);
}

// IFileSystem.cs
public interface IFileSystem
{
    Task<Result<string>> CreateDirectoryAsync(string path);
    Task<Result<bool>> FileExistsAsync(string path);
    Task<Result<long>> GetFileSizeAsync(string path);
    Task<Result<string>> GetAvailableDiskSpaceAsync(string path);
}

// IProgressReporter.cs
public interface IProgressReporter<T> where T : class
{
    void Report(T progress);
}

// IDownloadValidator.cs
public interface IDownloadValidator
{
    Result<DownloadRequest> Validate(DownloadRequest request);
}
```

**Checklist:**
- [ ] Criar todas interfaces em `Cutube.Domain/Interfaces/`
- [ ] Documentar XML comments para cada método
- [ ] Definir models (`VideoMetadata`, `DownloadRequest`, etc.)
- [ ] Build sem warnings

**Critérios de aceito:**
- ✅ Todas interfaces definidas
- ✅ Sem dependência de Console
- ✅ Compila sem erros

---

#### 2.1.3 Criar models de domínio
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
  ├── ValidationResult.cs
  └── TimeRange.cs
```

**Models:**

```csharp
// DownloadRequest.cs
public class DownloadRequest
{
    public required string Url { get; init; }
    public string? OutputPath { get; init; }
    public TimeRange? TimeRange { get; init; }
    public bool AudioOnly { get; init; }
    public string? CustomFilename { get; init; }
}

// DownloadProgress.cs
public class DownloadProgress
{
    public required string DownloadId { get; init; }
    public double Percentage { get; init; }
    public long DownloadedBytes { get; init; }
    public long TotalBytes { get; init; }
    public double Speed { get; init; }      // bytes/s
    public TimeSpan? Eta { get; init; }
    public DownloadStatus Status { get; init; }
}

public enum DownloadStatus
{
    Queued,
    Downloading,
    Processing,
    Completed,
    Failed,
    Cancelled
}

// VideoMetadata.cs
public class VideoMetadata
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

public class VideoFormat
{
    public required string FormatId { get; init; }
    public required string Extension { get; init; }
    public string? Resolution { get; init; }
    public long? FileSize { get; init; }
}

// TimeRange.cs
public class TimeRange
{
    public required TimeSpan Start { get; init; }
    public required TimeSpan End { get; init; }
}
```

**Checklist:**
- [ ] Criar todos models em `Cutube.Domain/Models/`
- [ ] Implementar validação em constructors (required properties)
- [ ] Adicionar records para models imutáveis
- [ ] Criar DTOs para APIs (se necessário)

**Critérios de aceito:**
- ✅ Models compilam sem erros
- ✅ Validação funcionando
- ✅ Records para imutabilidade

---

#### 2.1.4 Criar serviços de domínio
**Estimativa:** 8 horas
**Arquivos:**
```
Cutube.Domain/Services/
  ├── DownloadService.cs
  ├── MetadataService.cs
  ├── ValidationService.cs
  └── ProcessingService.cs
```

**Implementações:**

```csharp
// DownloadService.cs
public class DownloadService : IDownloadService
{
    private readonly IVideoDownloader _downloader;
    private readonly IVideoProcessor _processor;
    private readonly IDownloadValidator _validator;
    private readonly IFileSystem _fileSystem;

    public DownloadService(
        IVideoDownloader downloader,
        IVideoProcessor processor,
        IDownloadValidator validator,
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
            return Result.Failure(validation.Error);

        // 2. Verificar espaço em disco
        var spaceCheck = await _fileSystem.GetAvailableDiskSpaceAsync(request.OutputPath ?? ".");
        if (spaceCheck.IsFailure || spaceCheck.Value < 100_000_000) // 100MB
            return Result.Failure("Insufficient disk space");

        // 3. Download
        var downloadResult = await _downloader.DownloadAsync(request, progress, ct);
        if (downloadResult.IsFailure)
            return Result.Failure(downloadResult.Error);

        // 4. Processamento (ffmpeg) se necessário
        if (request.TimeRange is not null || request.AudioOnly)
        {
            var processingRequest = new ProcessingRequest
            {
                InputPath = downloadResult.Value.FilePath,
                OutputPath = request.GetOutputPath(),
                TimeRange = request.TimeRange,
                AudioOnly = request.AudioOnly
            };

            var processResult = await _processor.ProcessAsync(
                processingRequest,
                progress,
                ct);

            if (processResult.IsFailure)
                return Result.Failure(processResult.Error);

            return Result.Success(new DownloadResult
            {
                FilePath = processResult.Value.OutputPath,
                Size = processResult.Value.Size,
                Duration = processResult.Value.Duration
            });
        }

        return downloadResult;
    }
}
```

**Checklist:**
- [ ] Mover lógica de `YtDlpHelper.cs` para `DownloadService`
- [ ] Mover lógica de `FfmpegHelper.cs` para `ProcessingService`
- [ ] Criar `ValidationService` a partir de `ValidationHelper.cs`
- [ ] Implementar `MetadataService` para extrair metadados
- [ ] Injeção de dependências em todos serviços

**Critérios de aceito:**
- ✅ Lógica de domínio extraída do CLI
- ✅ Services têm 0 dependência de Console
- ✅ Testes unitários para cada service
- ✅ Build sem warnings

---

#### 2.1.5 Criar projeto de testes Cutube.Domain.Tests
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

**Checklist:**
- [ ] Criar projeto xUnit
- [ ] Adicionar Moq para mocks
- [ ] Escrever testes para `ValidationService`
- [ ] Escrever testes para `DownloadService`
- [ ] Mock de dependências externas (IVideoDownloader, etc.)
- [ ] Cobertura > 80%

**Critérios de aceito:**
- ✅ Todos testes passam: `dotnet test`
- ✅ Cobertura > 80%
- ✅ Testes executam em < 5 segundos

---

#### 2.1.6 Refatorar CLI para usar Domain
**Estimativa:** 6 horas
**Arquivos:**
- `cutube/ProgramWorkflow.cs` - refatorar para chamar Domain
- `cutube/Menu.cs` - remover lógica de negócio
- `cutube/cutube.csproj` - adicionar referência a Cutube.Domain

**Mudanças:**

```csharp
// ProgramWorkflow.cs - ANTES
public class ProgramWorkflow
{
    public async Task ExecuteAsync(string url, TimeRange? range, ...)
    {
        // Lógica de download aqui (muito código)
        var ytdlp = new YtDlpHelper(...);
        await ytdlp.DownloadAsync(...);
    }
}

// ProgramWorkflow.cs - DEPOIS
public class ProgramWorkflow
{
    private readonly IDownloadService _downloadService;
    private readonly IProgress<DownloadProgress> _progress;

    public ProgramWorkflow(IDownloadService downloadService, IProgress<DownloadProgress> progress)
    {
        _downloadService = downloadService;
        _progress = progress;
    }

    public async Task<Result<DownloadResult>> ExecuteAsync(string url, TimeRange? range, ...)
    {
        var request = new DownloadRequest
        {
            Url = url,
            TimeRange = range,
            // ...
        };

        return await _downloadService.DownloadAsync(request, _progress);
    }
}
```

**Checklist:**
- [ ] Adicionar referência a Cutube.Domain
- [ ] Injetar dependências no Program.cs
- [ ] Converter `ProgramWorkflow` para usar `IDownloadService`
- [ ] Converter `Menu` para apenas coletar inputs
- [ ] Remover lógica de negócio do CLI
- [ ] Testar E2E: CLI funciona igual antes

**Critérios de aceito:**
- ✅ CLI funciona exatamente como antes
- ✅ 100% dos testes passam
- ✅ Build sem warnings
- ✅ Zero lógica de negócio no CLI

---

#### 2.1.7 Testes E2E para CLI
**Estimativa:** 3 horas
**Arquivos:**
```
Cutube.Tests/Integration/
  └── CliEndToEndTests.cs
```

**Cenários de teste:**
- Download completo (URL simples)
- Download com timerange
- Download audio-only
- Validação de URL inválida
- Cancellation token (CTRL+C)

**Checklist:**
- [ ] Criar testes de integração
- [ ] Testar CLI com yt-dlp real (ou mock)
- [ ] Testar todas funcionalidades
- [ ] Testar cancelamento

**Critérios de aceito:**
- ✅ Todos testes passam
- ✅ CLI funcionalidade preservada

---

### Qualidade Gates - Fase 2.1

Antes de passar para Fase 2.2:

- [ ] `dotnet build` - **ZERO warnings**
- [ ] `dotnet test` - **100% pass**
- [ ] Cobertura de testes > 80%
- [ ] CLI funciona idêntico ao antes
- [ ] Code review aprovado

---

## Fase 2.2: REST API

**Duração:** 5-6 dias
**Responsável:** Backend Developer
**Prioridade:** 🔥 Alta

### Objetivo
Criar uma API REST em ASP.NET Core que expõe as funcionalidades do domínio via HTTP, preparando para integração com Web UI e sistema distribuído.

### Tarefas

#### 2.2.1 Criar projeto Cutube.Api
**Estimativa:** 2 horas
**Arquivos:**
- `src/Cutube.Api/Cutube.Api.csproj`
- `src/Cutube.Api/Program.cs`

**Configuração:**
```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Swashbuckle.AspNetCore" Version="7.2.0" />
    <PackageReference Include="Microsoft.AspNetCore.SignalR" Version="9.0.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="../Cutube.Domain/Cutube.Domain.csproj" />
  </ItemGroup>
</Project>
```

**Checklist:**
- [ ] Criar projeto ASP.NET Core Minimal API
- [ ] Adicionar ao solution
- [ ] Configurar launchSettings.json (portas 5000/5001)
- [ ] Adicionar referência a Cutube.Domain
- [ ] Build sem warnings

**Critérios de aceito:**
- ✅ Projeto cria com `dotnet run`
- ✅ CORS configurado para localhost:3000
- ✅ Swagger habilitado

---

#### 2.2.2 Implementar endpoint: POST /api/downloads
**Estimativa:** 4 horas
**Arquivo:** `src/Cutube.Api/Endpoints/DownloadsEndpoints.cs`

**Request/Response:**
```csharp
// Request
public record CreateDownloadRequest(
    string Url,
    string? OutputPath,
    string? StartTime,
    string? EndTime,
    bool AudioOnly = false,
    string? CustomFilename = null
);

// Response
public record CreateDownloadResponse(
    string DownloadId,
    string Status,
    string? Message = null
);
```

**Implementação:**
```csharp
// DownloadsEndpoints.cs
app.MapPost("/api/downloads", async (
    CreateDownloadRequest request,
    IDownloadService downloadService,
    IDownloadQueue downloadQueue,
    CancellationToken ct) =>
{
    // Validar
    var validation = await ValidateRequest(request);
    if (validation.IsFailure)
        return Results.BadRequest(validation.Error);

    // Criar download request
    var downloadRequest = MapToDomainRequest(request);

    // Enfileirar (processamento assíncrono)
    var downloadId = await downloadQueue.EnqueueAsync(downloadRequest, ct);

    // Retornar imediatamente
    return Results.Accepted($"/api/downloads/{downloadId}", new CreateDownloadResponse(
        downloadId,
        "queued"
    ));
})
.Accepts<CreateDownloadRequest>("application/json")
.Produces<CreateDownloadResponse>(StatusCodes.Status202Accepted)
.Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
.WithName("CreateDownload")
.WithOpenApi();
```

**Checklist:**
- [ ] Criar endpoint POST /api/downloads
- [ ] Validar request (URL, timerange)
- [ ] Enfileirar download (não bloquear)
- [ ] Retornar 202 Accepted com downloadId
- [ ] Documentar com Swagger/OpenAPI

**Critérios de aceito:**
- ✅ Endpoint responde em < 100ms
- ✅ Download é processado em background
- ✅ Swagger UI mostra endpoint

---

#### 2.2.3 Implementar endpoint: GET /api/downloads
**Estimativa:** 3 horas
**Arquivo:** `src/Cutube.Api/Endpoints/DownloadsEndpoints.cs`

**Response:**
```csharp
public record DownloadSummary(
    string DownloadId,
    string Url,
    string Status,
    double Progress,
    string? FilePath = null,
    DateTime CreatedAt = default
);

public record GetDownloadsResponse(
    DownloadSummary[] Downloads,
    int TotalCount
);
```

**Implementação:**
```csharp
app.MapGet("/api/downloads", async (
    IDownloadStatusRepository statusRepository,
    CancellationToken ct) =>
{
    var downloads = await statusRepository.GetAllAsync(ct);

    var summaries = downloads.Select(d => new DownloadSummary(
        d.Id,
        d.Url,
        d.Status.ToString(),
        d.Progress,
        d.FilePath,
        d.CreatedAt
    ));

    return Results.Ok(new GetDownloadsResponse(
        summaries.ToArray(),
        summaries.Count()
    ));
})
.Produces<GetDownloadsResponse>(StatusCodes.Status200OK)
.WithName("GetDownloads")
.WithOpenApi();
```

**Checklist:**
- [ ] Criar endpoint GET /api/downloads
- [ ] Retornar lista de downloads
- [ ] Suportar filtros (status, date range)
- [ ] Paginação opcional

**Critérios de aceito:**
- ✅ Lista todos downloads
- ✅ Filtra por status (opcional)
- ✅ Performance OK

---

#### 2.2.4 Implementar endpoint: GET /api/downloads/{id}
**Estimativa:** 3 horas
**Response:**
```csharp
public record DownloadDetails(
    string DownloadId,
    string Url,
    string Status,
    double Progress,
    double Speed,
    string? Eta,
    long? DownloadedBytes,
    long? TotalBytes,
    string? FilePath,
    string? ErrorMessage,
    DateTime CreatedAt,
    DateTime? CompletedAt
);
```

**Checklist:**
- [ ] Criar endpoint GET /api/downloads/{id}
- [ ] Retornar 404 se não encontrado
- [ ] Incluir todos detalhes de progresso

**Critérios de aceito:**
- ✅ Detalhes completos do download
- ✅ 404 para ID inválido

---

#### 2.2.5 Implementar endpoint: DELETE /api/downloads/{id}
**Estimativa:** 2 horas
**Checklist:**
- [ ] Criar endpoint DELETE /api/downloads/{id}
- [ ] Cancelar download se em andamento
- [ ] Remover da lista
- [ ] Retornar 204 No Content

**Critérios de aceito:**
- ✅ Download cancelado com sucesso
- ✅ Arquivos parcialmente baixados removidos

---

#### 2.2.6 Implementar endpoint: GET /api/videos/info
**Estimativa:** 4 horas
**Request:**
```csharp
public record GetVideoInfoRequest(string Url);
```

**Response:**
```csharp
public record VideoInfoResponse(
    string Id,
    string Title,
    string Uploader,
    string Duration,
    string ThumbnailUrl,
    long? ViewCount,
    VideoFormatDto[] Formats
);

public record VideoFormatDto(
    string FormatId,
    string Extension,
    string? Resolution,
    long? FileSize
);
```

**Implementação:**
```csharp
app.MapGet("/api/videos/info", async (
    [FromQuery] string url,
    IMetadataService metadataService,
    CancellationToken ct) =>
{
    var metadataResult = await metadataService.GetMetadataAsync(url, ct);

    if (metadataResult.IsFailure)
        return Results.BadRequest(new { error = metadataResult.Error });

    var metadata = metadataResult.Value;

    return Results.Ok(new VideoInfoResponse(
        metadata.Id,
        metadata.Title,
        metadata.Uploader,
        metadata.Duration.ToString(),
        metadata.ThumbnailUrl,
        metadata.ViewCount,
        metadata.Formats.Select(f => new VideoFormatDto(
            f.FormatId,
            f.Extension,
            f.Resolution,
            f.FileSize
        )).ToArray()
    ));
})
.Produces<VideoInfoResponse>(StatusCodes.Status200OK)
.Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
.WithName("GetVideoInfo")
.WithOpenApi();
```

**Checklist:**
- [ ] Criar endpoint GET /api/videos/info?url=
- [ ] Chamar MetadataService do domínio
- [ ] Retornar metadados completos
- [ ] Cache de metadados (opcional)

**Critérios de aceito:**
- ✅ Metadados retornados em < 2s
- ✅ Suporta YouTube e outras plataformas

---

#### 2.2.7 Configurar CORS para Next.js
**Estimativa:** 1 hora
**Arquivo:** `src/Cutube.Api/Program.cs`

**Configuração:**
```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowNextJs", policy =>
    {
        policy.WithOrigins(
                "http://localhost:3000",
                "http://localhost:3001",
                "https://cutube.dev" // Produção
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();

app.UseCors("AllowNextJs");
```

**Checklist:**
- [ ] Configurar CORS policy para localhost:3000
- [ ] Testar com curl/Postman
- [ ] Adicionar suporte a credenciais (cookies, auth)

**Critérios de aceito:**
- ✅ Next.js pode chamar API sem erros CORS

---

#### 2.2.8 Configurar Health Checks
**Estimativa:** 2 horas
**Arquivo:** `src/Cutube.Api/Program.cs`

**Health checks:**
- `/health` - API health
- `/health/ready` - Dependencies check (yt-dlp, ffmpeg)
- `/health/live` - Liveness probe

**Implementação:**
```csharp
builder.Services.AddHealthChecks()
    .AddCheck<yt-dlp HealthCheck>("yt-dlp")
    .AddCheck<FfmpegHealthCheck>("ffmpeg")
    .AddCheck<DiskSpaceHealthCheck>("disk-space");

app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false
});
```

**Checklist:**
- [ ] Criar health checks para dependências
- [ ] Expor endpoints /health, /ready, /live
- [ ] Retornar 200 se healthy, 503 se unhealthy

**Critérios de aceito:**
- ✅ Health check funcionando
- ✅ yt-dlp/ffmpeg detectados corretamente

---

#### 2.2.9 Criar DownloadQueue (Background Processing)
**Estimativa:** 6 horas
**Arquivos:**
```
Cutube.Api/Services/
  ├── IDownloadQueue.cs
  ├── DownloadQueue.cs          - Memory queue
  ├── IDownloadStatusRepository.cs
  ├── InMemoryStatusRepository.cs
  └── BackgroundDownloadWorker.cs  - HostedService
```

**Implementação:**
```csharp
// IDownloadQueue.cs
public interface IDownloadQueue
{
    Task<string> EnqueueAsync(DownloadRequest request, CancellationToken ct);
    Task<DownloadRequest?> DequeueAsync(CancellationToken ct);
    Task CompleteAsync(string downloadId, DownloadResult result);
    Task FailAsync(string downloadId, string error);
}

// BackgroundDownloadWorker.cs
public class BackgroundDownloadWorker : BackgroundService
{
    private readonly IDownloadQueue _queue;
    private readonly IDownloadService _downloadService;
    private readonly IDownloadStatusRepository _statusRepository;

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await foreach (var (downloadId, request) in _queue.DequeueAllAsync(ct))
        {
            try
            {
                await _statusRepository.UpdateStatusAsync(downloadId, DownloadStatus.Processing);

                var progress = new Progress<DownloadProgress>(p =>
                {
                    _statusRepository.UpdateProgressAsync(downloadId, p);
                });

                var result = await _downloadService.DownloadAsync(request, progress, ct);

                if (result.IsFailure)
                {
                    await _statusRepository.UpdateStatusAsync(downloadId, DownloadStatus.Failed, result.Error);
                }
                else
                {
                    await _statusRepository.UpdateStatusAsync(downloadId, DownloadStatus.Completed, result.Value);
                }
            }
            catch (Exception ex)
            {
                await _statusRepository.UpdateStatusAsync(downloadId, DownloadStatus.Failed, ex.Message);
            }
        }
    }
}
```

**Checklist:**
- [ ] Criar fila em memória (Channel)
- [ ] Criar HostedService para processar fila
- [ ] Criar repositório de status em memória
- [ ] Atualizar status durante download
- [ ] Suportar múltiplos downloads simultâneos

**Critérios de aceito:**
- ✅ Downloads processados em background
- ✅ Status atualizado em tempo real
- ✅ Múltiplos downloads funcionam

---

#### 2.2.10 Testes de Integração da API
**Estimativa:** 5 horas
**Arquivos:**
```
Cutube.Api.Tests/
  ├── Integration/
  │   ├── DownloadsEndpointsTests.cs
  │   ├── VideosEndpointsTests.cs
  │   └── HealthChecksTests.cs
  └── Helpers/
      └── TestWebApplicationFactory.cs
```

**Cenários:**
- POST /api/downloads - criar download
- GET /api/downloads - listar downloads
- GET /api/downloads/{id} - detalhes
- DELETE /api/downloads/{id} - cancelar
- GET /api/videos/info - metadados
- Health checks

**Checklist:**
- [ ] Criar testes de integração
- [ ] Usar WebApplicationFactory
- [ ] Testar todos endpoints
- [ ] Mock de dependências externas
- [ ] Testar cenários de erro

**Critérios de aceito:**
- ✅ Todos testes passam
- ✅ Cobertura > 70%

---

### Qualidade Gates - Fase 2.2

Antes de passar para Fase 2.3:

- [ ] `dotnet build` - **ZERO warnings**
- [ ] `dotnet test` - **100% pass**
- [ ] Swagger UI acessível
- [ ] CORS configurado
- [ ] Health checks funcionando

---

## Fase 2.3: WebSocket para Progresso

**Duração:** 3-4 dias
**Responsável:** Backend Developer
**Prioridade:** 🔥 Alta

### Objetivo
Implementar comunicação bidirecional em tempo real via SignalR para atualizações de progresso de downloads.

### Tarefas

#### 2.3.1 Criar SignalR Hub
**Estimativa:** 3 horas
**Arquivo:** `src/Cutube.Api/Hubs/DownloadHub.cs`

**Implementação:**
```csharp
// DownloadHub.cs
public interface IDownloadHubClient
{
    Task DownloadStarted(string downloadId, DownloadStartedEvent data);
    Task DownloadProgress(string downloadId, DownloadProgressEvent data);
    Task DownloadCompleted(string downloadId, DownloadCompletedEvent data);
    Task DownloadFailed(string downloadId, DownloadFailedEvent data);
    Task DownloadCancelled(string downloadId);
}

public class DownloadHub : Hub<IDownloadHubClient>
{
    public async Task JoinDownloadGroup(string downloadId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"download:{downloadId}");
    }

    public async Task LeaveDownloadGroup(string downloadId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"download:{downloadId}");
    }
}
```

**Events:**
```csharp
public record DownloadStartedEvent(
    string DownloadId,
    string Url,
    DateTime StartedAt
);

public record DownloadProgressEvent(
    string DownloadId,
    double Progress,
    double Speed,
    string? Eta,
    long DownloadedBytes,
    long TotalBytes
);

public record DownloadCompletedEvent(
    string DownloadId,
    string FilePath,
    long Size,
    TimeSpan Duration,
    DateTime CompletedAt
);

public record DownloadFailedEvent(
    string DownloadId,
    string Error,
    DateTime FailedAt
);
```

**Checklist:**
- [ ] Criar interface IDownloadHubClient
- [ ] Criar Hub DownloadHub
- [ ] Definir métodos de grupo (join/leave)
- [ ] Documentar events

**Critérios de aceito:**
- ✅ Hub compilado sem erros
- ✅ SignalR configurado no Program.cs

---

#### 2.3.2 Configurar SignalR no API
**Estimativa:** 2 horas
**Arquivo:** `src/Cutube.Api/Program.cs`

**Configuração:**
```csharp
builder.Services.AddSignalR(options =>
{
    options.KeepAliveInterval = TimeSpan.FromSeconds(10);
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
});

var app = builder.Build();

app.MapHub<DownloadHub>("/hubs/downloads");
```

**Checklist:**
- [ ] Adicionar SignalR services
- [ ] Configurar keep-alive intervals
- [ ] Mapear Hub endpoint
- [ ] CORS para SignalR

**Critérios de aceito:**
- ✅ WebSocket endpoint acessível
- ✅ Clients podem conectar

---

#### 2.3.3 Integrar Hub com DownloadQueue
**Estimativa:** 4 horas
**Arquivo:** `src/Cutube.Api/Services/BackgroundDownloadWorker.cs`

**Implementação:**
```csharp
public class BackgroundDownloadWorker : BackgroundService
{
    private readonly IDownloadQueue _queue;
    private readonly IDownloadService _downloadService;
    private readonly IHubContext<DownloadHub, IDownloadHubClient> _hub;

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await foreach (var (downloadId, request) in _queue.DequeueAllAsync(ct))
        {
            // Notify started
            await _hub.Clients.Group($"download:{downloadId}")
                .DownloadStarted(downloadId, new DownloadStartedEvent(downloadId, request.Url, DateTime.UtcNow));

            var progress = new Progress<DownloadProgress>(async p =>
            {
                await _hub.Clients.Group($"download:{downloadId}")
                    .DownloadProgress(downloadId, new DownloadProgressEvent(...));
            });

            var result = await _downloadService.DownloadAsync(request, progress, ct);

            if (result.IsFailure)
            {
                await _hub.Clients.Group($"download:{downloadId}")
                    .DownloadFailed(downloadId, new DownloadFailedEvent(downloadId, result.Error, DateTime.UtcNow));
            }
            else
            {
                await _hub.Clients.Group($"download:{downloadId}")
                    .DownloadCompleted(downloadId, new DownloadCompletedEvent(downloadId, result.Value.FilePath, ...));
            }
        }
    }
}
```

**Checklist:**
- [ ] Injetar IHubContext no Worker
- [ ] Enviar eventos de progresso
- [ ] Enviar eventos de conclusão/erro
- [ ] Testar com múltiplos clients

**Critérios de aceito:**
- ✅ Progresso atualizado em tempo real
- ✅ Múltiplos clients recebem updates

---

#### 2.3.4 Connection Lifecycle Management
**Estimativa:** 3 horas
**Arquivos:**
```
Cutube.Api/Hubs/
  ├── DownloadHub.cs
  └── ConnectionTracker.cs
```

**Implementação:**
```csharp
public class ConnectionTracker
{
    private readonly ConcurrentDictionary<string, HashSet<string>> _connections = new();

    public void AddConnection(string downloadId, string connectionId)
    {
        _connections.AddOrUpdate(downloadId,
            _ => new HashSet<string> { connectionId },
            (_, set) => { set.Add(connectionId); return set; });
    }

    public void RemoveConnection(string downloadId, string connectionId)
    {
        if (_connections.TryGetValue(downloadId, out var set))
        {
            set.Remove(connectionId);
            if (set.Count == 0)
                _connections.TryRemove(downloadId, out _);
        }
    }

    public bool HasActiveConnections(string downloadId)
    {
        return _connections.TryGetValue(downloadId, out var set) && set.Count > 0;
    }
}
```

**Checklist:**
- [ ] Rastrear conexões ativas
- [ ] OnConnect/OnDisconnect handlers
- [ ] Cleanup de grupos vazios
- [ ] Logging de conexões

**Critérios de aceito:**
- ✅ Conexões rastreadas corretamente
- ✅ Groups limpos após disconnect

---

#### 2.3.5 Testar WebSocket com cliente SignalR
**Estimativa:** 3 horas
**Arquivo:** `Cutube.Api.Tests/SignalR/SignalRTests.cs`

**Checklist:**
- [ ] Criar teste de integração SignalR
- [ ] Testar connection
- [ ] Testar envio de mensagens
- [ ] Testar reconnection
- [ ] Testar múltiplos clients

**Critérios de aceito:**
- ✅ SignalR funciona end-to-end
- ✅ Latência < 500ms

---

### Qualidade Gates - Fase 2.3

Antes de passar para Fase 2.4:

- [ ] SignalR funcionando
- [ ] Progresso em tempo real OK
- [ ] Múltiplos clients conectados
- [ ] Reconnection automático OK

---

## Fase 2.4: Frontend Next.js/React + TypeScript

**Duração:** 7-10 dias
**Responsável:** Frontend Developer
**Prioridade:** 🔥 Alta

### Objetivo
Criar interface web moderna usando Next.js 15, React e TypeScript que se comunica com a API REST e SignalR.

### Tarefas

#### 2.4.1 Criar projeto Next.js 15
**Estimativa:** 2 horas
**Comando:**
```bash
npx create-next-app@latest cutube-web \
  --typescript \
  --tailwind \
  --app \
  --no-src-dir \
  --import-alias "@/*"
```

**Checklist:**
- [ ] Criar projeto Next.js 15
- [ ] Configurar TypeScript strict mode
- [ ] Configurar TailwindCSS
- [ ] Configurar path aliases (@/*)
- [ ] Adicionar ESLint + Prettier
- [ ] Build OK

**Critérios de aceito:**
- ✅ Projeto cria com `npm run dev`
- ✅ Tailwind funcionando
- ✅ TypeScript sem erros

---

#### 2.4.2 Setup shadcn/ui components
**Estimativa:** 3 horas
**Comandos:**
```bash
npx shadcn@latest init
npx shadcn@latest add button card input form label select badge progress toast
```

**Checklist:**
- [ ] Configurar shadcn/ui
- [ ] Adicionar componentes base
- [ ] Configurar tema (dark mode)
- [ ] Testar componentes

**Critérios de aceito:**
- ✅ Componentes funcionando
- ✅ Dark mode configurado

---

#### 2.4.3 Criar tipos TypeScript
**Estimativa:** 2 horas
**Arquivos:**
```
cutube-web/types/
  ├── download.ts
  ├── video.ts
  └── api.ts
```

**Tipos:**
```typescript
// types/download.ts
export interface DownloadRequest {
  url: string;
  outputPath?: string;
  startTime?: string;
  endTime?: string;
  audioOnly?: boolean;
  customFilename?: string;
}

export interface DownloadProgress {
  downloadId: string;
  progress: number;
  speed: number;
  eta?: string;
  downloadedBytes: number;
  totalBytes: number;
  status: DownloadStatus;
}

export enum DownloadStatus {
  Queued = 'queued',
  Downloading = 'downloading',
  Processing = 'processing',
  Completed = 'completed',
  Failed = 'failed',
  Cancelled = 'cancelled',
}

export interface DownloadSummary {
  downloadId: string;
  url: string;
  status: DownloadStatus;
  progress: number;
  filePath?: string;
  createdAt: Date;
}

// types/video.ts
export interface VideoMetadata {
  id: string;
  title: string;
  uploader: string;
  duration: string;
  thumbnailUrl: string;
  viewCount?: number;
  formats: VideoFormat[];
}

export interface VideoFormat {
  formatId: string;
  extension: string;
  resolution?: string;
  fileSize?: number;
}
```

**Checklist:**
- [ ] Criar todos tipos
- [ ] Tipar resposta da API
- [ ] Exportar tipos

**Critérios de aceito:**
- ✅ Tipos compilam sem erros
- ✅ Tipagem correta da API

---

#### 2.4.4 Criar API client (REST)
**Estimativa:** 4 horas
**Arquivo:** `cutube-web/lib/api.ts`

**Implementação:**
```typescript
// lib/api.ts
import { DownloadRequest, DownloadSummary } from '@/types/download';
import { VideoMetadata } from '@/types/video';

const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000';

export const api = {
  downloads: {
    create: async (request: DownloadRequest): Promise<string> => {
      const response = await fetch(`${API_BASE_URL}/api/downloads`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(request),
      });
      if (!response.ok) throw new Error('Failed to create download');
      const data = await response.json();
      return data.downloadId;
    },

    list: async (): Promise<DownloadSummary[]> => {
      const response = await fetch(`${API_BASE_URL}/api/downloads`);
      if (!response.ok) throw new Error('Failed to fetch downloads');
      const data = await response.json();
      return data.downloads;
    },

    get: async (id: string): Promise<DownloadSummary> => {
      const response = await fetch(`${API_BASE_URL}/api/downloads/${id}`);
      if (!response.ok) throw new Error('Download not found');
      return response.json();
    },

    cancel: async (id: string): Promise<void> => {
      await fetch(`${API_BASE_URL}/api/downloads/${id}`, {
        method: 'DELETE',
      });
    },
  },

  videos: {
    getInfo: async (url: string): Promise<VideoMetadata> => {
      const response = await fetch(`${API_BASE_URL}/api/videos/info?url=${encodeURIComponent(url)}`);
      if (!response.ok) throw new Error('Failed to fetch video info');
      return response.json();
    },
  },
};
```

**Checklist:**
- [ ] Criar client REST
- [ ] Configurar fetch
- [ ] Error handling
- [ ] TypeScript types

**Critérios de aceito:**
- ✅ API client funcional
- ✅ Error handling OK

---

#### 2.4.5 Criar SignalR client
**Estimativa:** 4 horas
**Arquivo:** `cutube-web/lib/websocket.ts`

**Implementação:**
```typescript
// lib/websocket.ts
import * as signalR from '@microsoft/signalr';
import { DownloadProgress } from '@/types/download';

type DownloadEventListener = (data: DownloadProgress) => void;

class WebSocketService {
  private connection: signalR.HubConnection | null = null;
  private listeners: Map<string, DownloadEventListener[]> = new Map();

  connect(): void {
    const apiUrl = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5000';
    const hubUrl = apiUrl.replace('http', 'ws') + '/hubs/downloads';

    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(hubUrl)
      .withAutomaticReconnect([0, 2000, 10000, 30000])
      .build();

    this.connection.on('DownloadProgress', (downloadId, data) => {
      this.emit(`progress:${downloadId}`, data);
    });

    this.connection.on('DownloadCompleted', (downloadId, data) => {
      this.emit(`completed:${downloadId}`, data);
    });

    this.connection.on('DownloadFailed', (downloadId, data) => {
      this.emit(`failed:${downloadId}`, data);
    });

    this.connection.start().catch(err => console.error('SignalR connection error:', err));
  }

  joinDownload(downloadId: string): void {
    this.connection?.invoke('JoinDownloadGroup', downloadId);
  }

  leaveDownload(downloadId: string): void {
    this.connection?.invoke('LeaveDownloadGroup', downloadId);
  }

  on(downloadId: string, event: string, listener: DownloadEventListener): void {
    const key = `${event}:${downloadId}`;
    if (!this.listeners.has(key)) {
      this.listeners.set(key, []);
    }
    this.listeners.get(key)!.push(listener);
  }

  off(downloadId: string, event: string, listener: DownloadEventListener): void {
    const key = `${event}:${downloadId}`;
    const listeners = this.listeners.get(key);
    if (listeners) {
      const index = listeners.indexOf(listener);
      if (index > -1) {
        listeners.splice(index, 1);
      }
    }
  }

  private emit(key: string, data: any): void {
    const listeners = this.listeners.get(key);
    if (listeners) {
      listeners.forEach(listener => listener(data));
    }
  }
}

export const ws = new WebSocketService();
```

**Checklist:**
- [ ] Instalar @microsoft/signalr
- [ ] Criar WebSocketService
- [ ] Implementar connection
- [ ] Implementar events
- [ ] Auto-reconnect
- [ ] Testar com API

**Critérios de aceito:**
- ✅ SignalR conecta
- ✅ Events recebidos
- ✅ Reconnect funciona

---

#### 2.4.6 Criar página: Dashboard
**Estimativa:** 6 horas
**Arquivo:** `cutube-web/app/page.tsx`

**Componentes:**
```
cutube-web/app/page.tsx
cutube-web/components/
  ├── DownloadForm.tsx
  ├── DownloadsList.tsx
  ├── DownloadCard.tsx
  └── ProgressBar.tsx
```

**Funcionalidades:**
- Form para iniciar download
- Lista de downloads ativos
- Cards com progresso
- Status badges

**Checklist:**
- [ ] Criar layout Dashboard
- [ ] Criar DownloadForm
- [ ] Criar DownloadsList
- [ ] Criar DownloadCard
- [ ] Criar ProgressBar
- [ ] Conectar API
- [ ] Conectar SignalR

**Critérios de aceito:**
- ✅ Dashboard funcional
- ✅ Downloads listados
- ✅ Progresso em tempo real

---

#### 2.4.7 Criar página: Downloads
**Estimativa:** 4 horas
**Arquivos:**
```
cutube-web/app/downloads/page.tsx
cutube-web/app/downloads/[id]/page.tsx
```

**Funcionalidades:**
- Lista todos downloads (ativos e completados)
- Detalhes de download específico
- Actions: pause, cancel, retry

**Checklist:**
- [ ] Criar página /downloads
- [ ] Criar página /downloads/[id]
- [ ] Implementar filtros
- [ ] Implementar ações

**Critérios de aceito:**
- ✅ Páginas funcionais
- ✅ Navegação OK

---

#### 2.4.8 Criar componentes de UI
**Estimativa:** 6 horas
**Componentes:**
```
cutube-web/components/
  ├── DownloadForm.tsx       - Form com URL, timerange, opções
  ├── DownloadCard.tsx       - Card com info do download
  ├── ProgressBar.tsx        - Barra de progresso animada
  ├── StatusBadge.tsx        - Badge colorido por status
  ├── VideoPreview.tsx       - Preview com thumbnail, metadados
  ├── TimeRangePicker.tsx    - Input para start/end time
  └── DownloadActions.tsx    - Botões pause, cancel, retry
```

**Checklist:**
- [ ] Criar DownloadForm com validações
- [ ] Criar DownloadCard com progresso
- [ ] Criar ProgressBar animada
- [ ] Criar StatusBadge (cores por status)
- [ ] Criar VideoPreview (thumbnail + info)
- [ ] Criar TimeRangePicker (input formatado)
- [ ] Criar DownloadActions (botões)

**Critérios de aceito:**
- ✅ Todos componentes funcionais
- ✅ Acessibilidade OK

---

#### 2.4.9 Dark mode
**Estimativa:** 3 horas
**Arquivos:**
```
cutube-web/components/theme-provider.tsx
cutube-web/components/theme-toggle.tsx
```

**Checklist:**
- [ ] Criar ThemeProvider (next-themes)
- [ ] Criar ThemeToggle button
- [ ] Aplicar dark mode em todos componentes
- [ ] Persistir preferência

**Critérios de aceito:**
- ✅ Dark mode funciona
- ✅ Persistência OK

---

#### 2.4.10 Responsividade
**Estimativa:** 4 horas
**Checklist:**
- [ ] Mobile (320px+)
- [ ] Tablet (768px+)
- [ ] Desktop (1024px+)
- [ ] Testar em múltiplos devices
- [ ] Tailwind breakpoints

**Critérios de aceito:**
- ✅ Responsivo em todos tamanhos
- ✅ Lighthouse > 90

---

#### 2.4.11 Testes E2E com Playwright
**Estimativa:** 6 horas
**Arquivos:**
```
cutube-web/e2e/
  ├── downloads.spec.ts
  └── video-info.spec.ts
```

**Cenários:**
- Criar download
- Ver progresso em tempo real
- Cancelar download
- Buscar metadados de vídeo

**Checklist:**
- [ ] Configurar Playwright
- [ ] Criar testes E2E
- [ ] Testar fluxos principais
- [ ] Testar WebSocket

**Critérios de aceito:**
- ✅ Testes passam
- ✅ Cobertura de fluxos principais

---

### Qualidade Gates - Fase 2.4

Antes de passar para Fase 2.5:

- [ ] `npm run build` - **ZERO errors**
- [ ] `npm test` - **100% pass**
- [ ] Lighthouse > 90
- [ ] Responsivo
- [ ] Acessibilidade WCAG AA

---

## Fase 2.5: Integração CLI ↔ API

**Duração:** 2-3 dias
**Responsável:** Backend Developer
**Prioridade:** 🔥 Alta

### Objetivo
Permitir que CLI funcione tanto standalone quanto usando API remota, com configuração flexível.

### Tarefas

#### 2.5.1 Criar sistema de configuração
**Estimativa:** 3 horas
**Arquivos:**
```
cutube/Configuration/
  ├── AppConfig.cs
  └── ConfigService.cs
```

**Implementação:**
```csharp
// AppConfig.cs
public class AppConfig
{
    public string? ApiUrl { get; set; }
    public bool UseApi => !string.IsNullOrEmpty(ApiUrl);
    public string DefaultOutputPath { get; set; } = "~/Downloads";
    public int MaxConcurrentDownloads { get; set; } = 3;
}

// ConfigService.cs
public class ConfigService
{
    private static readonly string ConfigPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserConfig),
        "cutube",
        "config.json"
    );

    public async Task<AppConfig> LoadAsync()
    {
        if (!File.Exists(ConfigPath))
            return new AppConfig();

        var json = await File.ReadAllTextAsync(ConfigPath);
        return JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
    }

    public async Task SaveAsync(AppConfig config)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath)!);
        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        await File.WriteAllTextAsync(ConfigPath, json);
    }
}
```

**Checklist:**
- [ ] Criar AppConfig
- [ ] Criar ConfigService
- [ ] Salvar em ~/.config/cutube/config.json
- [ ] Testar load/save

**Critérios de aceito:**
- ✅ Config salva/carrega corretamente

---

#### 2.5.2 Criar ApiClient para CLI
**Estimativa:** 4 horas
**Arquivo:** `cutube/Services/ApiClient.cs`

**Implementação:**
```csharp
public class ApiClient : IDownloadService
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;

    public ApiClient(string baseUrl)
    {
        _httpClient = new HttpClient();
        _baseUrl = baseUrl;
    }

    public async Task<Result<DownloadResult>> DownloadAsync(
        DownloadRequest request,
        IProgress<DownloadProgress> progress,
        CancellationToken ct = default)
    {
        // Criar download via API
        var response = await _httpClient.PostAsJsonAsync($"{_baseUrl}/api/downloads", request, ct);
        response.EnsureSuccessStatusCode();

        var data = await response.Content.ReadFromJsonAsync<CreateDownloadResponse>(ct);

        // Poll progress
        while (!ct.IsCancellationRequested)
        {
            await Task.Delay(1000, ct);

            var statusResponse = await _httpClient.GetAsync($"{_baseUrl}/api/downloads/{data!.DownloadId}", ct);
            var status = await statusResponse.Content.ReadFromJsonAsync<DownloadDetails>(ct);

            progress.Report(new DownloadProgress(...));

            if (status!.Status is DownloadStatus.Completed or DownloadStatus.Failed or DownloadStatus.Cancelled)
                break;
        }

        return Result.Success(...);
    }
}
```

**Checklist:**
- [ ] Criar ApiClient
- [ ] Implementar IDownloadService
- [ ] Poll de progresso
- [ ] Error handling

**Critérios de aceito:**
- ✅ CLI pode usar API remota

---

#### 2.5.3 Modificar Program.cs para suportar modos
**Estimativa:** 3 horas
**Arquivo:** `cutube/Program.cs`

**Implementação:**
```csharp
// Adicionar flag --api-url
var apiUrlOption = new Option<string?>("--api-url", "API URL para modo remoto");
var rootCommand = new RootCommand();
rootCommand.AddOption(apiUrlOption);

rootCommand.SetHandler(async (string? apiUrl) =>
{
    var config = await configService.LoadAsync();

    // Determinar modo
    var useApi = apiUrl ?? config.ApiUrl;

    IDownloadService downloadService;

    if (useApi is not null)
    {
        // Modo API
        downloadService = new ApiClient(useApi);
    }
    else
    {
        // Modo local
        downloadService = new DownloadService(
            new YtDlpDownloader(...),
            new FfmpegProcessor(...),
            new ValidationService(),
            new FileSystem()
        );
    }

    var workflow = new ProgramWorkflow(downloadService, progress);
    await workflow.ExecuteAsync(...);

}, apiUrlOption);
```

**Checklist:**
- [ ] Adicionar flag --api-url
- [ ] Detectar modo (local vs remoto)
- [ ] Injetar dependências correto
- [ ] Testar ambos modos

**Critérios de aceito:**
- ✅ `cutube` funciona local
- ✅ `cutube --api-url http://localhost:5000` usa API

---

#### 2.5.4 Testar integração CLI ↔ API
**Estimativa:** 3 horas
**Cenários:**
- CLI local (sem API)
- CLI remota (com API)
- CLI com config file
- Error handling (API offline)

**Checklist:**
- [ ] Testar CLI local
- [ ] Testar CLI com API
- [ ] Testar config file
- [ ] Testar API offline
- [ ] Testar timeouts

**Critérios de aceito:**
- ✅ Ambos modos funcionam
- ✅ Error handling OK

---

### Qualidade Gates - Fase 2.5

- [ ] `dotnet build` - **ZERO warnings**
- [ ] `dotnet test` - **100% pass**
- [ ] CLI local funciona
- [ ] CLI com API funciona
- [ ] Config file OK

---

## Qualidade Gates Finais - Épico 2

Antes de considerar Épico 2 completo:

### Backend
- [ ] `dotnet build` (todos projetos) - **ZERO warnings**
- [ ] `dotnet test` (todos projetos) - **100% pass**
- [ ] Swagger UI acessível
- [ ] SignalR funcionando
- [ ] Health checks OK

### Frontend
- [ ] `npm run build` - **ZERO errors**
- [ ] `npm test` - **100% pass**
- [ ] Lighthouse score > 90
- [ ] Responsivo (mobile, tablet, desktop)
- [ ] Acessibilidade WCAG AA

### Integração
- [ ] CLI standalone funciona
- [ ] CLI ↔ API funciona
- [ ] Web ↔ API funciona
- [ ] WebSocket em tempo real

### Documentação
- [ ] README atualizado
- [ ] Swagger documentado
- [ ] Code review aprovado

---

## Cronograma

| Fase | Tarefas | Dias | Dependências |
|------|---------|------|--------------|
| **2.1** Domain Layer | 7 tarefas | 4-5 | ✅ Épico 1 |
| **2.2** REST API | 10 tarefas | 5-6 | ✅ Fase 2.1 |
| **2.3** WebSocket | 5 tarefas | 3-4 | ✅ Fase 2.2 |
| **2.4** Frontend | 11 tarefas | 7-10 | ✅ Fase 2.3 |
| **2.5** Integração CLI | 4 tarefas | 2-3 | ✅ Fase 2.4 |

**Total:** 21-28 dias

---

## Tecnologias

### Backend (.NET 10)
- **ASP.NET Core Minimal API** - REST
- **SignalR** - WebSocket
- **FluentResults** - Result pattern
- **Swashbuckle** - Swagger/OpenAPI

### Frontend (Next.js)
- **Next.js 15** - App Router
- **TypeScript** - Type safety
- **TailwindCSS** - Styling
- **shadcn/ui** - Components
- **@microsoft/signalr** - WebSocket client
- **React Query** - Cache (opcional)

### DevOps
- **Docker** (Fase 3)
- **GitHub Actions** (CI/CD)

---

## Próximos Passos

Após completar Épico 2:

1. ✅ **Épico 3**: Integração com RabbitMQ
   - Producer (API → Fila)
   - Consumer (Worker)
   - DLQ e Retry
   - Dashboard de monitoramento

2. 🎯 **Deploy em staging**
   - Docker compose
   - Reverse proxy (Nginx)

3. 📈 **Performance testing**
   - Load testing da API
   - Stress testing do WebSocket
   - Benchmark de downloads

---

**Status do Épico 2:** 🎯 Planejamento completo, aguardando início
**Próxima ação:** Iniciar Fase 2.1 (Domain Layer)
