# Fase 3.2: Producer (Frontend → Fila)

**Status:** 🎯 Planejamento
**Épico:** Cutube-858 (Épico 3: Integração com RabbitMQ)
**Duração:** 3-4 dias
**Responsável:** Backend Developer
**Prioridade:** 🔥 Alta
**Dependência:** ✅ Fase 3.1 (RabbitMQ Setup) completa

---

## Objetivo

Implementar o **Producer** no projeto Cutube.Api para publicar mensagens de download na fila RabbitMQ, substituindo o processamento síncrono atual por um modelo assíncrono baseado em filas.

**Benefícios:**
- ✅ Desacoplamento entre API (producer) e processamento (consumer)
- ✅ Escalabilidade horizontal (múltiplos workers)
- ✅ Resiliência (mensagens persistem até serem processadas)
- ✅ API responde instantaneamente (202 Accepted + correlationId)
- ✅ Fallback para processamento local se RabbitMQ indisponível

---

## Visão Arquitetural

### Fluxo de Mensagens

```
┌─────────────────────────────────────────────────────────────────────────┐
│                          Frontend (Next.js)                             │
│                                                                         │
│  POST /api/downloads                                                    │
│  { url, startTime, endTime, outputPath }                               │
└────────────────────────────┬────────────────────────────────────────────┘
                             │ HTTP 202 Accepted
                             │ { downloadId, correlationId, status: "queued" }
┌────────────────────────────▼────────────────────────────────────────────┐
│                         Cutube.Api (Producer)                           │
│                                                                         │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │                    IQueueProducer                                │   │
│  │  • PublishAsync(DownloadMessage)                                │   │
│  │  • Configure correlation ID para tracking                       │   │
│  │  • Retry automático se RabbitMQ desconectado                    │   │
│  └────────────────────────┬────────────────────────────────────────┘   │
│                           │                                              │
│                           │ Publicar mensagem                            │
┌───────────────────────────▼──────────────────────────────────────────────┐
│                          RabbitMQ                                       │
│                                                                         │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │           Exchange: cutube.direct                               │   │
│  │           Routing Key: "download"                               │   │
│  └────────────────────────┬────────────────────────────────────────┘   │
│                           │                                              │
│  ┌────────────────────────▼────────────────────────────────────────┐   │
│  │           Queue: cutube.downloads                               │   │
│  │           • Mensagens enfileiradas                             │   │
│  │           • Persistência em disco                              │   │
│  │           • TTL configurável                                   │   │
│  └────────────────────────┬────────────────────────────────────────┘   │
└──────────────────────────┼──────────────────────────────────────────────┘
                           │ Aguardando consumer
┌──────────────────────────▼──────────────────────────────────────────────┐
│                      Cutube.Worker (Consumer)                          │
│                      [Fase 3.3 - não implementado ainda]               │
└────────────────────────────────────────────────────────────────────────┘
```

### Esquema de Mensagem

```json
{
  "messageId": "guid",
  "correlationId": "guid",
  "url": "https://youtube.com/watch?v=example",
  "startTime": "00:01:30",
  "endTime": "00:02:00",
  "outputPath": "/path/to/output",
  "audioOnly": false,
  "outputFilename": null,
  "priority": "normal",
  "createdAt": "2026-02-10T10:00:00Z",
  "metadata": {
    "title": null,
    "duration": null,
    "thumbnail": null
  }
}
```

### Estados de um Download

```
┌─────────┐     ┌──────────┐     ┌──────────────┐     ┌─────────────┐
│ Request │ ──► │ Queued   │ ──► │ Processing   │ ──► │ Completed   │
│         │     │          │     │              │     │             │
└─────────┘     └──────────┘     └──────────────┘     └─────────────┘
                     │                   │                    │
                     │                   ▼                    │
                     │            ┌──────────┐               │
                     │            │ Failed   │               │
                     │            │ (retry)  │               │
                     │            └──────────┘               │
                     │                                       │
                     ▼                                       ▼
              ┌──────────────┐                      ┌─────────────┐
              │ DLQ          │◄─────────────────────│ Cancelled   │
              │ (dead letter)│   permanent error    │             │
              └──────────────┘                      └─────────────┘
```

---

## Tarefas

### 3.2.1 Criar Interface e Implementação do Producer

**Estimativa:** 5-6 horas

**Arquivos:**
```
src/Cutube.Api/Queuing/
  ├── IQueueProducer.cs
  ├── RabbitMqProducer.cs
  ├── Messages/
  │   ├── DownloadMessage.cs
  │   └── DownloadMessageResponse.cs
  └── Exceptions/
      └── QueuePublishException.cs
```

**Pacotes NuGet:**
```xml
<PackageReference Include="MassTransit.RabbitMQ" Version="8.3.0" />
<PackageReference Include="MassTransit.AspNetCore" Version="8.3.0" />
```

#### 3.2.1.1 Criar Interface IQueueProducer

```csharp
// src/Cutube.Api/Queuing/IQueueProducer.cs
using Cutube.Api.Queuing.Messages;

namespace Cutube.Api.Queuing;

/// <summary>
/// Interface para publicação de mensagens na fila RabbitMQ.
/// </summary>
public interface IQueueProducer
{
    /// <summary>
    /// Publica uma mensagem de download na fila.
    /// </summary>
    /// <param name="message">Mensagem de download.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    /// <returns>Correlation ID para tracking.</returns>
    Task<string> PublishDownloadAsync(DownloadMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifica se a conexão com RabbitMQ está ativa.
    /// </summary>
    bool IsConnected { get; }
}
```

#### 3.2.1.2 Criar DownloadMessage

```csharp
// src/Cutube.Api/Queuing/Messages/DownloadMessage.cs
namespace Cutube.Api.Queuing.Messages;

/// <summary>
/// Mensagem de download para processamento assíncrono.
/// </summary>
public record DownloadMessage
{
    /// <summary>
    /// ID único da mensagem (gerado automaticamente).
    /// </summary>
    public string MessageId { get; init; } = Guid.NewGuid().ToString();

    /// <summary>
    /// ID de correlação para tracking end-to-end.
    /// </summary>
    public required string CorrelationId { get; init; }

    /// <summary>
    /// URL do vídeo para download.
    /// </summary>
    public required string Url { get; init; }

    /// <summary>
    /// Tempo de início (opcional, para recortes).
    /// Formato: "HH:MM:SS" ou "segundos".
    /// </summary>
    public string? StartTime { get; init; }

    /// <summary>
    /// Tempo de término (opcional, para recortes).
    /// Formato: "HH:MM:SS" ou "segundos".
    /// </summary>
    public string? EndTime { get; init; }

    /// <summary>
    /// Caminho de saída para o arquivo.
    /// </summary>
    public required string OutputPath { get; init; }

    /// <summary>
    /// Download apenas áudio.
    /// </summary>
    public bool AudioOnly { get; init; }

    /// <summary>
    /// Nome do arquivo de saída (opcional).
    /// </summary>
    public string? OutputFilename { get; init; }

    /// <summary>
    /// Prioridade da mensagem.
    /// </summary>
    public string Priority { get; init; } = "normal";

    /// <summary>
    /// Timestamp de criação da mensagem.
    /// </summary>
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Metadados adicionais (título, duração, thumbnail).
    /// </summary>
    public DownloadMetadata? Metadata { get; init; }
}

/// <summary>
/// Metadados do vídeo.
/// </summary>
public record DownloadMetadata
{
    public string? Title { get; init; }
    public string? Duration { get; init; }
    public string? Thumbnail { get; init; }
}
```

#### 3.2.1.3 Criar RabbitMqProducer

```csharp
// src/Cutube.Api/Queuing/RabbitMqProducer.cs
using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Cutube.Api.Queuing.Messages;
using Cutube.Worker.Configuration;

namespace Cutube.Api.Queuing;

/// <summary>
/// Implementação de producer para RabbitMQ usando MassTransit.
/// </summary>
public class RabbitMqProducer : IQueueProducer
{
    private readonly IBus _bus;
    private readonly RabbitMqOptions _options;
    private readonly ILogger<RabbitMqProducer> _logger;
    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    private bool _isConnected;

    public bool IsConnected => _isConnected;

    public RabbitMqProducer(
        IBus bus,
        IOptions<RabbitMqOptions> options,
        ILogger<RabbitMqProducer> logger)
    {
        _bus = bus;
        _options = options.Value;
        _logger = logger;
        _isConnected = true; // MassTransit gerencia reconexão automaticamente
    }

    /// <inheritdoc/>
    public async Task<string> PublishDownloadAsync(DownloadMessage message, CancellationToken cancellationToken = default)
    {
        // Validar mensagem
        ValidateMessage(message);

        // Gerar correlation ID se não existe
        if (string.IsNullOrEmpty(message.CorrelationId))
        {
            message = message with { CorrelationId = Guid.NewGuid().ToString() };
        }

        try
        {
            _logger.LogInformation(
                "Publishing download message: {MessageId}, CorrelationId: {CorrelationId}, URL: {Url}",
                message.MessageId,
                message.CorrelationId,
                message.Url);

            // Publicar mensagem usando MassTransit
            var endpoint = await _bus.GetSendEndpoint(new Uri($"queue:{RabbitMqConfig.DownloadsQueue}"));

            await endpoint.Send(message, cancellationToken);

            _isConnected = true;
            _logger.LogInformation(
                "Message published successfully: {CorrelationId}",
                message.CorrelationId);

            return message.CorrelationId;
        }
        catch (Exception ex)
        {
            _isConnected = false;
            _logger.LogError(ex,
                "Failed to publish message: {CorrelationId}. Error: {Error}",
                message.CorrelationId,
                ex.Message);

            throw new QueuePublishException(
                $"Failed to publish download message: {message.CorrelationId}",
                message.CorrelationId,
                ex);
        }
    }

    private void ValidateMessage(DownloadMessage message)
    {
        if (string.IsNullOrWhiteSpace(message.Url))
        {
            throw new ArgumentException("URL is required.", nameof(message));
        }

        if (string.IsNullOrWhiteSpace(message.OutputPath))
        {
            throw new ArgumentException("Output path is required.", nameof(message));
        }

        // Validar URL
        if (!Uri.TryCreate(message.Url, UriKind.Absolute, out var uri))
        {
            throw new ArgumentException("Invalid URL format.", nameof(message));
        }

        // Validar startTime/endTime
        if (!string.IsNullOrEmpty(message.StartTime) && !string.IsNullOrEmpty(message.EndTime))
        {
            // Validação básica - formato será validado no worker
            if (message.StartTime == message.EndTime)
            {
                throw new ArgumentException("Start time and end time cannot be equal.", nameof(message));
            }
        }
    }
}
```

#### 3.2.1.4 Criar QueuePublishException

```csharp
// src/Cutube.Api/Queuing/Exceptions/QueuePublishException.cs
namespace Cutube.Api.Queuing.Exceptions;

/// <summary>
/// Exceção lançada quando falha ao publicar mensagem na fila.
/// </summary>
public class QueuePublishException : Exception
{
    /// <summary>
    /// Correlation ID da mensagem que falhou.
    /// </summary>
    public string CorrelationId { get; }

    public QueuePublishException(string message, string correlationId)
        : base(message)
    {
        CorrelationId = correlationId;
    }

    public QueuePublishException(string message, string correlationId, Exception innerException)
        : base(message, innerException)
    {
        CorrelationId = correlationId;
    }
}
```

**Checklist:**
- [ ] Criar diretório src/Cutube.Api/Queuing/
- [ ] Criar interface IQueueProducer.cs
- [ ] Criar DownloadMessage.cs com schema completo
- [ ] Criar DownloadMetadata.cs
- [ ] Criar RabbitMqProducer.cs
- [ ] Criar QueuePublishException.cs
- [ ] Adicionar pacotes MassTransit.RabbitMQ e MassTransit.AspNetCore
- [ ] Adicionar referência a Cutube.Worker.Configuration (ou mover config para API)

**Critérios de aceito:**
- ✅ Interface criada com método PublishDownloadAsync
- ✅ DownloadMessage contém todos os campos necessários
- ✅ RabbitMqProducer implementa IQueueProducer
- ✅ Validação de mensagem implementada
- ✅ Logging de sucesso/falha
- ✅ Exceção customizada QueuePublishException

---

### 3.2.2 Configurar MassTransit na API

**Estimativa:** 3-4 horas

**Arquivos:**
```
src/Cutube.Api/
  ├── Program.cs (modificar)
  ├── Configuration/
  │   └── RabbitMqOptions.cs (copiar ou referenciar)
  └── Extensions/
      └── ServiceCollectionExtensions.cs (criar)
```

#### 3.2.2.1 Configurar RabbitMqOptions (se ainda não existe)

```csharp
// src/Cutube.Api/Configuration/RabbitMqOptions.cs
namespace Cutube.Api.Configuration;

public class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string VirtualHost { get; set; } = "/";
    public string UserName { get; set; } = "cutube";
    public string Password { get; set; } = "cutube123";
    public int RetryCount { get; set; } = 5;
    public int Timeout { get; set; } = 30;
}
```

#### 3.2.2.2 Configurar MassTransit no Program.cs

```csharp
// src/Cutube.Api/Program.cs
using Cutube.Api.Configuration;
using Cutube.Api.Queuing;
using MassTransit;

var builder = WebApplication.CreateBuilder(args);

// ... outras configurações ...

// RabbitMQ Configuration
builder.Services.Configure<RabbitMqOptions>(
    builder.Configuration.GetSection(RabbitMqOptions.SectionName)
);

// MassTransit (Producer apenas - não configura consumers na API)
builder.Services.AddMassTransit(x =>
{
    // Não adiciona consumers na API (apenas producer)
});

builder.Services.AddMassTransitHostedService();

// Registrar producer
builder.Services.AddSingleton<IQueueProducer, RabbitMqProducer>();

// ... resto das configurações ...

var app = builder.Build();

// ... middleware ...

app.Run();
```

**appsettings.json:**
```json
{
  "RabbitMq": {
    "Host": "localhost",
    "Port": 5672,
    "VirtualHost": "/",
    "UserName": "cutube",
    "Password": "cutube123",
    "RetryCount": 5,
    "Timeout": 30
  }
}
```

**appsettings.Development.json:**
```json
{
  "RabbitMq": {
    "Host": "localhost"
  }
}
```

#### 3.2.2.3 Health Check para RabbitMQ

```csharp
// src/Cutube.Api/Program.cs
using MassTransit.HealthChecks;

// Adicionar health check
builder.Services.AddHealthChecks()
    .AddCheck<RabbitMqHealthCheck>("rabbitmq");

// Endpoint de health
app.MapHealthChecks("/health");
```

**Checklist:**
- [ ] Criar RabbitMqOptions.cs na API (se não existe)
- [ ] Adicionar configuração no appsettings.json
- [ ] Configurar AddMassTransit no Program.cs (apenas producer)
- [ ] Registrar IQueueProducer como singleton
- [ ] Adicionar health check para RabbitMQ
- [ ] Testar startup da API sem erros

**Critérios de aceito:**
- ✅ API inicia sem erros
- ✅ MassTransit configurado como producer
- ✅ IQueueProducer injetável no DI
- ✅ Health check retorna healthy se RabbitMQ conectado

---

### 3.2.3 Integrar Producer com Endpoint POST /api/downloads

**Estimativa:** 4-5 horas

**Arquivos a modificar:**
```
src/Cutube.Api/Endpoints/
  └── DownloadsEndpoints.cs (modificar)
src/Cutube.Api/Models/
  └── CreateDownloadResponse.cs (modificar)
```

#### 3.2.3.1 Modificar DownloadsEndpoints

```csharp
// src/Cutube.Api/Endpoints/DownloadsEndpoints.cs
using Microsoft.AspNetCore.Mvc;
using Cutube.Api.Queuing;
using Cutube.Api.Queuing.Messages;
using Cutube.Api.Models;
using Cutube.Domain.Models;

namespace Cutube.Api.Endpoints;

public static class DownloadsEndpoints
{
    public static void MapDownloadsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/downloads")
            .WithTags("Downloads")
            .WithOpenApi();

        // POST /api/downloads - Enfileirar download
        group.MapPost("/", async (
            [FromBody] CreateDownloadRequest request,
            IQueueProducer producer,
            ILogger<Program> logger,
            CancellationToken ct) =>
        {
            try
            {
                // Validar request
                if (string.IsNullOrWhiteSpace(request.Url))
                {
                    return Results.BadRequest(new { error = "URL is required" });
                }

                logger.LogInformation("Creating download for URL: {Url}", request.Url);

                // Criar mensagem
                var message = new DownloadMessage
                {
                    CorrelationId = Guid.NewGuid().ToString(),
                    Url = request.Url,
                    StartTime = request.StartTime,
                    EndTime = request.EndTime,
                    OutputPath = request.OutputPath,
                    AudioOnly = request.AudioOnly ?? false,
                    OutputFilename = request.OutputFilename,
                    Priority = request.Priority ?? "normal",
                    CreatedAt = DateTime.UtcNow
                };

                // Publicar na fila
                var correlationId = await producer.PublishDownloadAsync(message, ct);

                logger.LogInformation(
                    "Download enqueued: {CorrelationId}",
                    correlationId);

                // Retornar 202 Accepted + correlation ID
                return Results.Accepted(
                    $"/api/downloads/{correlationId}",
                    new CreateDownloadResponse
                    {
                        DownloadId = correlationId,
                        CorrelationId = correlationId,
                        Status = "queued",
                        Message = "Download enqueued successfully",
                        EnqueuedAt = DateTime.UtcNow
                    });
            }
            catch (QueuePublishException ex)
            {
                logger.LogError(ex, "Failed to enqueue download");

                // Retornar 503 Service Unavailable
                return Results.ServiceUnavailable(new
                {
                    error = "Failed to enqueue download",
                    message = "Queue service is unavailable. Please try again later.",
                    correlationId = ex.CorrelationId
                });
            }
            catch (ArgumentException ex)
            {
                logger.LogWarning(ex, "Invalid download request");
                return Results.BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error creating download");
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: 500,
                    title: "Internal server error"
                );
            }
        })
        .WithName("CreateDownload")
        .WithSummary("Enqueue a new download")
        .WithOpenApi();

        // ... outros endpoints ...
    }
}
```

#### 3.2.3.2 Modificar CreateDownloadResponse

```csharp
// src/Cutube.Api/Models/CreateDownloadResponse.cs
namespace Cutube.Api.Models;

public record CreateDownloadResponse
{
    /// <summary>
    /// ID do download (igual ao correlation ID).
    /// </summary>
    public required string DownloadId { get; init; }

    /// <summary>
    /// Correlation ID para tracking end-to-end.
    /// </summary>
    public required string CorrelationId { get; init; }

    /// <summary>
    /// Status do download: "queued".
    /// </summary>
    public required string Status { get; init; }

    /// <summary>
    /// Mensagem descritiva.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Timestamp de enfileiramento.
    /// </summary>
    public DateTime EnqueuedAt { get; init; }

    /// <summary>
    /// URL para consultar status do download.
    /// </summary>
    public string StatusUrl => $"/api/downloads/{DownloadId}";
}
```

#### 3.2.3.3 Implementar Fallback (opcional)

```csharp
// src/Cutube.Api/Queuing/QueueProducerFallback.cs
using Microsoft.Extensions.Options;

namespace Cutube.Api.Queuing;

/// <summary>
/// Producer com fallback para processamento local se RabbitMQ indisponível.
/// </summary>
public class RabbitMqProducerWithFallback : IQueueProducer
{
    private readonly RabbitMqProducer _producer;
    private readonly IQueueProducer _fallbackProducer;
    private readonly ILogger<RabbitMqProducerWithFallback> _logger;

    public bool IsConnected => _producer.IsConnected;

    public RabbitMqProducerWithFallback(
        RabbitMqProducer producer,
        IQueueProducer fallbackProducer,
        ILogger<RabbitMqProducerWithFallback> logger)
    {
        _producer = producer;
        _fallbackProducer = fallbackProducer;
        _logger = logger;
    }

    public async Task<string> PublishDownloadAsync(Messages.DownloadMessage message, CancellationToken cancellationToken = default)
    {
        try
        {
            // Tentar publicar no RabbitMQ
            if (_producer.IsConnected)
            {
                return await _producer.PublishDownloadAsync(message, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "RabbitMQ unavailable, falling back to local processing");
        }

        // Fallback para processamento local
        _logger.LogInformation("Using fallback producer for download: {CorrelationId}", message.CorrelationId);
        return await _fallbackProducer.PublishDownloadAsync(message, cancellationToken);
    }
}
```

**Checklist:**
- [ ] Modificar POST /api/downloads para usar IQueueProducer
- [ ] Criar DownloadMessage a partir do request
- [ ] Publicar mensagem usando producer.PublishDownloadAsync
- [ ] Retornar 202 Accepted + correlationId
- [ ] Tratar QueuePublishException (503 Service Unavailable)
- [ ] Tratar ArgumentException (400 Bad Request)
- [ ] Adicionar logging detalhado
- [ ] (Opcional) Implementar fallback para processamento local

**Critérios de aceito:**
- ✅ Endpoint publica mensagem na fila
- ✅ Retorna 202 Accepted com correlationId
- ✅ Trata erros de publicação (503)
- ✅ Trata requisições inválidas (400)
- ✅ Mensagem contém todos os campos necessários
- ✅ Logging de sucesso/falha

---

### 3.2.4 Atualizar Frontend para Tracking

**Estimativa:** 3-4 horas

**Arquivos:**
```
cutube-web/
  ├── lib/
  │   ├── api.ts (modificar)
  │   └── types.ts (modificar)
  └── components/
      ├── DownloadForm.tsx (modificar)
      └── DownloadCard.tsx (modificar)
```

#### 3.2.4.1 Modificar API Client

```typescript
// cutube-web/lib/api.ts
export interface CreateDownloadRequest {
  url: string;
  startTime?: string;
  endTime?: string;
  outputPath: string;
  audioOnly?: boolean;
  outputFilename?: string;
  priority?: string;
}

export interface CreateDownloadResponse {
  downloadId: string;
  correlationId: string;
  status: string;
  message: string;
  enqueuedAt: string;
  statusUrl: string;
}

export async function createDownload(
  request: CreateDownloadRequest
): Promise<CreateDownloadResponse> {
  const response = await fetch('/api/downloads', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(request),
  });

  if (!response.ok) {
    const error = await response.json();
    throw new Error(error.error || 'Failed to create download');
  }

  if (response.status === 202) {
    // Accepted - download enqueued
    return await response.json();
  }

  throw new Error(`Unexpected status: ${response.status}`);
}

export async function getDownloadStatus(
  downloadId: string
): Promise<DownloadStatus> {
  const response = await fetch(`/api/downloads/${downloadId}`);

  if (!response.ok) {
    throw new Error('Failed to get download status');
  }

  return await response.json();
}
```

#### 3.2.4.2 Modificar DownloadCard para Mostrar Status

```tsx
// cutube-web/components/DownloadCard.tsx
'use client';

import { DownloadStatus } from '@/lib/types';
import { useEffect, useState } from 'react';

interface DownloadCardProps {
  downloadId: string;
  correlationId: string;
  initialStatus?: string;
}

export function DownloadCard({ downloadId, correlationId, initialStatus = 'queued' }: DownloadCardProps) {
  const [status, setStatus] = useState(initialStatus);
  const [progress, setProgress] = useState(0);

  useEffect(() => {
    // Poll status a cada 2 segundos
    const interval = setInterval(async () => {
      try {
        const data = await getDownloadStatus(downloadId);
        setStatus(data.status);
        setProgress(data.progress || 0);

        // Parar polling se completou ou falhou
        if (data.status === 'completed' || data.status === 'failed' || data.status === 'cancelled') {
          clearInterval(interval);
        }
      } catch (error) {
        console.error('Failed to fetch status:', error);
      }
    }, 2000);

    return () => clearInterval(interval);
  }, [downloadId]);

  return (
    <div className="border rounded-lg p-4">
      <div className="flex justify-between items-center mb-2">
        <h3 className="font-semibold">{downloadId.slice(0, 8)}</h3>
        <StatusBadge status={status} />
      </div>

      {status === 'queued' && (
        <div className="text-sm text-gray-600">
          <p>📥 Enqueued</p>
          <p className="text-xs">Waiting for worker...</p>
        </div>
      )}

      {status === 'processing' && (
        <div>
          <div className="w-full bg-gray-200 rounded-full h-2">
            <div
              className="bg-blue-600 h-2 rounded-full transition-all"
              style={{ width: `${progress}%` }}
            />
          </div>
          <p className="text-xs text-gray-600 mt-1">{progress}%</p>
        </div>
      )}

      {status === 'completed' && (
        <div className="text-sm text-green-600">
          <p>✅ Completed</p>
        </div>
      )}

      {status === 'failed' && (
        <div className="text-sm text-red-600">
          <p>❌ Failed</p>
        </div>
      )}

      <div className="mt-2 text-xs text-gray-500">
        Correlation ID: {correlationId}
      </div>
    </div>
  );
}
```

**Checklist:**
- [ ] Modificar createDownload para lidar com 202 Accepted
- [ ] Adicionar correlationId na resposta
- [ ] Modificar DownloadCard para mostrar status "queued"
- [ ] Implementar polling de status (2 segundos)
- [ ] Mostrar correlation ID no card
- [ ] Testar fluxo completo

**Critérios de aceito:**
- ✅ Frontend lida com 202 Accepted
- ✅ Correlation ID armazenado e exibido
- ✅ Status atualiza via polling
- ✅ Cards mostram estado correto (queued, processing, completed)

---

### 3.2.5 Testes e Documentação

**Estimativa:** 3-4 horas

**Arquivos:**
```
tests/Cutube.Api.Tests/Unit/Queuing/
  ├── RabbitMqProducerTests.cs
  └── DownloadMessageTests.cs
docs/
  └── rabbitmq-producer.md
```

#### 3.2.5.1 Testes Unitários para RabbitMqProducer

```csharp
// tests/Cutube.Api.Tests/Unit/Queuing/RabbitMqProducerTests.cs
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.Logging;
using Moq;
using Cutube.Api.Queuing;
using Cutube.Api.Queuing.Messages;
using Cutube.Worker.Configuration;

namespace Cutube.Api.Tests.Unit.Queuing;

public class RabbitMqProducerTests
{
    [Fact]
    public async Task PublishDownloadAsync_WithValidMessage_ReturnsCorrelationId()
    {
        // Arrange
        var mockBus = new Mock<IBus>();
        var mockSendEndpoint = new Mock<ISendEndpoint>();
        var mockOptions = new Mock<IOptions<RabbitMqOptions>>();
        var mockLogger = new Mock<ILogger<RabbitMqProducer>>();

        mockOptions.Setup(x => x.Value).Returns(new RabbitMqOptions
        {
            Host = "localhost",
            Port = 5672
        });

        var message = new DownloadMessage
        {
            CorrelationId = Guid.NewGuid().ToString(),
            Url = "https://youtube.com/watch?v=test",
            OutputPath = "/tmp/test"
        };

        // Act
        var producer = new RabbitMqProducer(
            mockBus.Object,
            mockOptions.Object,
            mockLogger.Object);

        var result = await producer.PublishDownloadAsync(message);

        // Assert
        result.Should().Be(message.CorrelationId);
    }

    [Fact]
    public async Task PublishDownloadAsync_WithInvalidUrl_ThrowsArgumentException()
    {
        // Arrange
        var mockBus = new Mock<IBus>();
        var mockOptions = new Mock<IOptions<RabbitMqOptions>>();
        var mockLogger = new Mock<ILogger<RabbitMqProducer>>();

        var message = new DownloadMessage
        {
            CorrelationId = Guid.NewGuid().ToString(),
            Url = "invalid-url",
            OutputPath = "/tmp/test"
        };

        // Act
        var producer = new RabbitMqProducer(
            mockBus.Object,
            mockOptions.Object,
            mockLogger.Object);

        Func<Task> act = async () => await producer.PublishDownloadAsync(message);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }
}
```

#### 3.2.5.2 Documentação

```markdown
# RabbitMQ Producer - Cutube.Api

## Visão Geral

O Producer é responsável por publicar mensagens de download na fila RabbitMQ para processamento assíncrono.

## Endpoint

### POST /api/downloads

Enfileira um novo download para processamento.

**Request:**
```json
{
  "url": "https://youtube.com/watch?v=example",
  "startTime": "00:01:30",
  "endTime": "00:02:00",
  "outputPath": "/path/to/output",
  "audioOnly": false
}
```

**Response (202 Accepted):**
```json
{
  "downloadId": "guid",
  "correlationId": "guid",
  "status": "queued",
  "message": "Download enqueued successfully",
  "enqueuedAt": "2026-02-10T10:00:00Z",
  "statusUrl": "/api/downloads/{correlationId}"
}
```

## Estados do Download

- **queued**: Aguardando processamento na fila
- **processing**: Sendo processado pelo worker
- **completed**: Download concluído com sucesso
- **failed**: Falha no download
- **cancelled**: Cancelado pelo usuário

## Tracking

Use o `correlationId` para rastrear o download:
1. API retorna correlationId ao enfileirar
2. Worker usa correlationId para atualizar status
3. Frontend usa correlationId para consultar status

## Troubleshooting

### 503 Service Unavailable
RabbitMQ está indisponível. Verifique:
```bash
docker-compose ps rabbitmq
```

### Mensagem não processada
Verifique a DLQ:
```bash
curl -u cutube:cutube123 http://localhost:15672/api/queues/%2F/cutube.downloads.dlq
```
```

**Checklist:**
- [ ] Criar testes unitários para RabbitMqProducer
- [ ] Criar testes para validação de DownloadMessage
- [ ] Criar docs/rabbitmq-producer.md
- [ ] Atualizar README principal
- [ ] Adicionar exemplos de uso

**Critérios de aceito:**
- ✅ Testes unitários passam
- ✅ Cobertura > 80% para QueueProducer
- ✅ Documentação clara e completa
- ✅ README atualizado

---

## Qualidade Gates - Fase 3.2

**ANTES de considerar esta fase completa, TODOS os itens abaixo devem ser verdadeiros:**

- [ ] **dotnet build** - **0 warnings** (build limpo)
- [ ] **dotnet test** - **100% pass** (todos os testes)
- [ ] **RabbitMQ rodando** via Docker Compose
- [ ] **Producer publicado** mensagens na fila com sucesso
- [ ] **Endpoint POST /api/downloads** retorna 202 Accepted
- [ ] **Frontend atualizado** para lidar com correlationId
- [ ] **Health check** retorna healthy para RabbitMQ
- [ ] **Documentação completa** em docs/rabbitmq-producer.md
- [ ] **Testes manuais** executados com sucesso

---

## Cronograma Detalhado

| Tarefa | Estimativa | Dependencies | Blocker |
|--------|-----------|--------------|---------|
| 3.2.1 Interface e Producer | 5-6h | 3.1 completa | Não |
| 3.2.2 Configurar MassTransit | 3-4h | 3.2.1 | **Sim** |
| 3.2.3 Integrar com API | 4-5h | 3.2.2 | **Sim** |
| 3.2.4 Atualizar Frontend | 3-4h | 3.2.3 | **Sim** |
| 3.2.5 Testes e Docs | 3-4h | 3.2.1, 3.2.4 | **Sim** |

**Total:** 18-23 horas (~3-4 dias)

---

## Tecnologias

- **MassTransit 8** - Abstração para RabbitMQ
- **RabbitMQ.Client** - Cliente AMQP
- **.NET 10** - ASP.NET Core Minimal API
- **Next.js 15** - Frontend (TypeScript)
- **Docker** - RabbitMQ container

---

## Riscos e Mitigações

| Risco | Impacto | Mitigação |
|-------|---------|-----------|
| RabbitMQ downtime durante publish | Alto | Retry automático, fallback para processamento local |
| Perda de mensagens | Alto | Persistência habilitada, confirmação de entrega |
| Performance degradation | Médio | Timeout configurado, health checks |
| Frontend não trata 202 Accepted | Médio | Atualizar componentes, testar E2E |

---

## Próximos Passos

Após completar Fase 3.2:

1. **Fase 3.3: Consumer** - Implementar worker dedicado
2. Criar projeto Cutube.Worker
3. Implementar DownloadConsumer
4. Processar mensagens da fila

---

## Entregáveis (Deliverables)

- [x] IQueueProducer interface criada
- [ ] RabbitMqProducer implementado
- [ ] DownloadMessage schema definido
- [ ] MassTransit configurado na API
- [ ] Endpoint POST /api/downloads integrado
- [ ] Frontend atualizado para tracking
- [ ] Testes unitários criados
- [ ] Documentação completa
- [ ] Health check implementado

---

## Exemplo de Fluxo Completo

### 1. Frontend envia request

```bash
curl -X POST http://localhost:5000/api/downloads \
  -H "Content-Type: application/json" \
  -d '{
    "url": "https://youtube.com/watch?v=dQw4w9WgXcQ",
    "outputPath": "/tmp/downloads"
  }'
```

### 2. API publica na fila

```
HTTP/1.1 202 Accepted
{
  "downloadId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "correlationId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "status": "queued",
  "message": "Download enqueued successfully",
  "enqueuedAt": "2026-02-10T10:00:00Z",
  "statusUrl": "/api/downloads/a1b2c3d4-e5f6-7890-abcd-ef1234567890"
}
```

### 3. Verificar mensagem na fila

```bash
# Via RabbitMQ Management UI
# http://localhost:15672
# Login: cutube / cutube123
# Queue: cutube.downloads
# Get Messages -> obtém a mensagem enfileirada
```

### 4. Consultar status (depois que worker existir)

```bash
curl http://localhost:5000/api/downloads/a1b2c3d4-e5f6-7890-abcd-ef1234567890
```

---

**Fim do Plano - Fase 3.2**
