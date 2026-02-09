# Fase 2.2: REST API

**Status:** 🎯 Planejamento
**Épico:** Cutube-2i6 (Épico 2: Arquitetura Híbrida CLI + API)
**Duração:** 5-6 dias
**Responsável:** Backend Developer
**Prioridade:** 🔥 Alta
**Dependência:** ✅ Fase 2.1 completa (Domain Layer)

---

## Objetivo

Criar uma API REST em ASP.NET Core Minimal API que expõe as funcionalidades do domínio via HTTP, permitindo integração com Web UI (Next.js) e preparando para sistema distribuído.

**Benefícios:**
- Interface HTTP padronizada para acesso ao Domain
- Suporte a operações assíncronas (background processing)
- Documentação automática com Swagger/OpenAPI
- Health checks para monitoramento
- CORS configurado para Next.js

---

## Visão Arquitetural

```
┌──────────────────────────────────────────────────────────┐
│               Presentation Layer (API)                   │
│           ASP.NET Core Minimal API                        │
│                                                           │
│  /api/downloads        POST, GET, DELETE                 │
│  /api/videos/info      GET                               │
│  /swagger              OpenAPI documentation              │
│  /health               Health checks                     │
└──────────────────────────┬───────────────────────────────┘
                           │ Domain calls
                           ▼
┌──────────────────────────────────────────────────────────┐
│              Application Layer (API Services)            │
│                                                           │
│  - DownloadQueue (Channel)                                │
│  - BackgroundDownloadWorker (HostedService)              │
│  - InMemoryStatusRepository                               │
│  - ApiEndpoints (Minimal API)                             │
└──────────────────────────┬───────────────────────────────┘
                           │ Domain calls
                           ▼
┌──────────────────────────────────────────────────────────┐
│                  Domain Layer                            │
│              (Cutube.Domain)                             │
│                                                           │
│  - IDownloadService                                       │
│  - IMetadataService                                       │
│  - IValidationService                                     │
│  - Models (DownloadRequest, VideoMetadata, etc)          │
└──────────────────────────────────────────────────────────┘
```

---

## Tarefas

### 2.2.1 Criar projeto Cutube.Api

**Estimativa:** 2 horas
**Arquivos:**
- `src/Cutube.Api/Cutube.Api.csproj`
- `src/Cutube.Api/Program.cs`
- `src/Cutube.Api/appsettings.json`
- `src/Cutube.Api/Properties/launchSettings.json`

**Comandos:**
```bash
# Criar projeto API
dotnet new web -n Cutube.Api -o src/Cutube.Api -f net10.0

# Adicionar ao solution
dotnet sln cutube.sln add src/Cutube.Api/Cutube.Api.csproj

# Adicionar referência ao Domain
dotnet add src/Cutube.Api/Cutube.Api.csproj reference src/Cutube.Domain/Cutube.Domain.csproj

# Adicionar pacotes
dotnet add src/Cutube.Api/Cutube.Api.csproj package Swashbuckle.AspNetCore
dotnet add src/Cutube.Api/Cutube.Api.csproj package Microsoft.AspNetCore.SignalR
```

**Configuração do .csproj:**
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

**launchSettings.json:**
```json
{
  "$schema": "http://json.schemastore.org/launchsettings.json",
  "profiles": {
    "http": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": true,
      "launchUrl": "swagger",
      "applicationUrl": "http://localhost:5000",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    },
    "https": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": true,
      "launchUrl": "swagger",
      "applicationUrl": "https://localhost:5001;http://localhost:5000",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
```

**Program.cs básico:**
```csharp
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Services
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Cutube API",
        Version = "v1",
        Description = "REST API for video downloads"
    });
});

builder.Services.AddCors();
builder.Services.AddHostedService<BackgroundDownloadWorker>();

// Register Domain Services (será configurado nas tarefas seguintes)
// builder.Services.AddSingleton<IDownloadService, DownloadService>();
// ...

var app = builder.Build();

// Pipeline
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Cutube API v1");
});

app.UseCors();
app.UseHttpsRedirection();

// Endpoints (serão adicionados nas tarefas seguintes)
// app.MapGet("/", () => "Cutube API");

app.Run();
```

**Checklist:**
- [ ] Criar projeto ASP.NET Core Minimal API
- [ ] Adicionar ao solution
- [ ] Configurar launchSettings.json (portas 5000/5001)
- [ ] Adicionar referência a Cutube.Domain
- [ ] Adicionar pacotes (Swashbuckle, SignalR)
- [ ] Build sem warnings
- [ ] Executar `dotnet run` e acessar http://localhost:5000/swagger

**Critérios de aceito:**
- ✅ Projeto cria com `dotnet run`
- ✅ Swagger UI acessível em /swagger
- ✅ CORS configurado (básico)
- ✅ Sem erros de build

---

### 2.2.2 Implementar endpoint: POST /api/downloads

**Estimativa:** 4 horas
**Arquivo:** `src/Cutube.Api/Endpoints/DownloadsEndpoints.cs`

**Contrato da API:**

**Request:**
```http
POST /api/downloads
Content-Type: application/json

{
  "url": "https://youtube.com/watch?v=example",
  "outputPath": "/path/to/downloads",
  "startTime": "00:01:00",  // opcional
  "endTime": "00:02:00",    // opcional
  "audioOnly": false,       // opcional
  "customFilename": "my-video.mp4"  // opcional
}
```

**Response (202 Accepted):**
```json
{
  "downloadId": "abc123",
  "status": "queued",
  "message": "Download enqueued successfully"
}
```

**Response (400 Bad Request):**
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "url": ["URL is required"]
  }
}
```

**Implementação:**

```csharp
// src/Cutube.Api/DTOs/CreateDownloadRequest.cs
namespace Cutube.Api.DTOs;

public record CreateDownloadRequest(
    string Url,
    string? OutputPath,
    string? StartTime,
    string? EndTime,
    bool AudioOnly = false,
    string? CustomFilename = null
);

// src/Cutube.Api/DTOs/CreateDownloadResponse.cs
namespace Cutube.Api.DTOs;

public record CreateDownloadResponse(
    string DownloadId,
    string Status,
    string? Message = null
);

// src/Cutube.Api/Endpoints/DownloadsEndpoints.cs
using Cutube.Api.DTOs;
using Cutube.Domain.Models;
using Cutube.Domain.Services;
using FluentResults;
using Microsoft.AspNetCore.Mvc;

namespace Cutube.Api.Endpoints;

public static class DownloadsEndpoints
{
    public static void MapDownloadsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/downloads")
            .WithTags("Downloads")
            .WithOpenApi();

        group.MapPost("/", async (
            [FromBody] CreateDownloadRequest request,
            IDownloadService downloadService,
            IDownloadQueue downloadQueue,
            CancellationToken ct) =>
        {
            // 1. Validar request
            var validationResult = ValidateRequest(request);
            if (validationResult.IsFailure)
            {
                return Results.Problem(
                    detail: validationResult.Errors.First().Message,
                    statusCode: 400,
                    title: "Validation Error");
            }

            // 2. Converter para Domain Request
            var domainRequest = MapToDomainRequest(request);

            // 3. Enfileirar (processamento assíncrono em background)
            var downloadId = await downloadQueue.EnqueueAsync(domainRequest, ct);

            // 4. Retornar imediatamente (202 Accepted)
            var response = new CreateDownloadResponse(
                downloadId,
                "queued",
                "Download enqueued successfully"
            );

            return Results.Accepted($"/api/downloads/{downloadId}", response);
        })
        .WithName("CreateDownload")
        .WithOpenApi()
        .Produces<CreateDownloadResponse>(202)
        .Produces<ProblemDetails>(400);

        // Outros endpoints serão adicionados nas próximas tarefas
    }

    private static Result ValidateRequest(CreateDownloadRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Url))
            return Result.Fail("URL is required");

        // Validar URL format
        if (!Uri.TryCreate(request.Url, UriKind.Absolute, out _))
            return Result.Fail("Invalid URL format");

        // Validar TimeRange se presente
        if (!string.IsNullOrEmpty(request.StartTime) || !string.IsNullOrEmpty(request.EndTime))
        {
            if (!TimeSpan.TryParse(request.StartTime, out var start))
                return Result.Fail("Invalid StartTime format (expected HH:MM:SS)");

            if (!TimeSpan.TryParse(request.EndTime, out var end))
                return Result.Fail("Invalid EndTime format (expected HH:MM:SS)");

            if (start >= end)
                return Result.Fail("StartTime must be less than EndTime");
        }

        return Result.Ok();
    }

    private static DownloadRequest MapToDomainRequest(CreateDownloadRequest apiRequest)
    {
        TimeRange? timeRange = null;

        if (!string.IsNullOrEmpty(apiRequest.StartTime) && !string.IsNullOrEmpty(apiRequest.EndTime))
        {
            timeRange = new TimeRange
            {
                Start = TimeSpan.Parse(apiRequest.StartTime),
                End = TimeSpan.Parse(apiRequest.EndTime)
            };
        }

        return new DownloadRequest
        {
            Url = apiRequest.Url,
            OutputPath = apiRequest.OutputPath,
            TimeRange = timeRange,
            AudioOnly = apiRequest.AudioOnly,
            CustomFilename = apiRequest.CustomFilename
        };
    }
}
```

**Interfaces da API (será implementado na tarefa 2.2.9):**

```csharp
// src/Cutube.Api/Services/IDownloadQueue.cs
using Cutube.Domain.Models;

namespace Cutube.Api.Services;

public interface IDownloadQueue
{
    Task<string> EnqueueAsync(DownloadRequest request, CancellationToken ct);
}
```

**Checklist:**
- [ ] Criar DTOs (CreateDownloadRequest, CreateDownloadResponse)
- [ ] Criar endpoint POST /api/downloads
- [ ] Validar request (URL, timerange)
- [ ] Enfileirar download (não bloquear)
- [ ] Retornar 202 Accepted com downloadId
- [ ] Documentar com Swagger/OpenAPI
- [ ] Testar com curl/Postman

**Critérios de aceito:**
- ✅ Endpoint responde em < 100ms (enfileira apenas)
- ✅ Download enfileirado corretamente
- ✅ Swagger UI mostra endpoint com exemplo
- ✅ Validação de request funcionando

---

### 2.2.3 Implementar endpoint: GET /api/downloads

**Estimativa:** 3 horas

**Contrato da API:**

**Response (200 OK):**
```json
{
  "downloads": [
    {
      "downloadId": "abc123",
      "url": "https://youtube.com/watch?v=example",
      "status": "downloading",
      "progress": 45.5,
      "filePath": null,
      "createdAt": "2026-02-09T10:30:00Z"
    },
    {
      "downloadId": "def456",
      "url": "https://youtube.com/watch?v=example2",
      "status": "completed",
      "progress": 100.0,
      "filePath": "/path/to/video.mp4",
      "createdAt": "2026-02-09T09:00:00Z"
    }
  ],
  "totalCount": 2
}
```

**Query Parameters (opcional):**
```
?status=downloading     // Filtrar por status
?limit=10              // Paginação
?offset=0              // Offset para paginação
```

**Implementação:**

```csharp
// src/Cutube.Api/DTOs/DownloadSummary.cs
namespace Cutube.Api.DTOs;

public record DownloadSummary(
    string DownloadId,
    string Url,
    string Status,
    double Progress,
    string? FilePath,
    DateTime CreatedAt
);

public record GetDownloadsResponse(
    DownloadSummary[] Downloads,
    int TotalCount
);

// src/Cutube.Api/Endpoints/DownloadsEndpoints.cs (adicionar ao group)
group.MapGet("/", async (
    [FromQuery] string? status,
    [FromQuery] int? limit,
    [FromQuery] int? offset,
    IDownloadStatusRepository statusRepository,
    CancellationToken ct) =>
{
    // 1. Buscar todos downloads
    var allDownloads = await statusRepository.GetAllAsync(ct);

    // 2. Filtrar por status (se fornecido)
    if (!string.IsNullOrEmpty(status))
    {
        allDownloads = allDownloads
            .Where(d => d.Status.ToString().Equals(status, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    // 3. Paginação (se fornecido)
    var totalCount = allDownloads.Count;
    if (offset.HasValue)
        allDownloads = allDownloads.Skip(offset.Value).ToList();
    if (limit.HasValue)
        allDownloads = allDownloads.Take(limit.Value).ToList();

    // 4. Converter para DTOs
    var summaries = allDownloads.Select(d => new DownloadSummary(
        d.Id,
        d.Url,
        d.Status.ToString().ToLowerInvariant(),
        d.Progress,
        d.FilePath,
        d.CreatedAt
    )).ToArray();

    var response = new GetDownloadsResponse(summaries, totalCount);

    return Results.Ok(response);
})
.WithName("GetDownloads")
.WithOpenApi()
.Produces<GetDownloadsResponse>(200);
```

**Interface (será implementado na tarefa 2.2.9):**

```csharp
// src/Cutube.Api/Services/IDownloadStatusRepository.cs
using Cutube.Domain.Models;

namespace Cutube.Api.Services;

public interface IDownloadStatusRepository
{
    Task<IEnumerable<DownloadStatus>> GetAllAsync(CancellationToken ct);
    Task<DownloadStatus?> GetByIdAsync(string id, CancellationToken ct);
    Task UpdateAsync(string id, DownloadProgress progress);
}
```

**Checklist:**
- [ ] Criar DTOs (DownloadSummary, GetDownloadsResponse)
- [ ] Criar endpoint GET /api/downloads
- [ ] Retornar lista de downloads
- [ ] Suportar filtros (status)
- [ ] Suportar paginação (limit, offset)
- [ ] Documentar no Swagger

**Critérios de aceito:**
- ✅ Lista todos downloads
- ✅ Filtra por status (opcional)
- ✅ Paginação funcional (opcional)
- ✅ Performance OK (< 100ms para 1000 downloads)

---

### 2.2.4 Implementar endpoint: GET /api/downloads/{id}

**Estimativa:** 3 horas

**Contrato da API:**

**Response (200 OK):**
```json
{
  "downloadId": "abc123",
  "url": "https://youtube.com/watch?v=example",
  "status": "downloading",
  "progress": 45.5,
  "speed": 1048576.0,
  "eta": "00:02:30",
  "downloadedBytes": 52428800,
  "totalBytes": 115343360,
  "filePath": null,
  "errorMessage": null,
  "createdAt": "2026-02-09T10:30:00Z",
  "completedAt": null
}
```

**Response (404 Not Found):**
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.4",
  "title": "Not Found",
  "status": 404,
  "detail": "Download with ID 'abc123' not found"
}
```

**Implementação:**

```csharp
// src/Cutube.Api/DTOs/DownloadDetails.cs
namespace Cutube.Api.DTOs;

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

// src/Cutube.Api/Endpoints/DownloadsEndpoints.cs (adicionar ao group)
group.MapGet("/{id}", async (
    string id,
    IDownloadStatusRepository statusRepository,
    CancellationToken ct) =>
{
    // 1. Buscar download por ID
    var download = await statusRepository.GetByIdAsync(id, ct);

    if (download is null)
    {
        return Results.Problem(
            detail: $"Download with ID '{id}' not found",
            statusCode: 404,
            title: "Not Found");
    }

    // 2. Converter para DTO
    var details = new DownloadDetails(
        download.Id,
        download.Url,
        download.Status.ToString().ToLowerInvariant(),
        download.Progress,
        download.Speed,
        download.Eta?.ToString("hh\:mm\:ss"),
        download.DownloadedBytes,
        download.TotalBytes,
        download.FilePath,
        download.ErrorMessage,
        download.CreatedAt,
        download.CompletedAt
    );

    return Results.Ok(details);
})
.WithName("GetDownloadById")
.WithOpenApi()
.Produces<DownloadDetails>(200)
.Produces<ProblemDetails>(404);
```

**Checklist:**
- [ ] Criar DTO DownloadDetails
- [ ] Criar endpoint GET /api/downloads/{id}
- [ ] Retornar 404 se não encontrado
- [ ] Incluir todos detalhes de progresso
- [ ] Documentar no Swagger

**Critérios de aceito:**
- ✅ Detalhes completos do download
- ✅ 404 para ID inválido
- ✅ Performance OK (< 50ms)

---

### 2.2.5 Implementar endpoint: DELETE /api/downloads/{id}

**Estimativa:** 2 horas

**Contrato da API:**

**Response (204 No Content):**
```
(Nenhum body)
```

**Response (404 Not Found):**
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.4",
  "title": "Not Found",
  "status": 404,
  "detail": "Download with ID 'abc123' not found"
}
```

**Implementação:**

```csharp
// src/Cutube.Api/Endpoints/DownloadsEndpoints.cs (adicionar ao group)
group.MapDelete("/{id}", async (
    string id,
    IDownloadStatusRepository statusRepository,
    IDownloadQueue downloadQueue,
    CancellationToken ct) =>
{
    // 1. Verificar se download existe
    var download = await statusRepository.GetByIdAsync(id, ct);

    if (download is null)
    {
        return Results.Problem(
            detail: $"Download with ID '{id}' not found",
            statusCode: 404,
            title: "Not Found");
    }

    // 2. Cancelar download se em andamento
    if (download.Status == Domain.Models.DownloadStatus.Downloading ||
        download.Status == Domain.Models.DownloadStatus.Queued)
    {
        await downloadQueue.CancelAsync(id, ct);
    }

    // 3. Remover da lista
    await statusRepository.DeleteAsync(id, ct);

    // 4. Deletar arquivos parciais se existirem
    if (!string.IsNullOrEmpty(download.FilePath))
    {
        // Deletar arquivo (implementação em tarefa 2.2.9)
    }

    return Results.NoContent();
})
.WithName("DeleteDownload")
.WithOpenApi()
.Produces(204)
.Produces<ProblemDetails>(404);
```

**Checklist:**
- [ ] Criar endpoint DELETE /api/downloads/{id}
- [ ] Cancelar download se em andamento
- [ ] Remover da lista de status
- [ ] Deletar arquivos parcialmente baixados
- [ ] Retornar 204 No Content
- [ ] Documentar no Swagger

**Critérios de aceito:**
- ✅ Download cancelado com sucesso
- ✅ Arquivos parcialmente baixados removidos
- ✅ 404 para ID inválido

---

### 2.2.6 Implementar endpoint: GET /api/videos/info

**Estimativa:** 4 horas

**Contrato da API:**

**Request:**
```
GET /api/videos/info?url=https://youtube.com/watch?v=example
```

**Response (200 OK):**
```json
{
  "id": "abc123",
  "title": "Video Title",
  "uploader": "Channel Name",
  "duration": "00:10:30",
  "thumbnailUrl": "https://i.ytimg.com/vi/abc123/maxresdefault.jpg",
  "viewCount": 1000000,
  "uploadDate": "2026-01-15T00:00:00Z",
  "formats": [
    {
      "formatId": "137",
      "extension": "mp4",
      "resolution": "1920x1080",
      "fileSize": 115343360
    },
    {
      "formatId": "140",
      "extension": "m4a",
      "resolution": null,
      "fileSize": 2097152
    }
  ]
}
```

**Response (400 Bad Request):**
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Bad Request",
  "status": 400,
  "detail": "Invalid URL or video not found"
}
```

**Implementação:**

```csharp
// src/Cutube.Api/DTOs/VideoInfoResponse.cs
namespace Cutube.Api.DTOs;

public record VideoInfoResponse(
    string Id,
    string Title,
    string Uploader,
    string Duration,
    string ThumbnailUrl,
    long? ViewCount,
    DateTime? UploadDate,
    VideoFormatDto[] Formats
);

public record VideoFormatDto(
    string FormatId,
    string Extension,
    string? Resolution,
    long? FileSize
);

// src/Cutube.Api/Endpoints/VideosEndpoints.cs
using Cutube.Api.DTOs;
using Cutube.Domain.Models;
using Cutube.Domain.Services;
using FluentResults;
using Microsoft.AspNetCore.Mvc;

namespace Cutube.Api.Endpoints;

public static class VideosEndpoints
{
    public static void MapVideosEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/videos")
            .WithTags("Videos")
            .WithOpenApi();

        group.MapGet("/info", async (
            [FromQuery] string url,
            IMetadataService metadataService,
            CancellationToken ct) =>
        {
            // 1. Validar URL
            if (string.IsNullOrWhiteSpace(url))
            {
                return Results.Problem(
                    detail: "URL parameter is required",
                    statusCode: 400,
                    title: "Bad Request");
            }

            // 2. Buscar metadados via Domain
            Result<VideoMetadata> metadataResult = await metadataService.GetMetadataAsync(url, ct);

            if (metadataResult.IsFailure)
            {
                return Results.Problem(
                    detail: metadataResult.Errors.First().Message,
                    statusCode: 400,
                    title: "Bad Request");
            }

            var metadata = metadataResult.Value;

            // 3. Converter para DTO
            var response = new VideoInfoResponse(
                metadata.Id,
                metadata.Title,
                metadata.Uploader,
                metadata.Duration.ToString("hh\:mm\:ss"),
                metadata.ThumbnailUrl,
                metadata.ViewCount,
                metadata.UploadDate,
                metadata.Formats.Select(f => new VideoFormatDto(
                    f.FormatId,
                    f.Extension,
                    f.Resolution,
                    f.FileSize
                )).ToArray()
            );

            return Results.Ok(response);
        })
        .WithName("GetVideoInfo")
        .WithOpenApi()
        .Produces<VideoInfoResponse>(200)
        .Produces<ProblemDetails>(400);
    }
}
```

**Checklist:**
- [ ] Criar DTOs (VideoInfoResponse, VideoFormatDto)
- [ ] Criar endpoint GET /api/videos/info?url=
- [ ] Chamar MetadataService do domínio
- [ ] Retornar metadados completos
- [ ] Documentar no Swagger
- [ ] Testar com várias URLs (YouTube, Vimeo, etc)

**Critérios de aceito:**
- ✅ Metadados retornados em < 2s
- ✅ Suporta YouTube e outras plataformas
- ✅ Formato de duração padronizado (HH:MM:SS)
- ✅ Tratamento de erro adequado

---

### 2.2.7 Configurar CORS para Next.js

**Estimativa:** 1 hora
**Arquivo:** `src/Cutube.Api/Program.cs`

**Configuração:**

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowNextJs", policy =>
    {
        policy.WithOrigins(
                "http://localhost:3000",
                "http://localhost:3001",
                "https://cutube.dev",    // Produção (opcional)
                "https://www.cutube.dev" // Produção (opcional)
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();

app.UseCors("AllowNextJs");
```

**Testes:**

```bash
# Test com curl
curl -X GET http://localhost:5000/api/downloads \
  -H "Origin: http://localhost:3000" \
  -H "Access-Control-Request-Method: GET" \
  -v

# Verificar headers CORS na resposta:
# Access-Control-Allow-Origin: http://localhost:3000
# Access-Control-Allow-Credentials: true
```

**Checklist:**
- [ ] Configurar CORS policy para localhost:3000
- [ ] Adicionar origins de produção (opcional)
- [ ] Permitir credenciais (cookies, auth headers futuros)
- [ ] Testar com curl/Postman
- [ ] Verificar headers CORS na resposta

**Critérios de aceito:**
- ✅ Next.js (localhost:3000) pode chamar API sem erros CORS
- ✅ Preflight OPTIONS requests funcionam
- ✅ Credentials permitidos (para futuras features)

---

### 2.2.8 Configurar Health Checks

**Estimativa:** 2 horas
**Arquivo:** `src/Cutube.Api/Program.cs`
**Arquivos novos:**
- `src/Cutube.Api/HealthChecks/YtDlpHealthCheck.cs`
- `src/Cutube.Api/HealthChecks/FfmpegHealthCheck.cs`
- `src/Cutube.Api/HealthChecks/DiskSpaceHealthCheck.cs`

**Endpoints:**
- `/health` - Health check simples (API está rodando)
- `/health/ready` - Readiness probe (dependências OK)
- `/health/live` - Liveness probe (API está viva)

**Implementação:**

```csharp
// Program.cs
builder.Services.AddHealthChecks()
    .AddCheck<YtDlpHealthCheck>("yt-dlp", tags: new[] { "ready" })
    .AddCheck<FfmpegHealthCheck>("ffmpeg", tags: new[] { "ready" })
    .AddCheck<DiskSpaceHealthCheck>("disk-space", tags: new[] { "ready" });

var app = builder.Build();

app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false // Apenas liveness, não checa dependências
});
```

**Health Checks customizados:**

```csharp
// src/Cutube.Api/HealthChecks/YtDlpHealthCheck.cs
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Cutube.Api.HealthChecks;

public class YtDlpHealthCheck : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Verificar se yt-dlp está instalado
            var processStartInfo = new ProcessStartInfo
            {
                FileName = "yt-dlp",
                Arguments = "--version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };

            using var process = Process.Start(processStartInfo);
            await process!.WaitForExitAsync(cancellationToken);

            if (process.ExitCode == 0)
            {
                var version = await process.StandardOutput.ReadToEndAsync();
                return HealthCheckResult.Healthy($"yt-dlp is available: {version.Trim()}");
            }

            return HealthCheckResult.Unhealthy("yt-dlp is not installed or not working");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("yt-dlp check failed", ex);
        }
    }
}

// src/Cutube.Api/HealthChecks/FfmpegHealthCheck.cs
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Cutube.Api.HealthChecks;

public class FfmpegHealthCheck : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Verificar se ffmpeg está instalado
            var processStartInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = "-version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };

            using var process = Process.Start(processStartInfo);
            await process!.WaitForExitAsync(cancellationToken);

            if (process.ExitCode == 0)
            {
                return HealthCheckResult.Healthy("ffmpeg is available");
            }

            return HealthCheckResult.Unhealthy("ffmpeg is not installed or not working");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("ffmpeg check failed", ex);
        }
    }
}

// src/Cutube.Api/HealthChecks/DiskSpaceHealthCheck.cs
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Cutube.Api.HealthChecks;

public class DiskSpaceHealthCheck : IHealthCheck
{
    private readonly long _minRequiredSpaceBytes = 1024 * 1024 * 1024; // 1GB

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var drive = new DriveInfo(Directory.GetCurrentDirectory());
            var availableSpace = drive.AvailableFreeSpace;

            if (availableSpace >= _minRequiredSpaceBytes)
            {
                var availableGB = availableSpace / (1024.0 * 1024.0 * 1024.0);
                return Task.FromResult(HealthCheckResult.Healthy(
                    $"Disk space OK: {availableGB:F2} GB available"));
            }

            var availableMB = availableSpace / (1024.0 * 1024.0);
            return Task.FromResult(HealthCheckResult.Degraded(
                $"Low disk space: {availableMB:F2} MB available (min 1 GB required)"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("Disk space check failed", ex));
        }
    }
}
```

**Testes:**

```bash
# Test health endpoints
curl http://localhost:5000/health
curl http://localhost:5000/health/ready
curl http://localhost:5000/health/live
```

**Checklist:**
- [ ] Criar health checks para dependências (yt-dlp, ffmpeg)
- [ ] Criar health check de espaço em disco
- [ ] Expor endpoints /health, /health/ready, /health/live
- [ ] Retornar 200 se healthy, 503 se unhealthy
- [ ] Testar todos endpoints

**Critérios de aceito:**
- ✅ Health check funcionando
- ✅ yt-dlp/ffmpeg detectados corretamente
- ✅ Respostas apropriadas (200 healthy, 503 unhealthy, 200 degraded)

---

### 2.2.9 Criar DownloadQueue (Background Processing)

**Estimativa:** 6 horas
**Arquivos:**
```
Cutube.Api/Services/
  ├── IDownloadQueue.cs
  ├── DownloadQueue.cs                    - Memory queue (Channel)
  ├── IDownloadStatusRepository.cs
  ├── InMemoryStatusRepository.cs         - Repository em memória
  └── BackgroundDownloadWorker.cs         - HostedService
```

**Implementação - DownloadQueue:**

```csharp
// src/Cutube.Api/Services/IDownloadQueue.cs
using Cutube.Domain.Models;

namespace Cutube.Api.Services;

public interface IDownloadQueue
{
    Task<string> EnqueueAsync(DownloadRequest request, CancellationToken ct);
    Task CancelAsync(string downloadId, CancellationToken ct);
}

// src/Cutube.Api/Services/DownloadQueue.cs
using System.Threading.Channels;
using Cutube.Domain.Models;

namespace Cutube.Api.Services;

public class DownloadQueue : IDownloadQueue
{
    private readonly Channel<(string Id, DownloadRequest Request)> _channel;
    private readonly ILogger<DownloadQueue> _logger;

    public DownloadQueue(ILogger<DownloadQueue> logger)
    {
        var options = new UnboundedChannelOptions { SingleReader = true };
        _channel = Channel.CreateUnbounded<(string, DownloadRequest)>(options);
        _logger = logger;
    }

    public async Task<string> EnqueueAsync(DownloadRequest request, CancellationToken ct)
    {
        var downloadId = Guid.NewGuid().ToString("N");
        await _channel.Writer.WriteAsync((downloadId, request), ct);
        _logger.LogInformation("Download {DownloadId} enqueued for URL {Url}", downloadId, request.Url);
        return downloadId;
    }

    public Task CancelAsync(string downloadId, CancellationToken ct)
    {
        // Implementação simplificada (será melhorada com CancellationTokenSource)
        _logger.LogInformation("Download {DownloadId} cancel requested", downloadId);
        return Task.CompletedTask;
    }

    public async IAsyncEnumerable<(string Id, DownloadRequest Request)> DequeueAllAsync(
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach (var item in _channel.Reader.ReadAllAsync(ct))
        {
            yield return item;
        }
    }
}
```

**Implementação - StatusRepository:**

```csharp
// src/Cutube.Api/Services/IDownloadStatusRepository.cs
using Cutube.Domain.Models;

namespace Cutube.Api.Services;

public interface IDownloadStatusRepository
{
    Task AddAsync(string id, DownloadStatus status);
    Task<DownloadStatus?> GetByIdAsync(string id, CancellationToken ct);
    Task<IEnumerable<DownloadStatus>> GetAllAsync(CancellationToken ct);
    Task UpdateProgressAsync(string id, DownloadProgress progress);
    Task DeleteAsync(string id, CancellationToken ct);
}

// src/Cutube.Api/Services/InMemoryStatusRepository.cs
using Cutube.Domain.Models;
using Microsoft.Extensions.Logging;

namespace Cutube.Api.Services;

public class InMemoryStatusRepository : IDownloadStatusRepository
{
    private readonly ConcurrentDictionary<string, DownloadStatus> _downloads = new();
    private readonly ILogger<InMemoryStatusRepository> _logger;

    public InMemoryStatusRepository(ILogger<InMemoryStatusRepository> logger)
    {
        _logger = logger;
    }

    public Task AddAsync(string id, DownloadStatus status)
    {
        _downloads[id] = status;
        _logger.LogInformation("Download {DownloadId} added to repository", id);
        return Task.CompletedTask;
    }

    public Task<DownloadStatus?> GetByIdAsync(string id, CancellationToken ct)
    {
        _downloads.TryGetValue(id, out var status);
        return Task.FromResult(status);
    }

    public Task<IEnumerable<DownloadStatus>> GetAllAsync(CancellationToken ct)
    {
        return Task.FromResult<IEnumerable<DownloadStatus>>(_downloads.Values.ToList());
    }

    public Task UpdateProgressAsync(string id, DownloadProgress progress)
    {
        if (_downloads.TryGetValue(id, out var status))
        {
            // Atualizar status mantendo outros campos
            var updated = status with
            {
                Status = progress.Status,
                Progress = progress.Percentage,
                Speed = progress.Speed,
                Eta = progress.Eta,
                DownloadedBytes = progress.DownloadedBytes,
                TotalBytes = progress.TotalBytes,
                ErrorMessage = progress.ErrorMessage
            };

            _downloads[id] = updated;
        }

        return Task.CompletedTask;
    }

    public Task DeleteAsync(string id, CancellationToken ct)
    {
        _downloads.TryRemove(id, out _);
        _logger.LogInformation("Download {DownloadId} removed from repository", id);
        return Task.CompletedTask;
    }
}
```

**Implementação - BackgroundDownloadWorker:**

```csharp
// src/Cutube.Api/Services/BackgroundDownloadWorker.cs
using Cutube.Domain.Models;
using Cutube.Domain.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Cutube.Api.Services;

public class BackgroundDownloadWorker : BackgroundService
{
    private readonly IDownloadQueue _queue;
    private readonly IDownloadService _downloadService;
    private readonly IDownloadStatusRepository _statusRepository;
    private readonly ILogger<BackgroundDownloadWorker> _logger;

    public BackgroundDownloadWorker(
        IDownloadQueue queue,
        IDownloadService downloadService,
        IDownloadStatusRepository statusRepository,
        ILogger<BackgroundDownloadWorker> logger)
    {
        _queue = queue;
        _downloadService = downloadService;
        _statusRepository = statusRepository;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("BackgroundDownloadWorker started");

        await foreach (var (downloadId, request) in _queue.DequeueAllAsync(stoppingToken))
        {
            _logger.LogInformation("Processing download {DownloadId} for URL {Url}", downloadId, request.Url);

            try
            {
                // 1. Inicializar status
                await _statusRepository.AddAsync(downloadId, new DownloadStatus
                {
                    Id = downloadId,
                    Url = request.Url,
                    Status = DownloadStatus.Queued,
                    Progress = 0,
                    CreatedAt = DateTime.UtcNow
                });

                // 2. Atualizar para Processing
                await _statusRepository.UpdateProgressAsync(downloadId, new DownloadProgress
                {
                    DownloadId = downloadId,
                    Status = DownloadStatus.Downloading,
                    Percentage = 0,
                    DownloadedBytes = 0,
                    TotalBytes = 0,
                    Speed = 0
                });

                // 3. Criar progress reporter para atualizar status
                var progress = new Progress<DownloadProgress>(async p =>
                {
                    await _statusRepository.UpdateProgressAsync(downloadId, p);
                    _logger.LogDebug("Download {DownloadId} progress: {Progress}%", downloadId, p.Percentage);
                });

                // 4. Executar download via Domain
                var result = await _downloadService.DownloadAsync(request, progress, stoppingToken);

                if (result.IsFailure)
                {
                    // Falha
                    await _statusRepository.UpdateProgressAsync(downloadId, new DownloadProgress
                    {
                        DownloadId = downloadId,
                        Status = DownloadStatus.Failed,
                        Percentage = 0,
                        ErrorMessage = result.Errors.First().Message
                    });

                    _logger.LogError("Download {DownloadId} failed: {Error}", downloadId, result.Errors.First().Message);
                }
                else
                {
                    // Sucesso
                    await _statusRepository.UpdateProgressAsync(downloadId, new DownloadProgress
                    {
                        DownloadId = downloadId,
                        Status = DownloadStatus.Completed,
                        Percentage = 100
                    });

                    _logger.LogInformation("Download {DownloadId} completed: {FilePath}", downloadId, result.Value.FilePath);
                }
            }
            catch (Exception ex)
            {
                // Erro inesperado
                await _statusRepository.UpdateProgressAsync(downloadId, new DownloadProgress
                {
                    DownloadId = downloadId,
                    Status = DownloadStatus.Failed,
                    Percentage = 0,
                    ErrorMessage = ex.Message
                });

                _logger.LogError(ex, "Download {DownloadId} failed with exception", downloadId);
            }
        }
    }
}

// src/Cutube.Api/Models/DownloadStatus.cs (record para status persistido)
namespace Cutube.Api.Models;

public record DownloadStatus
{
    public required string Id { get; init; }
    public required string Url { get; init; }
    public DownloadStatus Status { get; set; }
    public double Progress { get; set; }
    public double Speed { get; set; }
    public TimeSpan? Eta { get; set; }
    public long? DownloadedBytes { get; set; }
    public long? TotalBytes { get; set; }
    public string? FilePath { get; set; }
    public string? ErrorMessage { get; set; }
    public required DateTime CreatedAt { get; init; }
    public DateTime? CompletedAt { get; set; }
}
```

**Registro de serviços no Program.cs:**

```csharp
// Program.cs
builder.Services.AddSingleton<IDownloadQueue, DownloadQueue>();
builder.Services.AddSingleton<IDownloadStatusRepository, InMemoryStatusRepository>();
builder.Services.AddHostedService<BackgroundDownloadWorker>();

// Register Domain Services
builder.Services.AddSingleton<IDownloadService, DownloadService>();
builder.Services.AddSingleton<IMetadataService, MetadataService>();
builder.Services.AddSingleton<IValidationService, ValidationService>();

// Register Infrastructure (implementações concretas do CLI)
builder.Services.AddSingleton<IVideoDownloader, YtDlpDownloader>();
builder.Services.AddSingleton<IVideoProcessor, FfmpegProcessor>();
builder.Services.AddSingleton<IFileSystem, FileSystem>();
```

**Checklist:**
- [ ] Criar fila em memória usando System.Threading.Channels
- [ ] Criar HostedService para processar fila em background
- [ ] Criar repositório de status em memória (ConcurrentDictionary)
- [ ] Atualizar status durante download via IProgress
- [ ] Suportar múltiplos downloads simultâneos (SingleReader = false opcional)
- [ ] Registrar serviços no DI container
- [ ] Testar com múltiplos downloads enfileirados

**Critérios de aceito:**
- ✅ Downloads processados em background (não bloqueia API)
- ✅ Status atualizado em tempo real
- ✅ Múltiplos downloads funcionam (fila processa sequencialmente)
- ✅ Logs adequados para debugging

---

### 2.2.10 Testes de Integração da API

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

**Comandos:**
```bash
# Criar projeto de testes
dotnet new xunit -n Cutube.Api.Tests -o tests/Cutube.Api.Tests

# Adicionar ao solution
dotnet sln cutube.sln add tests/Cutube.Api.Tests/Cutube.Api.Tests.csproj

# Adicionar referências
dotnet add tests/Cutube.Api.Tests/Cutube.Api.Tests.csproj reference src/Cutube.Api/Cutube.Api.csproj
dotnet add tests/Cutube.Api.Tests/Cutube.Api.Tests.csproj reference src/Cutube.Domain/Cutube.Domain.csproj

# Adicionar pacotes
dotnet add tests/Cutube.Api.Tests/Cutube.Api.Tests.csproj package Microsoft.AspNetCore.Mvc.Testing
dotnet add tests/Cutube.Api.Tests/Cutube.Api.Tests.csproj package Moq
dotnet add tests/Cutube.Api.Tests/Cutube.Api.Tests.csproj package FluentAssertions
```

**TestWebApplicationFactory:**

```csharp
// tests/Cutube.Api.Tests/Helpers/TestWebApplicationFactory.cs
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;

namespace Cutube.Api.Tests.Helpers;

public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Replace services with mocks for testing
            // Example:
            // var descriptor = services.SingleOrDefault(
            //     d => d.ServiceType == typeof(IDownloadService));
            // if (descriptor != null)
            // {
            //     services.Remove(descriptor);
            //     var mock = new Mock<IDownloadService>();
            //     // Setup mock behavior
            //     services.AddSingleton(mock.Object);
            // }
        });
    }
}
```

**Exemplos de testes:**

```csharp
// tests/Cutube.Api.Tests/Integration/DownloadsEndpointsTests.cs
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using System.Net.Http.Json;
using Cutube.Api.Tests.Helpers;

namespace Cutube.Api.Tests.Integration;

public class DownloadsEndpointsTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public DownloadsEndpointsTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateDownload_WithValidUrl_Returns202Accepted()
    {
        // Arrange
        var request = new
        {
            Url = "https://youtube.com/watch?v=test"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/downloads", request);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.Accepted);

        var content = await response.Content.ReadFromJsonAsync<dynamic>();
        ((string?)content?.GetProperty("status").GetString()).Should().Be("queued");
    }

    [Fact]
    public async Task CreateDownload_WithInvalidUrl_Returns400BadRequest()
    {
        // Arrange
        var request = new
        {
            Url = "not-a-valid-url"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/downloads", request);

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetDownloads_ReturnsOkWithDownloadsList()
    {
        // Act
        var response = await _client.GetAsync("/api/downloads");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);

        var content = await response.Content.ReadFromJsonAsync<dynamic>();
        // Assert structure
    }
}

// tests/Cutube.Api.Tests/Integration/VideosEndpointsTests.cs
using FluentAssertions;
using Xunit;

namespace Cutube.Api.Tests.Integration;

public class VideosEndpointsTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public VideosEndpointsTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetVideoInfo_WithValidUrl_Returns200Ok()
    {
        // Arrange
        var url = "https://youtube.com/watch?v=test";

        // Act
        var response = await _client.GetAsync($"/api/videos/info?url={Uri.EscapeDataString(url)}");

        // Assert
        // Depende de mock ou video real (usar Skip se necessário)
        response.StatusCode.Should().BeOneOf(
            System.Net.HttpStatusCode.OK,
            System.Net.HttpStatusCode.BadRequest // Se mock não configurado
        );
    }
}

// tests/Cutube.Api.Tests/Integration/HealthChecksTests.cs
using FluentAssertions;
using Xunit;
using System.Net.Http.Json;

namespace Cutube.Api.Tests.Integration;

public class HealthChecksTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public HealthChecksTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Health_Returns200Ok()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
    }

    [Fact]
    public async Task HealthReady_Returns200OkOr503()
    {
        // Act
        var response = await _client.GetAsync("/health/ready");

        // Assert
        response.StatusCode.Should().BeOneOf(
            System.Net.HttpStatusCode.OK,
            System.Net.HttpStatusCode.ServiceUnavailable
        );
    }
}
```

**Checklist:**
- [ ] Criar projeto de testes xUnit
- [ ] Criar TestWebApplicationFactory
- [ ] Criar testes para POST /api/downloads
- [ ] Criar testes para GET /api/downloads
- [ ] Criar testes para GET /api/downloads/{id}
- [ ] Criar testes para DELETE /api/downloads/{id}
- [ ] Criar testes para GET /api/videos/info
- [ ] Criar testes para health checks
- [ ] Mock de dependências externas (yt-dlp, ffmpeg)
- [ ] Cobertura > 70%

**Critérios de aceito:**
- ✅ Todos testes passam: `dotnet test`
- ✅ Cobertura > 70%
- ✅ Testes executam em < 10 segundos

---

## Qualidade Gates - Fase 2.2

**ANTES de passar para Fase 2.3, TODOS os itens abaixo devem ser concluídos:**

- [ ] **dotnet build** - **ZERO warnings** em todos projetos
- [ ] **dotnet test** - **100% pass** em todos projetos
- [ ] Swagger UI acessível em http://localhost:5000/swagger
- [ ] CORS configurado e testado (curl/Postman)
- [ ] Health checks funcionando (/health, /health/ready)
- [ ] Todos endpoints REST funcionando
- [ ] Background processing (DownloadQueue) funcionando
- [ ] Code review aprovado

---

## Cronograma Detalhado

| Tarefa | Estimativa | Dependencies | Blocker |
|--------|-----------|--------------|---------|
| 2.2.1 Criar projeto API | 2h | Fase 2.1 | Não |
| 2.2.2 POST /api/downloads | 4h | 2.2.1 | Não |
| 2.2.3 GET /api/downloads | 3h | 2.2.1 | Não |
| 2.2.4 GET /api/downloads/{id} | 3h | 2.2.1 | Não |
| 2.2.5 DELETE /api/downloads/{id} | 2h | 2.2.1 | Não |
| 2.2.6 GET /api/videos/info | 4h | 2.2.1 | Não |
| 2.2.7 CORS | 1h | 2.2.1 | Não |
| 2.2.8 Health Checks | 2h | 2.2.1 | Não |
| 2.2.9 DownloadQueue + Worker | 6h | 2.2.2 | **Sim** |
| 2.2.10 Testes integração | 5h | 2.2.9 | **Sim** |

**Total:** 32 horas (5-6 dias)

---

## Tecnologias

- **ASP.NET Core Minimal API** - Framework
- **System.Threading.Channels** - Producer/Consumer pattern
- **Microsoft.AspNetCore.Mvc.Testing** - Testes de integração
- **xUnit** - Testes
- **Moq** - Mocking
- **FluentAssertions** - Asserts fluentes

---

## Próximos Passos

Após completar Fase 2.2:

1. ✅ **Fase 2.3**: Implementar SignalR (WebSocket)
   - Criar DownloadHub
   - Eventos de progresso em tempo real
   - Connection lifecycle management

2. 🎯 **Code Review**
   - Revisar endpoints REST
   - Validar background processing
   - Verificar logs e métricas

3. 📝 **Documentação**
   - Atualizar Swagger descriptions
   - Documentar exemplos de requests/responses
