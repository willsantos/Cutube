# Fase 3.4: Status Tracking & Dashboard

**Status:** 🎯 Planejamento  
**Épico:** Cutube-858 (Épico 3: Integração com RabbitMQ)  
**Duração:** 4-5 dias  
**Responsável:** Full Stack Developer  
**Prioridade:** 🔥 Alta  
**Dependência:** ✅ Fase 3.3 (Consumer) completa ou em andamento

---

## Objetivo

Implementar página de monitoramento (`/monitor`) no cutube-web para acompanhamento administrativo da fila de downloads, métricas de performance e status do sistema. A página de monitoramento é **distinta** da home page (`/`) - enquanto a home foca em criar downloads, o monitor foca em visibilidade administrativa do sistema.

**Benefícios:**
- ✅ Dashboard administrativo separado da experiência do usuário
- ✅ Visibilidade completa do estado do sistema (fila, workers, métricas)
- ✅ Tracking end-to-end via correlationId
- ✅ Métricas de performance (throughput, tempo médio, taxa de erro)
- ✅ Histórico de downloads com limpeza automática
- ✅ Debug e troubleshooting facilitado

---

## Visão Arquitetural

### Separação de Responsabilidades

```
cutube-web/
├── app/
│   ├── page.tsx                    # Home: Criar downloads + meus downloads
│   ├── monitor/page.tsx            # NOVO: Dashboard de monitoramento
│   ├── queue/page.tsx              # NOVO: Status da fila (mais técnico)
│   └── downloads/
│       ├── page.tsx                # Lista completa de downloads
│       └── [id]/page.tsx           # Detalhes de um download
```

**Home (`/`) - Foco: Usuário**
- Criar downloads rapidamente
- Ver meus downloads recentes
- UX simplificada

**Monitor (`/monitor`) - Foco: Administrador/Dev**
- Métricas de sistema em tempo real
- Fila de downloads ativos
- Downloads falhos e DLQ
- Throughput e performance

**Queue (`/queue`) - Foco: DevOps**
- Status da fila RabbitMQ
- Conexões e workers
- Health do sistema

### Fluxo de Dados

```
┌─────────────────────────────────────────────────────────────────────────┐
│                      cutube-web (Frontend)                              │
│                                                                         │
│  ┌──────────────┐     ┌─────────────────┐     ┌──────────────────────┐ │
│  │     /        │     │    /monitor     │     │      /queue          │ │
│  │   (Home)     │     │  (Monitor)      │     │   (Queue Status)     │ │
│  │              │     │                 │     │                      │ │
│  │ Criar        │     │ Métricas        │     │ Fila RabbitMQ        │ │
│  │ Downloads    │     │ Fila ativa      │     │ Workers              │ │
│  │ Recentes     │     │ Falhas          │     │ Health               │ │
│  └──────┬───────┘     └────────┬────────┘     └──────────┬───────────┘ │
│         │                      │                         │             │
│         └──────────────────────┼─────────────────────────┘             │
│                                │                                       │
│                         ┌──────▼──────┐                                │
│                         │  SignalR/   │                                │
│                         │    REST     │                                │
│                         └──────┬──────┘                                │
└────────────────────────────────┼────────────────────────────────────────┘
                                 │
┌────────────────────────────────▼────────────────────────────────────────┐
│                         Cutube.Api                                      │
│                                                                         │
│  ┌──────────────────────────────────────────────────────────────────┐  │
│  │              IDownloadStatusRepository                            │  │
│  └──────────────────────────┬───────────────────────────────────────┘  │
│                             │                                           │
│  ┌──────────────────────────▼───────────────────────────────────────┐  │
│  │                    StatusEndpoints                                 │  │
│  │  GET /api/downloads?status=...                                   │  │
│  │  GET /api/downloads/{correlationId}                              │  │
│  │  GET /api/metrics                                                │  │
│  │  PATCH /api/downloads/{id}/status (callback)                     │  │
│  └──────────────────────────┬───────────────────────────────────────┘  │
│                             │                                           │
│  ┌──────────────────────────▼───────────────────────────────────────┐  │
│  │                    SignalR Hub                                     │  │
│  └──────────────────────────────────────────────────────────────────┘  │
└───────────────────────────┬─────────────────────────────────────────────┘
                            │
┌───────────────────────────▼─────────────────────────────────────────────┐
│                      Cutube.Worker (Consumer)                          │
│  ┌──────────────────────────────────────────────────────────────────┐  │
│  │               StatusNotificationService                           │  │
│  │  • Callbacks para API                                            │  │
│  └──────────────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────────────┘
```

### Estados do Download

```
┌─────────┐     ┌──────────┐     ┌──────────────┐     ┌─────────────┐
│ Pending │ ──► │ Queued   │ ──► │ Processing   │ ──► │ Completed   │
│         │     │          │     │              │     │             │
└─────────┘     └──────────┘     └──────────────┘     └─────────────┘
                      │                   │                    │
                      │                   ▼                    │
                      │            ┌──────────┐               │
                      │            │ Failed   │               │
                      │            │ (retry)  │               │
                      │            └──────────┘               │
                      │                   │                    │
                      ▼                   ▼                    ▼
               ┌──────────────┐   ┌─────────────┐     ┌─────────────┐
               │ DLQ          │   │ Cancelled   │     │ Expired     │
               │ (permanent)  │   │             │     │ (>24h)      │
               └──────────────┘   └─────────────┘     └─────────────┘
```

---

## Tarefas

### 3.4.1 Criar DownloadStatusRepository

**Estimativa:** 4-5 horas

**Arquivos:**
```
src/Cutube.Api/Data/
  ├── IDownloadStatusRepository.cs
  ├── InMemoryStatusRepository.cs
  ├── DownloadStatus.cs
  └── DownloadState.cs
```

#### 3.4.1.1 Criar Enum DownloadState

```csharp
// src/Cutube.Api/Data/DownloadState.cs
namespace Cutube.Api.Data;

public enum DownloadState
{
    Pending,
    Queued,
    Processing,
    Completed,
    Failed,
    DeadLetter,
    Cancelled,
    Expired
}
```

#### 3.4.1.2 Criar Model DownloadStatus

```csharp
// src/Cutube.Api/Data/DownloadStatus.cs
namespace Cutube.Api.Data;

public class DownloadStatus
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public required string CorrelationId { get; set; }
    public required string Url { get; set; }
    public DownloadState State { get; set; } = DownloadState.Pending;
    public int Progress { get; set; } = 0;
    public double Speed { get; set; } = 0;
    public long DownloadedBytes { get; set; } = 0;
    public long? TotalBytes { get; set; }
    public TimeSpan? Eta { get; set; }
    public string? OutputPath { get; set; }
    public string? OutputFilename { get; set; }
    public bool AudioOnly { get; set; } = false;
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public TimeSpan? Duration => CompletedAt.HasValue && StartedAt.HasValue
        ? CompletedAt.Value - StartedAt.Value
        : null;
    public DownloadMetadata? Metadata { get; set; }
}

public class DownloadMetadata
{
    public string? Title { get; set; }
    public string? Duration { get; set; }
    public string? Thumbnail { get; set; }
    public string? Channel { get; set; }
}
```

#### 3.4.1.3 Criar Interface IDownloadStatusRepository

```csharp
// src/Cutube.Api/Data/IDownloadStatusRepository.cs
namespace Cutube.Api.Data;

public interface IDownloadStatusRepository
{
    Task AddAsync(DownloadStatus status, CancellationToken cancellationToken = default);
    Task UpdateAsync(string correlationId, Action<DownloadStatus> updateAction, CancellationToken cancellationToken = default);
    Task UpdateStateAsync(string correlationId, DownloadState newState, CancellationToken cancellationToken = default);
    Task UpdateProgressAsync(string correlationId, int progress, double speed, long downloadedBytes, CancellationToken cancellationToken = default);
    Task<DownloadStatus?> GetByCorrelationIdAsync(string correlationId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DownloadStatus>> GetAllAsync(DownloadState? state = null, int? limit = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DownloadStatus>> GetByStatesAsync(IEnumerable<DownloadState> states, int? limit = null, CancellationToken cancellationToken = default);
    Task<int> CleanupOldRecordsAsync(TimeSpan maxAge, CancellationToken cancellationToken = default);
    Task<Dictionary<DownloadState, int>> CountByStateAsync(CancellationToken cancellationToken = default);
    Task<DownloadStats> GetStatsAsync(CancellationToken cancellationToken = default);
}

public class DownloadStats
{
    public int TotalDownloads { get; set; }
    public int PendingCount { get; set; }
    public int QueuedCount { get; set; }
    public int ProcessingCount { get; set; }
    public int CompletedCount { get; set; }
    public int FailedCount { get; set; }
    public int DeadLetterCount { get; set; }
    public double AverageProcessingTimeSeconds { get; set; }
    public double ErrorRate { get; set; }
}
```

#### 3.4.1.4 Criar InMemoryStatusRepository

```csharp
// src/Cutube.Api/Data/InMemoryStatusRepository.cs
using System.Collections.Concurrent;

namespace Cutube.Api.Data;

public class InMemoryStatusRepository : IDownloadStatusRepository
{
    private readonly ConcurrentDictionary<string, DownloadStatus> _statuses = new();
    private readonly ILogger<InMemoryStatusRepository> _logger;

    public InMemoryStatusRepository(ILogger<InMemoryStatusRepository> logger)
    {
        _logger = logger;
    }

    public Task AddAsync(DownloadStatus status, CancellationToken cancellationToken = default)
    {
        _statuses[status.CorrelationId] = status;
        _logger.LogDebug("Added download status: {CorrelationId}, State: {State}", 
            status.CorrelationId, status.State);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(string correlationId, Action<DownloadStatus> updateAction, CancellationToken cancellationToken = default)
    {
        if (_statuses.TryGetValue(correlationId, out var status))
        {
            updateAction(status);
            status.UpdatedAt = DateTime.UtcNow;
            _logger.LogDebug("Updated download status: {CorrelationId}, State: {State}", 
                correlationId, status.State);
        }
        else
        {
            _logger.LogWarning("Download status not found for update: {CorrelationId}", correlationId);
        }
        return Task.CompletedTask;
    }

    public Task UpdateStateAsync(string correlationId, DownloadState newState, CancellationToken cancellationToken = default)
    {
        return UpdateAsync(correlationId, status =>
        {
            status.State = newState;
            
            if (newState == DownloadState.Processing && !status.StartedAt.HasValue)
            {
                status.StartedAt = DateTime.UtcNow;
            }
            
            if (newState is DownloadState.Completed or DownloadState.Failed or DownloadState.Cancelled or DownloadState.DeadLetter)
            {
                status.CompletedAt = DateTime.UtcNow;
            }
        }, cancellationToken);
    }

    public Task UpdateProgressAsync(string correlationId, int progress, double speed, long downloadedBytes, CancellationToken cancellationToken = default)
    {
        return UpdateAsync(correlationId, status =>
        {
            status.Progress = Math.Clamp(progress, 0, 100);
            status.Speed = speed;
            status.DownloadedBytes = downloadedBytes;
        }, cancellationToken);
    }

    public Task<DownloadStatus?> GetByCorrelationIdAsync(string correlationId, CancellationToken cancellationToken = default)
    {
        _statuses.TryGetValue(correlationId, out var status);
        return Task.FromResult(status);
    }

    public Task<IReadOnlyList<DownloadStatus>> GetAllAsync(DownloadState? state = null, int? limit = null, CancellationToken cancellationToken = default)
    {
        var query = _statuses.Values.AsEnumerable();

        if (state.HasValue)
        {
            query = query.Where(s => s.State == state.Value);
        }

        query = query.OrderByDescending(s => s.CreatedAt);

        if (limit.HasValue)
        {
            query = query.Take(limit.Value);
        }

        return Task.FromResult<IReadOnlyList<DownloadStatus>>(query.ToList());
    }

    public Task<IReadOnlyList<DownloadStatus>> GetByStatesAsync(IEnumerable<DownloadState> states, int? limit = null, CancellationToken cancellationToken = default)
    {
        var stateSet = states.ToHashSet();
        var query = _statuses.Values
            .Where(s => stateSet.Contains(s.State))
            .OrderByDescending(s => s.CreatedAt);

        if (limit.HasValue)
        {
            return Task.FromResult<IReadOnlyList<DownloadStatus>>(query.Take(limit.Value).ToList());
        }

        return Task.FromResult<IReadOnlyList<DownloadStatus>>(query.ToList());
    }

    public Task<int> CleanupOldRecordsAsync(TimeSpan maxAge, CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow - maxAge;
        var keysToRemove = _statuses
            .Where(kvp => kvp.Value.UpdatedAt < cutoff)
            .Select(kvp => kvp.Key)
            .ToList();

        int removedCount = 0;
        foreach (var key in keysToRemove)
        {
            if (_statuses.TryRemove(key, out _))
            {
                removedCount++;
            }
        }

        if (removedCount > 0)
        {
            _logger.LogInformation("Cleaned up {Count} old download records", removedCount);
        }

        return Task.FromResult(removedCount);
    }

    public Task<Dictionary<DownloadState, int>> CountByStateAsync(CancellationToken cancellationToken = default)
    {
        var counts = _statuses.Values
            .GroupBy(s => s.State)
            .ToDictionary(g => g.Key, g => g.Count());

        return Task.FromResult(counts);
    }

    public Task<DownloadStats> GetStatsAsync(CancellationToken cancellationToken = default)
    {
        var counts = CountByStateAsync(cancellationToken).Result;

        var completed = _statuses.Values.Where(s => s.State == DownloadState.Completed && s.Duration.HasValue).ToList();
        var avgProcessingTime = completed.Any() 
            ? completed.Average(s => s.Duration!.Value.TotalSeconds) 
            : 0;

        var totalCompleted = counts.GetValueOrDefault(DownloadState.Completed);
        var totalFailed = counts.GetValueOrDefault(DownloadState.Failed) + counts.GetValueOrDefault(DownloadState.DeadLetter);
        var errorRate = totalCompleted + totalFailed > 0 
            ? (double)totalFailed / (totalCompleted + totalFailed) * 100 
            : 0;

        var stats = new DownloadStats
        {
            TotalDownloads = _statuses.Count,
            PendingCount = counts.GetValueOrDefault(DownloadState.Pending),
            QueuedCount = counts.GetValueOrDefault(DownloadState.Queued),
            ProcessingCount = counts.GetValueOrDefault(DownloadState.Processing),
            CompletedCount = totalCompleted,
            FailedCount = totalFailed,
            DeadLetterCount = counts.GetValueOrDefault(DownloadState.DeadLetter),
            AverageProcessingTimeSeconds = avgProcessingTime,
            ErrorRate = errorRate
        };

        return Task.FromResult(stats);
    }
}
```

#### 3.4.1.5 Criar CleanupHostedService

```csharp
// src/Cutube.Api/Services/DownloadStatusCleanupService.cs
namespace Cutube.Api.Services;

public class DownloadStatusCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DownloadStatusCleanupService> _logger;
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromHours(1);
    private readonly TimeSpan _maxAge = TimeSpan.FromHours(24);

    public DownloadStatusCleanupService(
        IServiceProvider serviceProvider,
        ILogger<DownloadStatusCleanupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("DownloadStatusCleanupService started. Cleanup interval: {Interval}, Max age: {MaxAge}", 
            _cleanupInterval, _maxAge);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_cleanupInterval, stoppingToken);

                using var scope = _serviceProvider.CreateScope();
                var repository = scope.ServiceProvider.GetRequiredService<IDownloadStatusRepository>();

                var removedCount = await repository.CleanupOldRecordsAsync(_maxAge, stoppingToken);
                
                if (removedCount > 0)
                {
                    _logger.LogInformation("Cleanup removed {Count} old download records", removedCount);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during cleanup");
            }
        }

        _logger.LogInformation("DownloadStatusCleanupService stopped");
    }
}
```

**Checklist:**
- [ ] Criar DownloadState enum
- [ ] Criar DownloadStatus model
- [ ] Criar IDownloadStatusRepository interface
- [ ] Criar InMemoryStatusRepository
- [ ] Criar DownloadStatusCleanupService
- [ ] Registrar repositório e service no DI

**Critérios de aceite:**
- ✅ Repositório thread-safe
- ✅ CRUD funciona corretamente
- ✅ Limpeza automática remove registros >24h

---

### 3.4.2 Criar Endpoints de Status

**Estimativa:** 4-5 horas

**Arquivos:**
```
src/Cutube.Api/Endpoints/
  ├── StatusEndpoints.cs
  └── DownloadStatusResponse.cs
```

#### 3.4.2.1 Criar DTOs de Resposta

```csharp
// src/Cutube.Api/Endpoints/DownloadStatusResponse.cs
using Cutube.Api.Data;

namespace Cutube.Api.Endpoints;

public record DownloadStatusResponse
{
    public required string Id { get; init; }
    public required string CorrelationId { get; init; }
    public required string Url { get; init; }
    public required string State { get; init; }
    public int Progress { get; init; }
    public double Speed { get; init; }
    public long DownloadedBytes { get; init; }
    public long? TotalBytes { get; init; }
    public string? Eta { get; init; }
    public string? OutputPath { get; init; }
    public string? OutputFilename { get; init; }
    public bool AudioOnly { get; init; }
    public string? ErrorMessage { get; init; }
    public int RetryCount { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public DateTime? StartedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public TimeSpan? Duration { get; init; }
    public DownloadMetadataResponse? Metadata { get; init; }
}

public record DownloadMetadataResponse
{
    public string? Title { get; init; }
    public string? Duration { get; init; }
    public string? Thumbnail { get; init; }
    public string? Channel { get; init; }
}

public record DownloadListResponse
{
    public required IReadOnlyList<DownloadStatusResponse> Downloads { get; init; }
    public int TotalCount { get; init; }
}

public record MetricsResponse
{
    public int TotalDownloads { get; init; }
    public int PendingCount { get; init; }
    public int QueuedCount { get; init; }
    public int ProcessingCount { get; init; }
    public int CompletedCount { get; init; }
    public int FailedCount { get; init; }
    public int DeadLetterCount { get; init; }
    public double AverageProcessingTimeSeconds { get; init; }
    public double ErrorRate { get; init; }
    public double ThroughputPerMinute { get; init; }
}

public record UpdateStatusRequest
{
    public required string State { get; init; }
    public string? ErrorMessage { get; init; }
}

public record UpdateProgressRequest
{
    public int Progress { get; init; }
    public double Speed { get; init; }
    public long DownloadedBytes { get; init; }
    public long? TotalBytes { get; init; }
    public string? Eta { get; init; }
}
```

#### 3.4.2.2 Criar StatusEndpoints

```csharp
// src/Cutube.Api/Endpoints/StatusEndpoints.cs
using Cutube.Api.Data;
using Cutube.Api.Hubs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace Cutube.Api.Endpoints;

public static class StatusEndpoints
{
    public static void MapStatusEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/downloads")
            .WithTags("Downloads")
            .WithOpenApi();

        // GET /api/downloads - Listar todos os downloads com filtro opcional
        group.MapGet("/", async (
            [FromQuery] string? status,
            [FromQuery] int? limit,
            IDownloadStatusRepository repository,
            CancellationToken ct) =>
        {
            IReadOnlyList<DownloadStatus> downloads;

            if (!string.IsNullOrEmpty(status) && Enum.TryParse<DownloadState>(status, true, out var state))
            {
                downloads = await repository.GetAllAsync(state, limit, ct);
            }
            else
            {
                downloads = await repository.GetAllAsync(limit: limit, cancellationToken: ct);
            }

            var response = new DownloadListResponse
            {
                Downloads = downloads.Select(MapToResponse).ToList(),
                TotalCount = downloads.Count
            };

            return Results.Ok(response);
        })
        .WithName("GetDownloads")
        .WithSummary("List all downloads with optional status filter")
        .WithOpenApi();

        // GET /api/downloads/{correlationId} - Buscar por correlation ID
        group.MapGet("/{correlationId}", async (
            string correlationId,
            IDownloadStatusRepository repository,
            CancellationToken ct) =>
        {
            var download = await repository.GetByCorrelationIdAsync(correlationId, ct);

            if (download == null)
            {
                return Results.NotFound(new { error = "Download not found", correlationId });
            }

            return Results.Ok(MapToResponse(download));
        })
        .WithName("GetDownloadById")
        .WithSummary("Get download status by correlation ID")
        .WithOpenApi();

        // GET /api/downloads/queue/active - Downloads ativos (queued + processing)
        group.MapGet("/queue/active", async (
            IDownloadStatusRepository repository,
            CancellationToken ct) =>
        {
            var downloads = await repository.GetByStatesAsync(
                new[] { DownloadState.Queued, DownloadState.Processing }, 
                cancellationToken: ct);

            return Results.Ok(new DownloadListResponse
            {
                Downloads = downloads.Select(MapToResponse).ToList(),
                TotalCount = downloads.Count
            });
        })
        .WithName("GetActiveDownloads")
        .WithSummary("Get active downloads (queued and processing)")
        .WithOpenApi();

        // GET /api/downloads/queue/failed - Downloads com falha
        group.MapGet("/queue/failed", async (
            IDownloadStatusRepository repository,
            CancellationToken ct) =>
        {
            var downloads = await repository.GetByStatesAsync(
                new[] { DownloadState.Failed, DownloadState.DeadLetter }, 
                cancellationToken: ct);

            return Results.Ok(new DownloadListResponse
            {
                Downloads = downloads.Select(MapToResponse).ToList(),
                TotalCount = downloads.Count
            });
        })
        .WithName("GetFailedDownloads")
        .WithSummary("Get failed and dead letter downloads")
        .WithOpenApi();

        // PATCH /api/downloads/{correlationId}/status - Callback do worker
        group.MapPatch("/{correlationId}/status", async (
            string correlationId,
            [FromBody] UpdateStatusRequest request,
            IDownloadStatusRepository repository,
            IHubContext<DownloadHub, IDownloadHubClient> hub,
            CancellationToken ct) =>
        {
            if (!Enum.TryParse<DownloadState>(request.State, true, out var newState))
            {
                return Results.BadRequest(new { error = "Invalid state" });
            }

            var download = await repository.GetByCorrelationIdAsync(correlationId, ct);
            if (download == null)
            {
                return Results.NotFound(new { error = "Download not found", correlationId });
            }

            await repository.UpdateAsync(correlationId, status =>
            {
                status.State = newState;
                if (!string.IsNullOrEmpty(request.ErrorMessage))
                {
                    status.ErrorMessage = request.ErrorMessage;
                }
            }, ct);

            // Notificar via SignalR
            var groupName = DownloadHub.GetDownloadGroupName(correlationId);
            await hub.Clients.Group(groupName)
                .DownloadStatusChanged(correlationId, request.State);

            return Results.NoContent();
        })
        .WithName("UpdateDownloadStatus")
        .WithSummary("Update download status (worker callback)")
        .WithOpenApi();

        // POST /api/downloads/{correlationId}/progress - Callback de progresso
        group.MapPost("/{correlationId}/progress", async (
            string correlationId,
            [FromBody] UpdateProgressRequest request,
            IDownloadStatusRepository repository,
            IHubContext<DownloadHub, IDownloadHubClient> hub,
            CancellationToken ct) =>
        {
            var download = await repository.GetByCorrelationIdAsync(correlationId, ct);
            if (download == null)
            {
                return Results.NotFound(new { error = "Download not found", correlationId });
            }

            await repository.UpdateProgressAsync(
                correlationId, 
                request.Progress, 
                request.Speed, 
                request.DownloadedBytes, 
                ct);

            // Notificar via SignalR
            var groupName = DownloadHub.GetDownloadGroupName(correlationId);
            await hub.Clients.Group(groupName)
                .DownloadProgress(correlationId, new DownloadProgressEvent(
                    correlationId,
                    request.Progress,
                    request.Speed,
                    request.Eta,
                    request.DownloadedBytes,
                    request.TotalBytes ?? 0,
                    "downloading"
                ));

            return Results.NoContent();
        })
        .WithName("UpdateDownloadProgress")
        .WithSummary("Update download progress (worker callback)")
        .WithOpenApi();

        // GET /api/metrics - Métricas do sistema
        app.MapGet("/api/metrics", async (
            IDownloadStatusRepository repository,
            CancellationToken ct) =>
        {
            var stats = await repository.GetStatsAsync(ct);

            // Calcular throughput (downloads completados na última hora)
            var recentCompleted = (await repository.GetAllAsync(DownloadState.Completed, cancellationToken: ct))
                .Where(d => d.CompletedAt.HasValue && d.CompletedAt.Value > DateTime.UtcNow.AddHours(-1))
                .ToList();

            var throughput = recentCompleted.Any() 
                ? recentCompleted.Count / 60.0 
                : 0;

            var response = new MetricsResponse
            {
                TotalDownloads = stats.TotalDownloads,
                PendingCount = stats.PendingCount,
                QueuedCount = stats.QueuedCount,
                ProcessingCount = stats.ProcessingCount,
                CompletedCount = stats.CompletedCount,
                FailedCount = stats.FailedCount,
                DeadLetterCount = stats.DeadLetterCount,
                AverageProcessingTimeSeconds = stats.AverageProcessingTimeSeconds,
                ErrorRate = stats.ErrorRate,
                ThroughputPerMinute = throughput
            };

            return Results.Ok(response);
        })
        .WithName("GetMetrics")
        .WithSummary("Get system metrics")
        .WithOpenApi();
    }

    private static DownloadStatusResponse MapToResponse(DownloadStatus status)
    {
        return new DownloadStatusResponse
        {
            Id = status.Id,
            CorrelationId = status.CorrelationId,
            Url = status.Url,
            State = status.State.ToString().ToLowerInvariant(),
            Progress = status.Progress,
            Speed = status.Speed,
            DownloadedBytes = status.DownloadedBytes,
            TotalBytes = status.TotalBytes,
            Eta = status.Eta?.ToString(),
            OutputPath = status.OutputPath,
            OutputFilename = status.OutputFilename,
            AudioOnly = status.AudioOnly,
            ErrorMessage = status.ErrorMessage,
            RetryCount = status.RetryCount,
            CreatedAt = status.CreatedAt,
            UpdatedAt = status.UpdatedAt,
            StartedAt = status.StartedAt,
            CompletedAt = status.CompletedAt,
            Duration = status.Duration,
            Metadata = status.Metadata == null ? null : new DownloadMetadataResponse
            {
                Title = status.Metadata.Title,
                Duration = status.Metadata.Duration,
                Thumbnail = status.Metadata.Thumbnail,
                Channel = status.Metadata.Channel
            }
        };
    }
}
```

**Checklist:**
- [ ] Criar DTOs de resposta
- [ ] Criar StatusEndpoints com todos endpoints
- [ ] Implementar callbacks PATCH e POST
- [ ] Implementar GET /api/metrics
- [ ] Registrar endpoints no Program.cs

**Critérios de aceite:**
- ✅ Todos endpoints retornam dados corretos
- ✅ Callbacks atualizam repositório e disparam SignalR
- ✅ Métricas calculadas corretamente

---

### 3.4.3 Criar Página de Monitoramento (/monitor)

**Estimativa:** 6-8 horas

**Arquivos:**
```
cutube-web/
  ├── app/monitor/page.tsx                 # Página de monitoramento
  ├── app/queue/page.tsx                   # Página de status da fila
  ├── components/monitor/
  │   ├── MonitorMetrics.tsx               # Cards de métricas
  │   ├── ActiveQueue.tsx                  # Fila ativa
  │   ├── FailedDownloads.tsx              # Downloads falhos
  │   ├── QueueStatusCard.tsx              # Card de status da fila
  │   └── SystemHealth.tsx                 # Health do sistema
  ├── hooks/
  │   └── useMonitor.ts                    # Hook para monitoramento
  └── lib/
      └── api.ts (adicionar endpoints)
```

#### 3.4.3.1 Adicionar Endpoints na API

```typescript
// cutube-web/lib/api.ts (adicionar)

export interface DownloadStatus {
  id: string;
  correlationId: string;
  url: string;
  state: 'pending' | 'queued' | 'processing' | 'completed' | 'failed' | 'dead_letter' | 'cancelled' | 'expired';
  progress: number;
  speed: number;
  downloadedBytes: number;
  totalBytes?: number;
  eta?: string;
  outputPath?: string;
  outputFilename?: string;
  audioOnly: boolean;
  errorMessage?: string;
  retryCount: number;
  createdAt: string;
  updatedAt: string;
  startedAt?: string;
  completedAt?: string;
  duration?: string;
  metadata?: {
    title?: string;
    duration?: string;
    thumbnail?: string;
    channel?: string;
  };
}

export interface DownloadListResponse {
  downloads: DownloadStatus[];
  totalCount: number;
}

export interface MetricsResponse {
  totalDownloads: number;
  pendingCount: number;
  queuedCount: number;
  processingCount: number;
  completedCount: number;
  failedCount: number;
  deadLetterCount: number;
  averageProcessingTimeSeconds: number;
  errorRate: number;
  throughputPerMinute: number;
}

export async function getDownloads(status?: string, limit?: number): Promise<DownloadListResponse> {
  const params = new URLSearchParams();
  if (status) params.append('status', status);
  if (limit) params.append('limit', limit.toString());

  const response = await fetch(`/api/downloads?${params}`);
  if (!response.ok) {
    throw new Error('Failed to fetch downloads');
  }
  return response.json();
}

export async function getActiveDownloads(): Promise<DownloadListResponse> {
  const response = await fetch('/api/downloads/queue/active');
  if (!response.ok) {
    throw new Error('Failed to fetch active downloads');
  }
  return response.json();
}

export async function getFailedDownloads(): Promise<DownloadListResponse> {
  const response = await fetch('/api/downloads/queue/failed');
  if (!response.ok) {
    throw new Error('Failed to fetch failed downloads');
  }
  return response.json();
}

export async function getMetrics(): Promise<MetricsResponse> {
  const response = await fetch('/api/metrics');
  if (!response.ok) {
    throw new Error('Failed to fetch metrics');
  }
  return response.json();
}
```

#### 3.4.3.2 Criar Hook useMonitor

```typescript
// cutube-web/hooks/useMonitor.ts
'use client';

import { useState, useEffect, useCallback } from 'react';
import { DownloadStatus, MetricsResponse, getActiveDownloads, getFailedDownloads, getMetrics } from '@/lib/api';

interface UseMonitorReturn {
  activeDownloads: DownloadStatus[];
  failedDownloads: DownloadStatus[];
  metrics: MetricsResponse | null;
  isLoading: boolean;
  error: string | null;
  refresh: () => Promise<void>;
  lastUpdated: Date | null;
}

export function useMonitor(refreshInterval = 5000): UseMonitorReturn {
  const [activeDownloads, setActiveDownloads] = useState<DownloadStatus[]>([]);
  const [failedDownloads, setFailedDownloads] = useState<DownloadStatus[]>([]);
  const [metrics, setMetrics] = useState<MetricsResponse | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [lastUpdated, setLastUpdated] = useState<Date | null>(null);

  const fetchData = useCallback(async () => {
    try {
      setError(null);
      const [active, failed, metricsData] = await Promise.all([
        getActiveDownloads(),
        getFailedDownloads(),
        getMetrics()
      ]);

      setActiveDownloads(active.downloads);
      setFailedDownloads(failed.downloads);
      setMetrics(metricsData);
      setLastUpdated(new Date());
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Unknown error');
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchData();

    const interval = setInterval(fetchData, refreshInterval);
    return () => clearInterval(interval);
  }, [fetchData, refreshInterval]);

  return {
    activeDownloads,
    failedDownloads,
    metrics,
    isLoading,
    error,
    refresh: fetchData,
    lastUpdated
  };
}
```

#### 3.4.3.3 Criar MonitorMetrics

```tsx
// cutube-web/components/monitor/MonitorMetrics.tsx
'use client';

import { MetricsResponse } from '@/lib/api';

interface MonitorMetricsProps {
  metrics: MetricsResponse | null;
}

export function MonitorMetrics({ metrics }: MonitorMetricsProps) {
  if (!metrics) {
    return (
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mb-6">
        {[...Array(4)].map((_, i) => (
          <div key={i} className="bg-gray-100 animate-pulse h-24 rounded-lg" />
        ))}
      </div>
    );
  }

  const formatDuration = (seconds: number) => {
    if (seconds < 60) return `${Math.round(seconds)}s`;
    if (seconds < 3600) return `${Math.round(seconds / 60)}m`;
    return `${Math.round(seconds / 3600)}h`;
  };

  const cards = [
    {
      label: 'Processing',
      value: metrics.processingCount,
      subtext: `${metrics.queuedCount} queued`,
      color: 'bg-blue-500',
      icon: '⚡'
    },
    {
      label: 'Completed',
      value: metrics.completedCount,
      subtext: `${metrics.throughputPerMinute.toFixed(1)}/min`,
      color: 'bg-green-500',
      icon: '✅'
    },
    {
      label: 'Failed / DLQ',
      value: metrics.failedCount + metrics.deadLetterCount,
      subtext: `${metrics.errorRate.toFixed(1)}% error rate`,
      color: metrics.errorRate > 10 ? 'bg-red-500' : 'bg-yellow-500',
      icon: '⚠️'
    },
    {
      label: 'Avg Time',
      value: formatDuration(metrics.averageProcessingTimeSeconds),
      subtext: 'per download',
      color: 'bg-purple-500',
      icon: '⏱️'
    }
  ];

  return (
    <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mb-6">
      {cards.map((card) => (
        <div key={card.label} className="bg-white rounded-lg shadow p-4">
          <div className="flex items-center justify-between mb-2">
            <span className="text-2xl">{card.icon}</span>
            <span className={`text-white text-xs px-2 py-1 rounded ${card.color}`}>
              {card.label}
            </span>
          </div>
          <div className="text-3xl font-bold text-gray-800">{card.value}</div>
          <div className="text-sm text-gray-500">{card.subtext}</div>
        </div>
      ))}
    </div>
  );
}
```

#### 3.4.3.4 Criar QueueStatusCard

```tsx
// cutube-web/components/monitor/QueueStatusCard.tsx
'use client';

import { DownloadStatus } from '@/lib/api';

interface QueueStatusCardProps {
  download: DownloadStatus;
}

export function QueueStatusCard({ download }: QueueStatusCardProps) {
  const getStatusColor = (state: string) => {
    switch (state) {
      case 'queued': return 'bg-gray-100 text-gray-700 border-gray-300';
      case 'processing': return 'bg-blue-50 text-blue-700 border-blue-300';
      case 'completed': return 'bg-green-50 text-green-700 border-green-300';
      case 'failed': return 'bg-red-50 text-red-700 border-red-300';
      case 'dead_letter': return 'bg-orange-50 text-orange-700 border-orange-300';
      default: return 'bg-gray-50 text-gray-700 border-gray-300';
    }
  };

  const getStatusIcon = (state: string) => {
    switch (state) {
      case 'queued': return '⏳';
      case 'processing': return '⚡';
      case 'completed': return '✅';
      case 'failed': return '❌';
      case 'dead_letter': return '⚠️';
      default: return '❓';
    }
  };

  const formatBytes = (bytes: number) => {
    if (bytes === 0) return '0 B';
    const k = 1024;
    const sizes = ['B', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
  };

  const formatSpeed = (bytesPerSecond: number) => {
    return formatBytes(bytesPerSecond) + '/s';
  };

  return (
    <div className={`bg-white rounded-lg shadow p-4 border-2 ${getStatusColor(download.state)}`}>
      {/* Header */}
      <div className="flex justify-between items-start mb-3">
        <div className="flex-1 min-w-0">
          <h3 className="font-semibold text-gray-800 truncate" title={download.url}>
            {download.metadata?.title || 'Untitled'}
          </h3>
          <p className="text-xs text-gray-500 truncate">{download.url}</p>
        </div>
        <span className={`text-xs px-2 py-1 rounded-full border ${getStatusColor(download.state)}`}>
          {getStatusIcon(download.state)} {download.state}
        </span>
      </div>

      {/* Progress */}
      {download.state === 'processing' && (
        <div className="mb-3">
          <div className="flex justify-between text-sm mb-1">
            <span className="text-gray-700 font-medium">{download.progress}%</span>
            <span className="text-gray-500">{formatSpeed(download.speed)}</span>
          </div>
          <div className="w-full bg-gray-200 rounded-full h-2">
            <div
              className="bg-blue-600 h-2 rounded-full transition-all duration-300"
              style={{ width: `${download.progress}%` }}
            />
          </div>
          <div className="flex justify-between text-xs text-gray-500 mt-1">
            <span>{formatBytes(download.downloadedBytes)}</span>
            <span>{download.totalBytes ? formatBytes(download.totalBytes) : 'Unknown'}</span>
          </div>
        </div>
      )}

      {/* Footer */}
      <div className="flex justify-between items-center text-xs text-gray-500">
        <div>
          <span className="font-mono">ID: {download.correlationId.slice(0, 8)}...</span>
          {download.retryCount > 0 && (
            <span className="ml-2 text-orange-600 font-semibold">Retry: {download.retryCount}</span>
          )}
        </div>
        <div className="flex gap-2">
          {download.audioOnly && <span className="text-purple-600">🎵 Audio</span>}
          {download.metadata?.duration && (
            <span className="text-gray-400">⏱️ {download.metadata.duration}</span>
          )}
        </div>
      </div>

      {/* Error Message */}
      {download.errorMessage && (
        <div className="mt-2 text-xs text-red-600 bg-red-50 p-2 rounded border border-red-200">
          <strong>Error:</strong> {download.errorMessage}
        </div>
      )}
    </div>
  );
}
```

#### 3.4.3.5 Criar ActiveQueue

```tsx
// cutube-web/components/monitor/ActiveQueue.tsx
'use client';

import { DownloadStatus } from '@/lib/api';
import { QueueStatusCard } from './QueueStatusCard';

interface ActiveQueueProps {
  downloads: DownloadStatus[];
}

export function ActiveQueue({ downloads }: ActiveQueueProps) {
  const processing = downloads.filter(d => d.state === 'processing');
  const queued = downloads.filter(d => d.state === 'queued');

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h2 className="text-xl font-semibold flex items-center gap-2">
          <span className="text-blue-500">⚡</span>
          Active Queue
        </h2>
        <div className="text-sm text-gray-500">
          <span className="inline-flex items-center px-2 py-1 bg-blue-100 text-blue-700 rounded mr-2">
            {processing.length} processing
          </span>
          <span className="inline-flex items-center px-2 py-1 bg-gray-100 text-gray-700 rounded">
            {queued.length} queued
          </span>
        </div>
      </div>

      {downloads.length === 0 ? (
        <div className="bg-gray-50 rounded-lg p-8 text-center text-gray-500 border-2 border-dashed border-gray-300">
          <p className="text-4xl mb-2">📭</p>
          <p className="font-medium">Queue is empty</p>
          <p className="text-sm mt-1">No active downloads at the moment</p>
        </div>
      ) : (
        <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
          {downloads.map((download) => (
            <QueueStatusCard key={download.correlationId} download={download} />
          ))}
        </div>
      )}
    </div>
  );
}
```

#### 3.4.3.6 Criar FailedDownloads

```tsx
// cutube-web/components/monitor/FailedDownloads.tsx
'use client';

import { DownloadStatus } from '@/lib/api';
import { QueueStatusCard } from './QueueStatusCard';

interface FailedDownloadsProps {
  downloads: DownloadStatus[];
}

export function FailedDownloads({ downloads }: FailedDownloadsProps) {
  if (downloads.length === 0) {
    return null;
  }

  const failed = downloads.filter(d => d.state === 'failed');
  const deadLetter = downloads.filter(d => d.state === 'dead_letter');

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h2 className="text-xl font-semibold flex items-center gap-2 text-red-600">
          <span>⚠️</span>
          Failed Downloads
        </h2>
        <div className="text-sm">
          <span className="inline-flex items-center px-2 py-1 bg-yellow-100 text-yellow-700 rounded mr-2">
            {failed.length} retryable
          </span>
          <span className="inline-flex items-center px-2 py-1 bg-orange-100 text-orange-700 rounded">
            {deadLetter.length} dead letter
          </span>
        </div>
      </div>

      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
        {downloads.map((download) => (
          <QueueStatusCard key={download.correlationId} download={download} />
        ))}
      </div>
    </div>
  );
}
```

#### 3.4.3.7 Criar Página /monitor

```tsx
// cutube-web/app/monitor/page.tsx
'use client';

import { useMonitor } from '@/hooks/useMonitor';
import { MonitorMetrics } from '@/components/monitor/MonitorMetrics';
import { ActiveQueue } from '@/components/monitor/ActiveQueue';
import { FailedDownloads } from '@/components/monitor/FailedDownloads';

export default function MonitorPage() {
  const { 
    activeDownloads, 
    failedDownloads, 
    metrics, 
    isLoading, 
    error, 
    refresh,
    lastUpdated 
  } = useMonitor(5000);

  if (error) {
    return (
      <div className="min-h-screen bg-gray-50 p-6">
        <div className="max-w-7xl mx-auto">
          <div className="bg-red-50 border border-red-200 rounded-lg p-6 text-center">
            <p className="text-4xl mb-2">❌</p>
            <h2 className="text-lg font-semibold text-red-700 mb-2">Error loading monitor</h2>
            <p className="text-red-600 mb-4">{error}</p>
            <button
              onClick={refresh}
              className="px-4 py-2 bg-red-600 text-white rounded hover:bg-red-700"
            >
              Retry
            </button>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gray-50 p-6">
      <div className="max-w-7xl mx-auto">
        {/* Header */}
        <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center mb-6 gap-4">
          <div>
            <h1 className="text-3xl font-bold text-gray-800">Monitor</h1>
            <p className="text-gray-500">
              System monitoring and queue status
              {lastUpdated && (
                <span className="ml-2 text-xs text-gray-400">
                  Last updated: {lastUpdated.toLocaleTimeString()}
                </span>
              )}
            </p>
          </div>
          <button
            onClick={refresh}
            disabled={isLoading}
            className="px-4 py-2 bg-blue-600 text-white rounded hover:bg-blue-700 disabled:opacity-50 flex items-center gap-2"
          >
            {isLoading ? '🔄 Refreshing...' : '🔄 Refresh'}
          </button>
        </div>

        {/* Metrics */}
        <MonitorMetrics metrics={metrics} />

        {/* Active Queue */}
        <div className="mb-8">
          <ActiveQueue downloads={activeDownloads} />
        </div>

        {/* Failed Downloads */}
        <FailedDownloads downloads={failedDownloads} />
      </div>
    </div>
  );
}
```

#### 3.4.3.8 Adicionar Link no Header

```tsx
// Modificar cutube-web/components/layout/header.tsx (ou equivalente)

// Adicionar link para /monitor no header/navigation
<nav className="flex items-center gap-4">
  <Link href="/" className="text-gray-600 hover:text-gray-900">Home</Link>
  <Link href="/downloads" className="text-gray-600 hover:text-gray-900">Downloads</Link>
  <Link href="/monitor" className="text-blue-600 hover:text-blue-800 font-medium">
    📊 Monitor
  </Link>
</nav>
```

**Checklist:**
- [ ] Criar interface TypeScript DownloadStatus
- [ ] Criar funções de API (getActiveDownloads, getFailedDownloads, getMetrics)
- [ ] Criar hook useMonitor com polling
- [ ] Criar componente MonitorMetrics
- [ ] Criar componente QueueStatusCard
- [ ] Criar componente ActiveQueue
- [ ] Criar componente FailedDownloads
- [ ] Criar página /app/monitor/page.tsx
- [ ] Adicionar link para /monitor no header
- [ ] Testar responsividade

**Critérios de aceite:**
- ✅ Página /monitor carrega corretamente
- ✅ Métricas exibidas em cards
- ✅ Fila ativa mostra downloads queued + processing
- ✅ Downloads falhos visíveis separadamente
- ✅ Polling atualiza dados a cada 5 segundos
- ✅ Indicador de última atualização
- ✅ Link no header para navegação fácil

---

### 3.4.4 Implementar SignalR para Monitor

**Estimativa:** 3-4 horas

**Arquivos:**
```
cutube-web/
  ├── hooks/
  │   └── useSignalR.ts
  └── components/monitor/
      └── SignalRStatus.tsx
```

#### 3.4.4.1 Criar Hook useSignalR

```typescript
// cutube-web/hooks/useSignalR.ts
'use client';

import { useState, useEffect, useRef, useCallback } from 'react';
import * as signalR from '@microsoft/signalr';

interface SignalRCallbacks {
  onDownloadStatusChanged?: (correlationId: string, newStatus: string) => void;
  onDownloadProgress?: (correlationId: string, progress: number, speed: number) => void;
  onDownloadCompleted?: (correlationId: string) => void;
  onDownloadFailed?: (correlationId: string, error: string) => void;
}

export function useSignalR(callbacks: SignalRCallbacks) {
  const [isConnected, setIsConnected] = useState(false);
  const [connectionError, setConnectionError] = useState<string | null>(null);
  const connectionRef = useRef<signalR.HubConnection | null>(null);

  useEffect(() => {
    const connection = new signalR.HubConnectionBuilder()
      .withUrl('/hubs/downloads')
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Information)
      .build();

    connectionRef.current = connection;

    connection.on('DownloadStatusChanged', (correlationId: string, newStatus: string) => {
      callbacks.onDownloadStatusChanged?.(correlationId, newStatus);
    });

    connection.on('DownloadProgress', (correlationId: string, data: { progress: number; speed: number }) => {
      callbacks.onDownloadProgress?.(correlationId, data.progress, data.speed);
    });

    connection.on('DownloadCompleted', (correlationId: string) => {
      callbacks.onDownloadCompleted?.(correlationId);
    });

    connection.on('DownloadFailed', (correlationId: string, error: string) => {
      callbacks.onDownloadFailed?.(correlationId, error);
    });

    connection
      .start()
      .then(() => {
        setIsConnected(true);
        setConnectionError(null);
      })
      .catch((err) => {
        setConnectionError(err.message);
      });

    connection.onreconnecting(() => {
      setIsConnected(false);
    });

    connection.onreconnected(() => {
      setIsConnected(true);
      setConnectionError(null);
    });

    connection.onclose(() => {
      setIsConnected(false);
    });

    return () => {
      connection.stop();
    };
  }, []);

  return { isConnected, connectionError };
}
```

#### 3.4.4.2 Adicionar SignalR ao Monitor

```tsx
// Modificar cutube-web/app/monitor/page.tsx para incluir SignalR
'use client';

import { useMonitor } from '@/hooks/useMonitor';
import { useSignalR } from '@/hooks/useSignalR';
import { MonitorMetrics } from '@/components/monitor/MonitorMetrics';
import { ActiveQueue } from '@/components/monitor/ActiveQueue';
import { FailedDownloads } from '@/components/monitor/FailedDownloads';
import { useCallback } from 'react';

export default function MonitorPage() {
  const { 
    activeDownloads, 
    failedDownloads, 
    metrics, 
    isLoading, 
    error, 
    refresh,
    lastUpdated 
  } = useMonitor(30000); // Polling mais lento (30s) quando SignalR conectado

  const handleStatusChanged = useCallback(() => {
    refresh();
  }, [refresh]);

  const { isConnected } = useSignalR({
    onDownloadStatusChanged: handleStatusChanged,
    onDownloadCompleted: handleStatusChanged,
    onDownloadFailed: handleStatusChanged
  });

  return (
    <div className="min-h-screen bg-gray-50 p-6">
      <div className="max-w-7xl mx-auto">
        {/* Header com status de conexão */}
        <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center mb-6 gap-4">
          <div>
            <h1 className="text-3xl font-bold text-gray-800">Monitor</h1>
            <p className="text-gray-500">
              System monitoring and queue status
              {isConnected ? (
                <span className="ml-2 text-green-600 font-medium">● Live</span>
              ) : (
                <span className="ml-2 text-yellow-600">○ Polling</span>
              )}
              {lastUpdated && (
                <span className="ml-2 text-xs text-gray-400">
                  Updated: {lastUpdated.toLocaleTimeString()}
                </span>
              )}
            </p>
          </div>
          <button
            onClick={refresh}
            disabled={isLoading}
            className="px-4 py-2 bg-blue-600 text-white rounded hover:bg-blue-700 disabled:opacity-50 flex items-center gap-2"
          >
            {isLoading ? '🔄 Refreshing...' : '🔄 Refresh'}
          </button>
        </div>

        {/* Resto do componente... */}
        <MonitorMetrics metrics={metrics} />
        <div className="mb-8">
          <ActiveQueue downloads={activeDownloads} />
        </div>
        <FailedDownloads downloads={failedDownloads} />
      </div>
    </div>
  );
}
```

**Checklist:**
- [ ] Instalar pacote @microsoft/signalr
- [ ] Criar hook useSignalR
- [ ] Implementar conexão SignalR no monitor
- [ ] Adicionar indicador Live/Polling
- [ ] Ajustar polling para 30s quando SignalR conectado

**Critérios de aceite:**
- ✅ Página conecta via SignalR
- ✅ Updates em tempo real funcionam
- ✅ Fallback para polling quando desconectado
- ✅ Indicador visual de estado da conexão

---

## Qualidade Gates - Fase 3.4

**ANTES de considerar esta fase completa, TODOS os itens abaixo devem ser verdadeiros:**

- [ ] **dotnet build** - **0 warnings**
- [ ] **dotnet test** - **100% pass**
- [ ] **npm run build** (frontend) - Build sem erros
- [ ] Repositório thread-safe
- [ ] Cleanup service funciona
- [ ] Endpoints de status funcionam
- [ ] Página /monitor acessível
- [ ] Link no header funciona
- [ ] Métricas calculadas corretamente
- [ ] Polling atualiza dados
- [ ] SignalR funciona (quando disponível)
- [ ] Responsivo em todos tamanhos

---

## Cronograma Detalhado

| Tarefa | Estimativa | Dependencies | Blocker |
|--------|-----------|--------------|---------|
| 3.4.1 DownloadStatusRepository | 4-5h | Nenhuma | Não |
| 3.4.2 Status Endpoints | 4-5h | 3.4.1 | **Sim** |
| 3.4.3 Página /monitor | 6-8h | 3.4.2 | **Sim** |
| 3.4.4 SignalR Monitor | 3-4h | 3.4.3 | **Sim** |
| Testes e Ajustes | 2-3h | Todas | **Sim** |

**Total:** 19-25 horas (~4-5 dias)

---

## Tecnologias

### Backend (.NET 10)
- `ConcurrentDictionary` - Thread-safe storage
- `BackgroundService` - Cleanup
- Minimal APIs - Endpoints

### Frontend (Next.js 16.1.6)
- React Hooks
- Tailwind CSS
- @microsoft/signalr

---

## Documentação

### API Endpoints

| Método | Endpoint | Descrição |
|--------|----------|-----------|
| GET | /api/downloads | Listar downloads |
| GET | /api/downloads/{id} | Buscar por ID |
| GET | /api/downloads/queue/active | Downloads ativos |
| GET | /api/downloads/queue/failed | Downloads falhos |
| PATCH | /api/downloads/{id}/status | Atualizar status |
| POST | /api/downloads/{id}/progress | Atualizar progresso |
| GET | /api/metrics | Métricas do sistema |

### Rotas do Frontend

| Rota | Descrição |
|------|-----------|
| / | Home - Criar downloads |
| /downloads | Lista de downloads |
| /downloads/{id} | Detalhes do download |
| /monitor | **NOVO** - Monitoramento do sistema |
| /queue | Status da fila RabbitMQ |

---

**Fim do Plano - Fase 3.4**
