# Fase 2.3: WebSocket para Progresso em Tempo Real

**Status:** 🎯 Planejamento
**Épico:** Cutube-2i6 (Épico 2: Arquitetura Híbrida CLI + API)
**Duração:** 3-4 dias
**Responsável:** Backend Developer
**Prioridade:** 🔥 Alta
**Dependência:** ✅ Fase 2.2 completa (REST API)

---

## Objetivo

Implementar comunicação bidirecional em tempo real via SignalR para enviar atualizações de progresso de downloads do backend para o frontend sem polling.

**Benefícios:**
- Atualizações de progresso em tempo real (< 100ms latência)
- Múltiplos clients podem observar o mesmo download
- Reconnection automática em caso de falha de rede
- Reduz carga no servidor (sem polling a cada 1-2s)
- Escalabilidade via SignalR scaling (Redis backplane futuro)

---

## Visão Arquitetural

```
┌─────────────────────────────────────────────────────────────┐
│                     Frontend Layer                          │
│                   Next.js + SignalR Client                  │
│                                                             │
│  - WebSocket connection to /hubs/downloads                 │
│  - Join groups: "download:{downloadId}"                    │
│  - Receive events: Progress, Completed, Failed             │
└──────────────────────┬──────────────────────────────────────┘
                       │ SignalR Protocol
                       │ (WebSocket, Server-Sent Events)
                       ▼
┌─────────────────────────────────────────────────────────────┐
│                  SignalR Hub Layer                          │
│              Cutube.Api/Hubs/DownloadHub                    │
│                                                             │
│  - OnConnected: Track connection                           │
│  - JoinGroup/LeaveGroup: Manage download groups            │
│  - IDownloadHubClient: Typed hub interface                 │
└──────────────────────┬──────────────────────────────────────┘
                       │ IHubContext injection
                       ▼
┌─────────────────────────────────────────────────────────────┐
│              Background Processing Layer                    │
│           Cutube.Api/Services/BackgroundDownloadWorker     │
│                                                             │
│  - Process items from DownloadQueue                        │
│  - Inject IHubContext<DownloadHub>                         │
│  - Broadcast events to groups                              │
│  - Started, Progress, Completed, Failed events             │
└─────────────────────────────────────────────────────────────┘
```

**SignalR Events Flow:**

```
Download Created
    │
    ├─> REST Response: 202 Accepted + downloadId
    │
    └─> SignalR Event: DownloadStarted
            (Group: "download:{id}")
            │
            ├─> Progress Updates (every 1-2s)
            │   └─> SignalR Event: DownloadProgress
            │       (Progress %, Speed, ETA)
            │
            ├─> Processing (ffmpeg)
            │   └─> SignalR Event: DownloadProgress
            │       (Status: "processing")
            │
            └─> Download Complete
                └─> SignalR Event: DownloadCompleted
                    (FilePath, Size, Duration)
```

---

## Tarefas

### 2.3.1 Criar SignalR Hub e interfaces

**Estimativa:** 3 horas
**Arquivos:**
```
src/Cutube.Api/Hubs/
  ├── IDownloadHubClient.cs           - Typed hub interface (client methods)
  ├── DownloadHub.cs                  - SignalR Hub implementation
  └── DownloadHubEvents.cs            - Event DTOs
```

**IDownloadHubClient (Typed Hub):**

```csharp
// src/Cutube.Api/Hubs/IDownloadHubClient.cs
using Microsoft.AspNetCore.SignalR;

namespace Cutube.Api.Hubs;

/// <summary>
/// Interface typed para métodos do cliente SignalR
/// Define quais métodos o cliente pode receber do servidor
/// </summary>
public interface IDownloadHubClient
{
    /// <summary>
    /// Cliente recebe evento quando download inicia
    /// </summary>
    Task DownloadStarted(string downloadId, DownloadStartedEvent data);

    /// <summary>
    /// Cliente recebe atualização de progresso
    /// </summary>
    Task DownloadProgress(string downloadId, DownloadProgressEvent data);

    /// <summary>
    /// Cliente recebe evento quando download completa processamento
    /// </summary>
    Task DownloadCompleted(string downloadId, DownloadCompletedEvent data);

    /// <summary>
    /// Cliente recebe evento quando download falha
    /// </summary>
    Task DownloadFailed(string downloadId, DownloadFailedEvent data);

    /// <summary>
    /// Cliente recebe evento quando download é cancelado
    /// </summary>
    Task DownloadCancelled(string downloadId);
}
```

**DownloadHub Implementation:**

```csharp
// src/Cutube.Api/Hubs/DownloadHub.cs
using Microsoft.AspNetCore.SignalR;

namespace Cutube.Api.Hubs;

/// <summary>
/// SignalR Hub para comunicação em tempo real de downloads
/// Clients podem entrar/sair de grupos específicos por downloadId
/// </summary>
public class DownloadHub : Hub<IDownloadHubClient>
{
    private readonly ILogger<DownloadHub> _logger;

    public DownloadHub(ILogger<DownloadHub> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Cliente conecta-se e entra no grupo de um download específico
    /// </summary>
    /// <param name="downloadId">ID do download a observar</param>
    public async Task JoinDownloadGroup(string downloadId)
    {
        var groupName = GetDownloadGroupName(downloadId);
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

        _logger.LogInformation(
            "Connection {ConnectionId} joined group {GroupName} for download {DownloadId}",
            Context.ConnectionId, groupName, downloadId);
    }

    /// <summary>
    /// Cliente sai do grupo de um download (para de receber updates)
    /// </summary>
    /// <param name="downloadId">ID do download a parar de observar</param>
    public async Task LeaveDownloadGroup(string downloadId)
    {
        var groupName = GetDownloadGroupName(downloadId);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);

        _logger.LogInformation(
            "Connection {ConnectionId} left group {GroupName} for download {DownloadId}",
            Context.ConnectionId, groupName, downloadId);
    }

    /// <summary>
    /// Override chamado quando cliente conecta
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Override chamado quando cliente desconecta
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (exception is not null)
        {
            _logger.LogError(exception,
                "Client disconnected with error: {ConnectionId}",
                Context.ConnectionId);
        }
        else
        {
            _logger.LogInformation("Client disconnected: {ConnectionId}", Context.ConnectionId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Gera nome de grupo SignalR para um download
    /// </summary>
    public static string GetDownloadGroupName(string downloadId)
    {
        return $"download:{downloadId}";
    }
}
```

**Event DTOs:**

```csharp
// src/Cutube.Api/Hubs/DownloadHubEvents.cs
namespace Cutube.Api.Hubs;

/// <summary>
/// Evento disparado quando download inicia
/// </summary>
public record DownloadStartedEvent(
    string DownloadId,
    string Url,
    DateTime StartedAt
);

/// <summary>
/// Evento disparado periodicamente com progresso
/// </summary>
public record DownloadProgressEvent(
    string DownloadId,
    double Progress,          // 0-100
    double Speed,             // bytes/s
    string? Eta,              // HH:MM:SS format
    long DownloadedBytes,
    long TotalBytes,
    string Status             // "downloading", "processing"
);

/// <summary>
/// Evento disparado quando download completa com sucesso
/// </summary>
public record DownloadCompletedEvent(
    string DownloadId,
    string FilePath,
    long Size,
    TimeSpan Duration,
    DateTime CompletedAt
);

/// <summary>
/// Evento disparado quando download falha
/// </summary>
public record DownloadFailedEvent(
    string DownloadId,
    string Error,
    DateTime FailedAt
);
```

**Checklist:**
- [ ] Criar interface IDownloadHubClient com todos métodos de cliente
- [ ] Criar Hub DownloadHub herdando de Hub<IDownloadHubClient>
- [ ] Implementar JoinDownloadGroup/LeaveDownloadGroup
- [ ] Criar DTOs de evento (Started, Progress, Completed, Failed)
- [ ] Adicionar logging em todos métodos
- [ ] Compilar sem erros

**Critérios de aceito:**
- ✅ Hub compilado sem erros
- ✅ Interface typed definida corretamente
- ✅ Event DTOs com shape correto
- ✅ Logging configurado

---

### 2.3.2 Configurar SignalR no API

**Estimativa:** 2 horas
**Arquivo:** `src/Cutube.Api/Program.cs`

**Configuração:**

```csharp
// Program.cs - Adicionar SignalR services
var builder = WebApplication.CreateBuilder(args);

// SignalR Configuration
builder.Services.AddSignalR(options =>
{
    // Keep-alive interval: servidor envia ping a cada 10s
    // para detectar conexões mortas
    options.KeepAliveInterval = TimeSpan.FromSeconds(10);

    // Client timeout: se cliente não responder em 30s, desconecta
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);

    // Handshake timeout: tempo máximo para handshake inicial
    options.HandshakeTimeout = TimeSpan.FromSeconds(15);

    // Maximum message size (para progress events grandes)
    options.MaximumReceiveMessageSize = 1024 * 1024; // 1MB
});

// Adicionar CORS para SignalR (necessário para WebSocket)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSignalR", policy =>
    {
        policy.WithOrigins(
                "http://localhost:3000",
                "http://localhost:3001",
                "https://cutube.dev"    // Produção
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();       // Necessário para WebSocket
    });
});

var app = builder.Build();

// Pipeline
app.UseCors("AllowSignalR");

// Mapear Hub endpoint
app.MapHub<DownloadHub>("/hubs/downloads");

// REST endpoints continuam funcionando normalmente
app.MapDownloadsEndpoints();
app.MapVideosEndpoints();
app.MapHealthChecks();

app.Run();
```

**Teste de configuração:**

```bash
# Verificar se WebSocket endpoint responde
curl -i -N \
  -H "Connection: Upgrade" \
  -H "Upgrade: websocket" \
  -H "Sec-WebSocket-Version: 13" \
  -H "Sec-WebSocket-Key: test" \
  http://localhost:5000/hubs/downloads

# Deve retornar:
# HTTP/1.1 101 Switching Protocols
# Upgrade: websocket
# Connection: Upgrade
```

**Checklist:**
- [ ] Adicionar SignalR services no DI container
- [ ] Configurar keep-alive, timeout, handshake
- [ ] Adicionar CORS policy com AllowCredentials
- [ ] Mapear Hub endpoint: /hubs/downloads
- [ ] Verificar que REST endpoints ainda funcionam
- [ ] Testar WebSocket handshake com curl

**Critérios de aceito:**
- ✅ SignalR configurado corretamente
- ✅ Endpoint /hubs/downloads acessível via WebSocket
- ✅ CORS permite conexões do frontend
- ✅ REST endpoints não afetados

---

### 2.3.3 Integrar Hub com BackgroundDownloadWorker

**Estimativa:** 4 horas
**Arquivo:** `src/Cutube.Api/Services/BackgroundDownloadWorker.cs`

**Objetivo:** Injetar `IHubContext<DownloadHub>` no worker e enviar eventos SignalR durante o processamento do download.

**Implementação:**

```csharp
// src/Cutube.Api/Services/BackgroundDownloadWorker.cs
using Cutube.Api.Hubs;
using Cutube.Domain.Models;
using Cutube.Domain.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Cutube.Api.Services;

public class BackgroundDownloadWorker : BackgroundService
{
    private readonly IDownloadQueue _queue;
    private readonly IDownloadService _downloadService;
    private readonly IDownloadStatusRepository _statusRepository;
    private readonly IHubContext<DownloadHub, IDownloadHubClient> _hub;
    private readonly ILogger<BackgroundDownloadWorker> _logger;

    public BackgroundDownloadWorker(
        IDownloadQueue queue,
        IDownloadService downloadService,
        IDownloadStatusRepository statusRepository,
        IHubContext<DownloadHub, IDownloadHubClient> hub,
        ILogger<BackgroundDownloadWorker> logger)
    {
        _queue = queue;
        _downloadService = downloadService;
        _statusRepository = statusRepository;
        _hub = hub;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("BackgroundDownloadWorker started");

        await foreach (var (downloadId, request) in _queue.DequeueAllAsync(stoppingToken))
        {
            _logger.LogInformation("Processing download {DownloadId} for URL {Url}",
                downloadId, request.Url);

            try
            {
                // 1. Notificar: Download Started
                await NotifyStartedAsync(downloadId, request);

                // 2. Inicializar status no repository
                await _statusRepository.AddAsync(downloadId, new Api.Models.DownloadStatus
                {
                    Id = downloadId,
                    Url = request.Url,
                    Status = Domain.Models.DownloadStatus.Queued,
                    Progress = 0,
                    CreatedAt = DateTime.UtcNow
                });

                // 3. Criar progress reporter que:
                //    a) Atualiza repository
                //    b) Envia SignalR events
                var progress = new Progress<Domain.Models.DownloadProgress>(async p =>
                {
                    await UpdateProgressAsync(downloadId, p);
                });

                // 4. Executar download via Domain Service
                var result = await _downloadService.DownloadAsync(
                    request,
                    progress,
                    stoppingToken);

                // 5. Notificar resultado
                if (result.IsFailure)
                {
                    await NotifyFailedAsync(downloadId, result.Errors.First().Message);
                }
                else
                {
                    await NotifyCompletedAsync(downloadId, result.Value);
                }
            }
            catch (OperationCanceledException)
            {
                await NotifyCancelledAsync(downloadId);
                _logger.LogInformation("Download {DownloadId} was cancelled", downloadId);
            }
            catch (Exception ex)
            {
                await NotifyFailedAsync(downloadId, ex.Message);
                _logger.LogError(ex, "Download {DownloadId} failed with exception", downloadId);
            }
        }
    }

    /// <summary>
    /// Notifica clientes conectados que download iniciou
    /// </summary>
    private async Task NotifyStartedAsync(string downloadId, Domain.Models.DownloadRequest request)
    {
        var groupName = DownloadHub.GetDownloadGroupName(downloadId);
        var eventData = new DownloadStartedEvent(
            downloadId,
            request.Url,
            DateTime.UtcNow
        );

        await _hub.Clients.Group(groupName)
            .DownloadStarted(downloadId, eventData);

        _logger.LogDebug("Sent DownloadStarted event for {DownloadId} to group {GroupName}",
            downloadId, groupName);
    }

    /// <summary>
    /// Atualiza progresso: repository + SignalR
    /// </summary>
    private async Task UpdateProgressAsync(string downloadId, Domain.Models.DownloadProgress domainProgress)
    {
        // 1. Atualizar repository (para REST GET /api/downloads/{id})
        var apiProgress = new Api.Models.DownloadProgress
        {
            DownloadId = downloadId,
            Percentage = domainProgress.Percentage,
            Speed = domainProgress.Speed,
            Eta = domainProgress.Eta?.ToString("hh\\:mm\\:ss"),
            DownloadedBytes = domainProgress.DownloadedBytes,
            TotalBytes = domainProgress.TotalBytes,
            Status = domainProgress.Status
        };

        await _statusRepository.UpdateProgressAsync(downloadId, apiProgress);

        // 2. Enviar SignalR event para clients conectados
        var groupName = DownloadHub.GetDownloadGroupName(downloadId);
        var eventData = new DownloadProgressEvent(
            downloadId,
            domainProgress.Percentage,
            domainProgress.Speed,
            domainProgress.Eta?.ToString("hh\\:mm\\:ss"),
            domainProgress.DownloadedBytes,
            domainProgress.TotalBytes,
            domainProgress.Status.ToString().ToLowerInvariant()
        );

        await _hub.Clients.Group(groupName)
            .DownloadProgress(downloadId, eventData);

        _logger.LogDebug("Sent DownloadProgress event for {DownloadId}: {Progress}%",
            downloadId, domainProgress.Percentage);
    }

    /// <summary>
    /// Notifica clientes que download completou
    /// </summary>
    private async Task NotifyCompletedAsync(string downloadId, Domain.Models.DownloadResult result)
    {
        var groupName = DownloadHub.GetDownloadGroupName(downloadId);
        var eventData = new DownloadCompletedEvent(
            downloadId,
            result.FilePath,
            result.Size,
            result.Duration,
            DateTime.UtcNow
        );

        await _hub.Clients.Group(groupName)
            .DownloadCompleted(downloadId, eventData);

        _logger.LogInformation("Sent DownloadCompleted event for {DownloadId}", downloadId);
    }

    /// <summary>
    /// Notifica clientes que download falhou
    /// </summary>
    private async Task NotifyFailedAsync(string downloadId, string error)
    {
        var groupName = DownloadHub.GetDownloadGroupName(downloadId);
        var eventData = new DownloadFailedEvent(
            downloadId,
            error,
            DateTime.UtcNow
        );

        await _hub.Clients.Group(groupName)
            .DownloadFailed(downloadId, eventData);

        _logger.LogError("Sent DownloadFailed event for {DownloadId}: {Error}",
            downloadId, error);
    }

    /// <summary>
    /// Notifica clientes que download foi cancelado
    /// </summary>
    private async Task NotifyCancelledAsync(string downloadId)
    {
        var groupName = DownloadHub.GetDownloadGroupName(downloadId);

        await _hub.Clients.Group(groupName)
            .DownloadCancelled(downloadId);

        _logger.LogInformation("Sent DownloadCancelled event for {DownloadId}", downloadId);
    }
}
```

**Otimização - Throttle de eventos:**

Para não inundar o cliente com updates (yt-dlp pode reportar progresso a cada 100ms):

```csharp
// Adicionar no BackgroundDownloadWorker
private readonly ConcurrentDictionary<string, DateTime> _lastProgressUpdate = new();

private async Task UpdateProgressAsync(string downloadId, Domain.Models.DownloadProgress domainProgress)
{
    // Throttle: enviar no máximo 1 update por segundo
    var now = DateTime.UtcNow;
    if (_lastProgressUpdate.TryGetValue(downloadId, out var lastUpdate))
    {
        var timeSinceLastUpdate = (now - lastUpdate).TotalMilliseconds;
        if (timeSinceLastUpdate < 1000 && domainProgress.Percentage < 100)
        {
            // Skip update (ainda não passou 1s)
            return;
        }
    }

    _lastProgressUpdate[downloadId] = now;

    // Resto da implementação...
    await _statusRepository.UpdateProgressAsync(downloadId, apiProgress);
    await _hub.Clients.Group(groupName).DownloadProgress(downloadId, eventData);
}
```

**Checklist:**
- [ ] Injetar IHubContext<DownloadHub, IDownloadHubClient> no worker
- [ ] Implementar NotifyStartedAsync
- [ ] Implementar UpdateProgressAsync (com throttle opcional)
- [ ] Implementar NotifyCompletedAsync
- [ ] Implementar NotifyFailedAsync
- [ ] Implementar NotifyCancelledAsync
- [ ] Adicionar logging para todos eventos
- [ ] Testar com cliente SignalR conectado

**Critérios de aceito:**
- ✅ DownloadStarted event enviado ao iniciar
- ✅ DownloadProgress events enviados durante download
- ✅ DownloadCompleted/Failed events enviados ao finalizar
- ✅ Múltiplos clients recebem mesmos eventos
- ✅ Performance OK (sem flood de mensagens)

---

### 2.3.4 Connection Lifecycle Management

**Estimativa:** 3 horas
**Arquivos:**
```
src/Cutube.Api/Hubs/
  └── ConnectionTracker.cs           - Rastreia conexões ativas

src/Cutube.Api/Services/
  └── ConnectionCleanupService.cs    - HostedService para limpeza
```

**Objetivo:** Rastrear quais clients estão conectados a quais downloads para:
- Evitar enviar eventos para grupos vazios
- Limpar grupos de downloads finalizados
- Monitorar número de conexões ativas
- Logging e debugging

**ConnectionTracker:**

```csharp
// src/Cutube.Api/Hubs/ConnectionTracker.cs
using System.Collections.Concurrent;

namespace Cutube.Api.Hubs;

/// <summary>
/// Rastreia conexões SignalR ativas e seus grupos de downloads
/// Thread-safe para uso em ambiente multi-threaded
/// </summary>
public class ConnectionTracker
{
    private readonly ConcurrentDictionary<string, HashSet<string>> _downloadConnections = new();
    private readonly ConcurrentDictionary<string, HashSet<string>> _connectionDownloads = new();
    private readonly ILogger<ConnectionTracker> _logger;

    public ConnectionTracker(ILogger<ConnectionTracker> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Registra que uma conexão entrou no grupo de um download
    /// </summary>
    public void AddConnection(string downloadId, string connectionId)
    {
        // Adicionar connection ao grupo do download
        _downloadConnections.AddOrUpdate(
            downloadId,
            _ => new HashSet<string> { connectionId },
            (_, connections) =>
            {
                lock (connections)
                {
                    connections.Add(connectionId);
                }
                return connections;
            });

        // Adicionar download à lista da conexão (para disconnect)
        _connectionDownloads.AddOrUpdate(
            connectionId,
            _ => new HashSet<string> { downloadId },
            (_, downloads) =>
            {
                lock (downloads)
                {
                    downloads.Add(downloadId);
                }
                return downloads;
            });

        _logger.LogDebug(
            "Connection {ConnectionId} joined download {DownloadId} (total connections: {Count})",
            connectionId, downloadId, _downloadConnections.TryGetValue(downloadId, out var conns) ? conns.Count : 0);
    }

    /// <summary>
    /// Remove uma conexão do grupo de um download
    /// </summary>
    public void RemoveConnection(string downloadId, string connectionId)
    {
        // Remover connection do grupo do download
        if (_downloadConnections.TryGetValue(downloadId, out var connections))
        {
            lock (connections)
            {
                connections.Remove(connectionId);
            }

            if (connections.Count == 0)
            {
                _downloadConnections.TryRemove(downloadId, out _);
                _logger.LogDebug("Download {DownloadId} has no more active connections", downloadId);
            }
        }

        // Remover download da lista da conexão
        if (_connectionDownloads.TryGetValue(connectionId, out var downloads))
        {
            lock (downloads)
            {
                downloads.Remove(downloadId);
            }

            if (downloads.Count == 0)
            {
                _connectionDownloads.TryRemove(connectionId, out _);
            }
        }

        _logger.LogDebug(
            "Connection {ConnectionId} left download {DownloadId}",
            connectionId, downloadId);
    }

    /// <summary>
    /// Remove todas as associações de uma conexão (quando cliente desconecta)
    /// </summary>
    public void RemoveConnectionAll(string connectionId)
    {
        if (!_connectionDownloads.TryRemove(connectionId, out var downloads))
            return;

        lock (downloads)
        {
            foreach (var downloadId in downloads)
            {
                RemoveConnection(downloadId, connectionId);
            }
        }

        _logger.LogInformation("Connection {ConnectionId} fully removed from tracker", connectionId);
    }

    /// <summary>
    /// Verifica se um download tem conexões ativas
    /// </summary>
    public bool HasActiveConnections(string downloadId)
    {
        return _downloadConnections.TryGetValue(downloadId, out var connections) &&
               connections.Count > 0;
    }

    /// <summary>
    /// Retorna número de conexões ativas para um download
    /// </summary>
    public int GetActiveConnectionCount(string downloadId)
    {
        if (_downloadConnections.TryGetValue(downloadId, out var connections))
        {
            lock (connections)
            {
                return connections.Count;
            }
        }
        return 0;
    }

    /// <summary>
    /// Retorna estatísticas gerais
    /// </summary>
    public (int TotalDownloads, int TotalConnections) GetStats()
    {
        return (_downloadConnections.Count, _connectionDownloads.Count);
    }
}
```

**Integrar com DownloadHub:**

```csharp
// src/Cutube.Api/Hubs/DownloadHub.cs - atualizar
public class DownloadHub : Hub<IDownloadHubClient>
{
    private readonly ILogger<DownloadHub> _logger;
    private readonly ConnectionTracker _connectionTracker;

    public DownloadHub(
        ILogger<DownloadHub> logger,
        ConnectionTracker connectionTracker)
    {
        _logger = logger;
        _connectionTracker = connectionTracker;
    }

    public async Task JoinDownloadGroup(string downloadId)
    {
        var groupName = GetDownloadGroupName(downloadId);
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

        // Rastrear conexão
        _connectionTracker.AddConnection(downloadId, Context.ConnectionId);

        _logger.LogInformation(
            "Connection {ConnectionId} joined group {GroupName} for download {DownloadId}",
            Context.ConnectionId, groupName, downloadId);
    }

    public async Task LeaveDownloadGroup(string downloadId)
    {
        var groupName = GetDownloadGroupName(downloadId);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);

        // Remover rastreamento
        _connectionTracker.RemoveConnection(downloadId, Context.ConnectionId);

        _logger.LogInformation(
            "Connection {ConnectionId} left group {GroupName} for download {DownloadId}",
            Context.ConnectionId, groupName, downloadId);
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        // Remover todas as associações desta conexão
        _connectionTracker.RemoveConnectionAll(Context.ConnectionId);

        if (exception is not null)
        {
            _logger.LogError(exception,
                "Client disconnected with error: {ConnectionId}",
                Context.ConnectionId);
        }
        else
        {
            _logger.LogInformation("Client disconnected: {ConnectionId}", Context.ConnectionId);
        }

        await base.OnDisconnectedAsync(exception);
    }
}
```

**Checklist:**
- [ ] Criar ConnectionTracker com thread-safe operations
- [ ] Implementar AddConnection/RemoveConnection
- [ ] Implementar RemoveConnectionAll (disconnect)
- [ ] Implementar HasActiveConnections
- [ ] Registrar ConnectionTracker no DI container
- [ ] Integrar com DownloadHub (OnConnected/OnDisconnected)
- [ ] Adicionar logging
- [ ] Testar com múltiplos connects/disconnects

**Critérios de aceito:**
- ✅ Conexões rastreadas corretamente
- ✅ Disconnect limpa todos grupos
- ✅ Thread-safe (testar com múltiplas conexões simultâneas)
- ✅ Estatísticas disponíveis (logging)

---

### 2.3.5 Testes de Integração SignalR

**Estimativa:** 3 horas
**Arquivos:**
```
tests/Cutube.Api.Tests/SignalR/
  ├── SignalRTests.cs              - Testes de integração SignalR
  └── TestDownloadHubClient.cs     - Client mock para testes
```

**TestDownloadHubClient (Mock Client):**

```csharp
// tests/Cutube.Api.Tests/SignalR/TestDownloadHubClient.cs
using Cutube.Api.Hubs;

namespace Cutube.Api.Tests.SignalR;

/// <summary>
/// Mock client para testar SignalR Hub
/// Simula o comportamento de um cliente real conectado
/// </summary>
public class TestDownloadHubClient : IDownloadHubClient
{
    private readonly Dictionary<string, List<object>> _receivedEvents = new();

    public IReadOnlyDictionary<string, List<object>> ReceivedEvents => _receivedEvents;

    // IDownloadHubClient implementation (chamado pelo servidor)
    public Task DownloadStarted(string downloadId, DownloadStartedEvent data)
    {
        AddEvent("DownloadStarted", new { downloadId, data });
        return Task.CompletedTask;
    }

    public Task DownloadProgress(string downloadId, DownloadProgressEvent data)
    {
        AddEvent("DownloadProgress", new { downloadId, data });
        return Task.CompletedTask;
    }

    public Task DownloadCompleted(string downloadId, DownloadCompletedEvent data)
    {
        AddEvent("DownloadCompleted", new { downloadId, data });
        return Task.CompletedTask;
    }

    public Task DownloadFailed(string downloadId, DownloadFailedEvent data)
    {
        AddEvent("DownloadFailed", new { downloadId, data });
        return Task.CompletedTask;
    }

    public Task DownloadCancelled(string downloadId)
    {
        AddEvent("DownloadCancelled", new { downloadId });
        return Task.CompletedTask;
    }

    private void AddEvent(string eventType, object data)
    {
        if (!_receivedEvents.ContainsKey(eventType))
            _receivedEvents[eventType] = new List<object>();

        _receivedEvents[eventType].Add(data);
    }

    /// <summary>
    /// Limpa todos eventos recebidos
    /// </summary>
    public void Clear()
    {
        _receivedEvents.Clear();
    }

    /// <summary>
    /// Retorna número de eventos de um tipo recebidos
    /// </summary>
    public int GetEventCount(string eventType)
    {
        return _receivedEvents.TryGetValue(eventType, out var events) ? events.Count : 0;
    }
}
```

**SignalR Integration Tests:**

```csharp
// tests/Cutube.Api.Tests/SignalR/SignalRTests.cs
using FluentAssertions;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Cutube.Api.Hubs;
using Moq;

namespace Cutube.Api.Tests.SignalR;

public class SignalRTests : IClassFixture<SignalRTestFixture>
{
    private readonly SignalRTestFixture _fixture;

    public SignalRTests(SignalRTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task JoinDownloadGroup_ShouldTrackConnection()
    {
        // Arrange
        var hub = _fixture.GetHub();
        var connectionId = "test-connection-1";
        var downloadId = "download-123";

        hub.Context = new HubCallerContextFixture { ConnectionId = connectionId };

        // Act
        await hub.JoinDownloadGroup(downloadId);

        // Assert
        var tracker = _fixture.GetConnectionTracker();
        tracker.HasActiveConnections(downloadId).Should().BeTrue();
        tracker.GetActiveConnectionCount(downloadId).Should().Be(1);
    }

    [Fact]
    public async Task LeaveDownloadGroup_ShouldRemoveTracking()
    {
        // Arrange
        var hub = _fixture.GetHub();
        var connectionId = "test-connection-2";
        var downloadId = "download-456";

        hub.Context = new HubCallerContextFixture { ConnectionId = connectionId };
        await hub.JoinDownloadGroup(downloadId);

        // Act
        await hub.LeaveDownloadGroup(downloadId);

        // Assert
        var tracker = _fixture.GetConnectionTracker();
        tracker.HasActiveConnections(downloadId).Should().BeFalse();
        tracker.GetActiveConnectionCount(downloadId).Should().Be(0);
    }

    [Fact]
    public async Task BackgroundWorker_ShouldSendSignalREvents()
    {
        // Arrange
        var downloadId = "download-test-1";
        var mockClient = new Mock<IDownloadHubClient>();
        var mockClients = new Mock<IHubContext<DownloadHub, IDownloadHubClient>>();

        // Setup mock clients
        var mockGroup = new Mock<IClientProxy>();
        mockClients
            .Setup(x => x.Clients.Group(It.IsAny<string>()))
            .Returns(mockGroup.Object);

        // Act
        // Simular BackgroundDownloadWorker enviando eventos
        // (em testes reais, usaríamos WebApplicationFactory e Worker real)

        // Assert
        mockGroup.Verify(x => x.SendCoreAsync(
            "DownloadStarted",
            It.Is<object[]>(args => args.Length == 2),
            default), Times.Once);
    }
}

/// <summary>
/// Fixture para configurar ambiente de teste SignalR
/// </summary>
public class SignalRTestFixture : IDisposable
{
    private readonly ServiceProvider _serviceProvider;

    public SignalRTestFixture()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ConnectionTracker>();
        services.AddSingleton<DownloadHub>();

        _serviceProvider = services.BuildServiceProvider();
    }

    public DownloadHub GetHub()
    {
        return _serviceProvider.GetRequiredService<DownloadHub>();
    }

    public ConnectionTracker GetConnectionTracker()
    {
        return _serviceProvider.GetRequiredService<ConnectionTracker>();
    }

    public void Dispose()
    {
        _serviceProvider?.Dispose();
    }
}

/// <summary>
/// Mock HubCallerContext para testes
/// </summary>
public class HubCallerContextFixture : HubCallerContext
{
    public override string ConnectionId { get; set; } = "test-connection";
    public override string UserIdentifier { get; set; } = "test-user";
    public override System.Security.Claims.ClaimsPrincipal User { get; set; } = new();
    public override IDictionary<object, object> Items { get; } = new Dictionary<object, object>();
    public override IFeatureCollection Features { get; } = new FeatureCollection();
}
```

**Checklist:**
- [ ] Criar TestDownloadHubClient (mock client)
- [ ] Criar SignalRTestFixture
- [ ] Criar testes para JoinDownloadGroup
- [ ] Criar testes para LeaveDownloadGroup
- [ ] Criar testes para OnDisconnected
- [ ] Criar testes para ConnectionTracker
- [ ] Testar com WebApplicationFactory (end-to-end)
- [ ] Verificar que eventos SignalR são enviados corretamente

**Critérios de aceito:**
- ✅ Todos testes passam
- ✅ Conexões rastreadas corretamente
- ✅ Events enviados para grupos corretos
- ✅ OnDisconnected limpa grupos

---

## Qualidade Gates - Fase 2.3

**ANTES de passar para Fase 2.4, TODOS os itens abaixo devem ser concluídos:**

- [ ] **dotnet build** - **ZERO warnings** em todos projetos
- [ ] **dotnet test** - **100% pass** em todos projetos (incluindo SignalR tests)
- [ ] SignalR Hub configurado e acessível em /hubs/downloads
- [ ] WebSocket handshake funcionando (teste com curl/Postman)
- [ ] BackgroundDownloadWorker enviando eventos SignalR
- [ ] ConnectionTracker rastreando conexões corretamente
- [ ] Testes de integração SignalR passando
- [ ] Code review aprovado

---

## Cronograma Detalhado

| Tarefa | Estimativa | Dependencies | Blocker |
|--------|-----------|--------------|---------|
| 2.3.1 Criar Hub e interfaces | 3h | Fase 2.2 | Não |
| 2.3.2 Configurar SignalR | 2h | 2.3.1 | Não |
| 2.3.3 Integrar com Worker | 4h | 2.3.1, 2.3.2 | **Sim** |
| 2.3.4 Connection Lifecycle | 3h | 2.3.1 | Não |
| 2.3.5 Testes SignalR | 3h | 2.3.3 | **Sim** |

**Total:** 15 horas (3-4 dias)

---

## Tecnologias

- **ASP.NET Core SignalR** - Framework WebSocket
- **Typed Hubs** - Interface strongly-typed para clientes
- **IHubContext** - Injeção para enviar eventos de fora do Hub
- **System.Threading.Channels** - DownloadQueue (já existente)
- **ConcurrentDictionary** - Thread-safe connection tracking

---

## Próximos Passos

Após completar Fase 2.3:

1. ✅ **Fase 2.4**: Criar Frontend Next.js
   - Configurar SignalR client (@microsoft/signalr)
   - Conectar ao Hub
   - Receber eventos de progresso
   - Atualizar UI em tempo real

2. 🎯 **Testes End-to-End**
   - Criar download via REST
   - Conectar WebSocket
   - Receber todos eventos
   - Verificar progresso em tempo real

3. 📝 **Documentação**
   - Documentar SignalR events
   - Exemplos de client code
   - Troubleshooting comum

---

## Troubleshooting Comum

**Problema: WebSocket connection fails**

```
Error: WebSocket connection to 'ws://localhost:5000/hubs/downloads' failed
```

**Solução:**
1. Verificar se CORS está configurado com `AllowCredentials()`
2. Verificar se SignalR está mapeado antes de outros middlewares
3. Verificar firewall/proxy não bloqueando WebSockets

**Problema: Client não recebe eventos**

```
Expected: DownloadProgress event
Actual: No events received
```

**Solução:**
1. Verificar se client chamou `JoinDownloadGroup(downloadId)`
2. Verificar se worker está usando `IHubContext` corretamente
3. Adicionar logging no Hub para ver se events são enviados
4. Verificar se groupName está correto (`download:{id}`)

**Problema: Performance - muitos eventos**

```
Client receiving 100+ events/second
```

**Solução:**
1. Implementar throttle no UpdateProgressAsync (máx 1 evento/segundo)
2. Usar sampling progress (só enviar de 1% em 1%)
3. Aumentar keep-alive interval se necessário
