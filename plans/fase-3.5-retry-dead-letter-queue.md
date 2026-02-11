# Fase 3.5: Retry & Dead Letter Queue

**Status:** 🎯 Planejamento  
**Épico:** Cutube-858 (Épico 3: Integração com RabbitMQ)  
**Duração:** 2-3 dias  
**Responsável:** Backend Developer  
**Prioridade:** 🔥 Alta  
**Dependência:** ✅ Fase 3.4 (Status Tracking) completa

---

## Objetivo

Implementar políticas robustas de **retry** com backoff exponencial e **Dead Letter Queue (DLQ)** para garantir resiliência no processamento de downloads. Quando um download falha temporariamente (network timeout, indisponibilidade do YouTube), o sistema deve tentar novamente automaticamente. Após esgotar as tentativas, a mensagem vai para a DLQ para análise e reprocessamento manual.

**Benefícios:**
- ✅ **Resiliência**: Falhas transitórias são automaticamente recuperadas
- ✅ **Visibilidade**: DLQ permite identificar problemas sistêmicos
- ✅ **Reprocessamento**: Mensagens na DLQ podem ser retentadas manualmente
- ✅ **Debugging**: Logs detalhados para análise de falhas
- ✅ **Observability**: Métricas de retry e taxa de erro

---

## Visão Arquitetural

### Fluxo de Retry e DLQ

```
┌─────────────────────────────────────────────────────────────────────────┐
│                         Fluxo de Processamento                          │
│                                                                         │
│  ┌──────────────┐                                                       │
│  │ cutube.      │  1. Consumer recebe mensagem                          │
│  │ downloads    │                                                       │
│  └──────┬───────┘                                                       │
│         │                                                               │
│         ▼                                                               │
│  ┌──────────────────────────────────────────────────────────────────┐  │
│  │                    DownloadConsumer                               │  │
│  │                                                                   │  │
│  │  ┌─────────────┐    ┌─────────────┐    ┌─────────────┐          │  │
│  │  │ Tentativa 1 │───►│ Tentativa 2 │───►│ Tentativa 3 │          │  │
│  │  │   (5s)      │    │   (30s)     │    │   (2min)    │          │  │
│  │  └──────┬──────┘    └──────┬──────┘    └──────┬──────┘          │  │
│  │         │                  │                  │                  │  │
│  │         ▼                  ▼                  ▼                  │  │
│  │  ┌──────────────────────────────────────────────────────────┐   │  │
│  │  │              TENTATIVAS ESgotadas                         │   │  │
│  │  └────────────────────────┬─────────────────────────────────┘   │  │
│  │                           │                                      │  │
│  │                           ▼                                      │  │
│  │  ┌──────────────────────────────────────────────────────────┐   │  │
│  │  │                    DLQ Handler                            │   │  │
│  │  │  • Log da falha                                          │   │  │
│  │  │  • Atualiza status para DeadLetter                       │   │  │
│  │  │  • Notifica API via callback                             │   │  │
│  │  └────────────────────────┬─────────────────────────────────┘   │  │
│  │                           │                                      │  │
│  │                           ▼                                      │  │
│  │  ┌──────────────────────────────────────────────────────────┐   │  │
│  │  │           cutube.downloads.dlq                            │   │  │
│  │  │  (Dead Letter Queue - mensagens com erro permanente)     │   │  │
│  │  └──────────────────────────────────────────────────────────┘   │  │
│  │                                                                   │  │
│  └───────────────────────────────────────────────────────────────────┘  │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────────────┐
│                      Reprocessamento Manual                             │
│                                                                         │
│  ┌─────────────────────┐        POST /api/downloads/dlq/{id}/retry     │
│  │  Dashboard /monitor │──────────────────────────────────────────────►│
│  │                     │                                              │
│  │  [Retry] [Delete]   │        1. Busca mensagem na DLQ              │
│  │                     │        2. Republica na fila principal        │
│  │  Failed Downloads   │        3. Remove da DLQ                      │
│  │  • Error details    │        4. Atualiza status para Queued        │
│  │  • Retry count      │                                              │
│  └─────────────────────┘                                              │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### Estados e Transições

```
┌──────────┐     ┌──────────┐     ┌──────────────┐     ┌─────────────┐
│  Queued  │────►│Processing│────►│   Completed  │     │             │
└──────────┘     └────┬─────┘     └──────────────┘     │  Success    │
                      │                                 └─────────────┘
                      │
                      ▼ Error temporário
               ┌──────────────┐
               │   Failed     │
               │ (retryable)  │
               └──────┬───────┘
                      │
                      │ 3 tentativas com backoff
                      │
                      ▼ Error permanente
               ┌──────────────┐     POST /api/downloads/dlq/{id}/retry
               │  DeadLetter  │◄──────────────────────────────────────┐
               │    (DLQ)     │                                        │
               └──────────────┘────────────────────────────────────────┘
                      │
                      ▼ Reprocessado
               ┌──────────────┐
               │  Queued      │
               │  (retry)     │
               └──────────────┘

Legenda de Erros:
┌──────────────────────────┬─────────────────┬─────────────┐
│ Erro                     │ Classificação   │ Ação        │
├──────────────────────────┼─────────────────┼─────────────┤
│ Network timeout          │ Temporário      │ Retry       │
│ DNS resolution failed    │ Temporário      │ Retry       │
│ HTTP 429 (Rate limit)    │ Temporário      │ Retry       │
│ HTTP 503 (Unavailable)   │ Temporário      │ Retry       │
│ yt-dlp not found         │ Permanente      │ DLQ         │
│ Invalid URL format       │ Permanente      │ DLQ         │
│ Permission denied        │ Permanente      │ DLQ         │
│ Disk full                │ Permanente      │ DLQ         │
└──────────────────────────┴─────────────────┴─────────────┘
```

### Configuração do Retry Policy

```
┌─────────────────────────────────────────────────────────────────┐
│              Política de Retry com Backoff Exponencial          │
│                                                                 │
│  Tentativa    │  Delay    │  Cumulative Time   │  Action        │
│  ─────────────┼───────────┼────────────────────┼────────────────│
│  1ª           │  5s       │  5s                │  Retry         │
│  2ª           │  30s      │  35s               │  Retry         │
│  3ª           │  2min     │  2min 35s          │  Retry         │
│  ─────────────┼───────────┼────────────────────┼────────────────│
│  DLQ          │  -        │  -                 │  Manual Retry  │
│                                                                 │
│  Backoff: exponential (multiplier ~6 between attempts)          │
│  MaxRetries: 3                                                  │
│  Filter: Only transient exceptions (network, timeout)           │
└─────────────────────────────────────────────────────────────────┘
```

---

## Tarefas

### 3.5.1 Implementar Política de Retry no Worker

**Estimativa:** 5-6 horas

**Arquivos:**
```
src/Cutube.Worker/
  ├── Configuration/
  │   └── RetryPolicyOptions.cs
  ├── Exceptions/
  │   ├── TransientException.cs
  │   └── PermanentException.cs
  ├── Handlers/
  │   └── RetryHandler.cs
  └── Consumers/
      └── DownloadConsumer.cs (atualizar)
```

#### 3.5.1.1 Criar RetryPolicyOptions

```csharp
// src/Cutube.Worker/Configuration/RetryPolicyOptions.cs
namespace Cutube.Worker.Configuration;

/// <summary>
/// Configurações da política de retry.
/// </summary>
public class RetryPolicyOptions
{
    public const string SectionName = "RetryPolicy";

    /// <summary>
    /// Número máximo de tentativas (default: 3).
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Delay inicial em segundos (default: 5).
    /// </summary>
    public int InitialDelaySeconds { get; set; } = 5;

    /// <summary>
    /// Multiplicador para backoff exponencial (default: 6).
    /// </summary>
    public double BackoffMultiplier { get; set; } = 6;

    /// <summary>
    /// Delay máximo em segundos (default: 120 = 2min).
    /// </summary>
    public int MaxDelaySeconds { get; set; } = 120;

    /// <summary>
    /// Calcula o delay para uma tentativa específica.
    /// </summary>
    public TimeSpan GetDelayForAttempt(int attemptNumber)
    {
        if (attemptNumber <= 1) 
            return TimeSpan.FromSeconds(InitialDelaySeconds);
        
        var delay = InitialDelaySeconds * Math.Pow(BackoffMultiplier, attemptNumber - 1);
        var clampedDelay = Math.Min(delay, MaxDelaySeconds);
        
        return TimeSpan.FromSeconds(clampedDelay);
    }
}
```

#### 3.5.1.2 Criar Exceções de Classificação

```csharp
// src/Cutube.Worker/Exceptions/TransientException.cs
namespace Cutube.Worker.Exceptions;

/// <summary>
/// Exceção que indica uma falha transitória que pode ser resolvida com retry.
/// </summary>
public class TransientException : Exception
{
    public TransientException(string message) : base(message) { }
    public TransientException(string message, Exception innerException) 
        : base(message, innerException) { }
}

// src/Cutube.Worker/Exceptions/PermanentException.cs
namespace Cutube.Worker.Exceptions;

/// <summary>
/// Exceção que indica uma falha permanente que não deve ser retentada.
/// </summary>
public class PermanentException : Exception
{
    public PermanentException(string message) : base(message) { }
    public PermanentException(string message, Exception innerException) 
        : base(message, innerException) { }
}
```

#### 3.5.1.3 Criar RetryHandler

```csharp
// src/Cutube.Worker/Handlers/RetryHandler.cs
using Cutube.Worker.Configuration;
using Cutube.Worker.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cutube.Worker.Handlers;

/// <summary>
/// Handler responsável por executar operações com retry e backoff exponencial.
/// </summary>
public class RetryHandler
{
    private readonly RetryPolicyOptions _options;
    private readonly ILogger<RetryHandler> _logger;

    public RetryHandler(
        IOptions<RetryPolicyOptions> options,
        ILogger<RetryHandler> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// Executa uma operação com retry automático para exceções transitórias.
    /// </summary>
    public async Task<T> ExecuteAsync<T>(
        string operationName,
        Func<Task<T>> operation,
        CancellationToken cancellationToken = default)
    {
        var lastException = new Exception("Unknown error");
        
        for (int attempt = 1; attempt <= _options.MaxRetries; attempt++)
        {
            try
            {
                _logger.LogDebug(
                    "Executing {OperationName}, attempt {Attempt}/{MaxRetries}",
                    operationName, attempt, _options.MaxRetries);

                var result = await operation();
                
                if (attempt > 1)
                {
                    _logger.LogInformation(
                        "{OperationName} succeeded on attempt {Attempt}",
                        operationName, attempt);
                }
                
                return result;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (TransientException ex)
            {
                lastException = ex;
                
                if (attempt < _options.MaxRetries)
                {
                    var delay = _options.GetDelayForAttempt(attempt);
                    _logger.LogWarning(
                        ex,
                        "{OperationName} failed with transient error on attempt {Attempt}. " +
                        "Retrying in {DelaySeconds}s...",
                        operationName, attempt, delay.TotalSeconds);
                    
                    await Task.Delay(delay, cancellationToken);
                }
                else
                {
                    _logger.LogError(
                        ex,
                        "{OperationName} failed after {MaxRetries} attempts. " +
                        "Max retries exhausted.",
                        operationName, _options.MaxRetries);
                }
            }
            catch (PermanentException)
            {
                throw;
            }
            catch (Exception ex)
            {
                // Erros não classificados são considerados permanentes
                _logger.LogError(
                    ex,
                    "{OperationName} failed with permanent error on attempt {Attempt}. " +
                    "No retry will be attempted.",
                    operationName, attempt);
                throw new PermanentException(
                    $"Permanent error in {operationName}: {ex.Message}", ex);
            }
        }

        throw new TransientException(
            $"{operationName} failed after {_options.MaxRetries} attempts", 
            lastException);
    }

    /// <summary>
    /// Executa uma operação sem retorno com retry.
    /// </summary>
    public async Task ExecuteAsync(
        string operationName,
        Func<Task> operation,
        CancellationToken cancellationToken = default)
    {
        await ExecuteAsync(operationName, async () => 
        {
            await operation();
            return true;
        }, cancellationToken);
    }
}

/// <summary>
/// Extensões para classificar exceções.
/// </summary>
public static class ExceptionClassifier
{
    /// <summary>
    /// Classifica uma exceção de download como transitória ou permanente.
    /// </summary>
    public static Exception ClassifyDownloadException(Exception ex, string url)
    {
        var message = ex.Message.ToLowerInvariant();
        var innerMessage = ex.InnerException?.Message?.ToLowerInvariant() ?? "";

        // Erros transitórios (network, timeout, rate limit)
        var transientPatterns = new[]
        {
            "timeout",
            "connection",
            "network",
            "unreachable",
            "temporarily",
            "rate limit",
            "429",
            "503",
            "502",
            "504",
            "dns",
            "name resolution",
            "connection refused",
            "no route to host"
        };

        // Erros permanentes (lógicos, configuração, permissões)
        var permanentPatterns = new[]
        {
            "permission denied",
            "not found",
            "invalid",
            "forbidden",
            "401",
            "403",
            "404",
            "disk full",
            "no space",
            "unauthorized",
            "bad request",
            "malformed"
        };

        if (transientPatterns.Any(p => message.Contains(p) || innerMessage.Contains(p)))
        {
            return new TransientException($"Transient error processing {url}: {ex.Message}", ex);
        }

        if (permanentPatterns.Any(p => message.Contains(p) || innerMessage.Contains(p)))
        {
            return new PermanentException($"Permanent error processing {url}: {ex.Message}", ex);
        }

        // Por padrão, erros de processamento são transitórios (yt-dlp pode falhar intermitentemente)
        return new TransientException($"Processing error for {url}: {ex.Message}", ex);
    }
}
```

#### 3.5.1.4 Atualizar DownloadConsumer com Retry

```csharp
// src/Cutube.Worker/Consumers/DownloadConsumer.cs
using Cutube.Api.Queuing.Messages;
using Cutube.Worker.Exceptions;
using Cutube.Worker.Handlers;
using Cutube.Worker.Services;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Cutube.Worker.Consumers;

/// <summary>
/// Consumer que processa mensagens de download com retry automático.
/// </summary>
public class DownloadConsumer : IConsumer<DownloadMessage>
{
    private readonly DownloadProcessingService _processingService;
    private readonly StatusNotificationService _notificationService;
    private readonly RetryHandler _retryHandler;
    private readonly ILogger<DownloadConsumer> _logger;

    public DownloadConsumer(
        DownloadProcessingService processingService,
        StatusNotificationService notificationService,
        RetryHandler retryHandler,
        ILogger<DownloadConsumer> logger)
    {
        _processingService = processingService;
        _notificationService = notificationService;
        _retryHandler = retryHandler;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<DownloadMessage> context)
    {
        var message = context.Message;
        var correlationId = message.CorrelationId;

        _logger.LogInformation(
            "Processing download: {CorrelationId}, URL: {Url}, Attempt: {RetryCount}",
            correlationId, message.Url, message.RetryCount);

        try
        {
            // Notificar início do processamento
            await _notificationService.NotifyProcessingStartedAsync(correlationId);

            // Executar download com retry
            var result = await _retryHandler.ExecuteAsync(
                $"Download-{correlationId}",
                async () => await ExecuteDownloadAsync(message, context.CancellationToken),
                context.CancellationToken);

            // Sucesso
            await _notificationService.NotifyCompletedAsync(correlationId, result);
            
            _logger.LogInformation(
                "Download completed successfully: {CorrelationId}",
                correlationId);
        }
        catch (TransientException ex)
        {
            // Todas as tentativas falharam - enviar para DLQ
            _logger.LogError(
                ex,
                "Download failed after all retry attempts: {CorrelationId}. " +
                "Sending to DLQ...",
                correlationId);

            await _notificationService.NotifyDeadLetterAsync(
                correlationId, 
                ex.Message,
                message.RetryCount + 1);

            // Re-lançar para que MassTransit envie para DLQ
            throw;
        }
        catch (PermanentException ex)
        {
            // Erro permanente - enviar direto para DLQ sem retry
            _logger.LogError(
                ex,
                "Download failed with permanent error: {CorrelationId}. " +
                "Sending to DLQ...",
                correlationId);

            await _notificationService.NotifyDeadLetterAsync(
                correlationId, 
                ex.Message,
                message.RetryCount);

            throw;
        }
        catch (Exception ex)
        {
            // Erro não esperado - tentar classificar
            _logger.LogError(
                ex,
                "Unexpected error processing download: {CorrelationId}",
                correlationId);

            var classified = ExceptionClassifier.ClassifyDownloadException(ex, message.Url);
            
            if (classified is TransientException)
            {
                await _notificationService.NotifyDeadLetterAsync(
                    correlationId, 
                    $"Failed after retries: {ex.Message}",
                    message.RetryCount + 1);
            }
            else
            {
                await _notificationService.NotifyDeadLetterAsync(
                    correlationId, 
                    ex.Message,
                    message.RetryCount);
            }

            throw;
        }
    }

    private async Task<DownloadResult> ExecuteDownloadAsync(
        DownloadMessage message, 
        CancellationToken cancellationToken)
    {
        try
        {
            return await _processingService.ProcessAsync(message, cancellationToken);
        }
        catch (Exception ex)
        {
            // Classificar e relançar
            var classified = ExceptionClassifier.ClassifyDownloadException(ex, message.Url);
            throw classified;
        }
    }
}

/// <summary>
/// Resultado de um download.
/// </summary>
public record DownloadResult
{
    public required string OutputPath { get; init; }
    public required long FileSizeBytes { get; init; }
    public TimeSpan? Duration { get; init; }
}
```

**appsettings.json:**
```json
{
  "RetryPolicy": {
    "MaxRetries": 3,
    "InitialDelaySeconds": 5,
    "BackoffMultiplier": 6,
    "MaxDelaySeconds": 120
  }
}
```

**Checklist:**
- [ ] Criar `RetryPolicyOptions` com configurações de retry
- [ ] Criar `TransientException` para erros temporários
- [ ] Criar `PermanentException` para erros permanentes
- [ ] Criar `RetryHandler` com backoff exponencial
- [ ] Criar `ExceptionClassifier` para categorizar erros
- [ ] Atualizar `DownloadConsumer` para usar retry
- [ ] Configurar injeção de dependência no Program.cs
- [ ] Adicionar configuração em appsettings.json

**Critérios de aceite:**
- ✅ Retry executa 3 tentativas para erros transitórios
- ✅ Backoff exponencial respeitado (5s → 30s → 2min)
- ✅ Erros permanentes vão direto para DLQ
- ✅ Erros transitórios são reclassificados automaticamente
- ✅ Logs detalhados de cada tentativa
- ✅ Retry count é reportado para API

---

### 3.5.2 Implementar Dead Letter Handler

**Estimativa:** 4-5 horas

**Arquivos:**
```
src/Cutube.Worker/
  ├── Handlers/
  │   └── DeadLetterHandler.cs
  └── Consumers/
      └── DlqConsumer.cs

src/Cutube.Api/
  └── Services/
      └── DlqService.cs
```

#### 3.5.2.1 Criar DeadLetterHandler

```csharp
// src/Cutube.Worker/Handlers/DeadLetterHandler.cs
using Cutube.Api.Queuing.Messages;
using Cutube.Worker.Services;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Cutube.Worker.Handlers;

/// <summary>
/// Handler para processar mensagens que chegam na Dead Letter Queue.
/// </summary>
public class DeadLetterHandler
{
    private readonly StatusNotificationService _notificationService;
    private readonly ILogger<DeadLetterHandler> _logger;

    public DeadLetterHandler(
        StatusNotificationService notificationService,
        ILogger<DeadLetterHandler> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    /// <summary>
    /// Processa uma mensagem que foi enviada para a DLQ.
    /// </summary>
    public async Task HandleAsync(
        ConsumeContext<DownloadMessage> context,
        Exception exception)
    {
        var message = context.Message;
        var correlationId = message.CorrelationId;

        _logger.LogError(
            exception,
            "Message moved to DLQ: {CorrelationId}, URL: {Url}, " +
            "RetryCount: {RetryCount}, Error: {ErrorMessage}",
            correlationId,
            message.Url,
            message.RetryCount,
            exception.Message);

        // Extrair informações detalhadas do erro
        var errorDetails = ExtractErrorDetails(exception);

        // Notificar API sobre a mensagem na DLQ
        await _notificationService.NotifyDeadLetterAsync(
            correlationId,
            errorDetails,
            message.RetryCount);

        // Log estruturado para análise
        _logger.LogInformation(
            "DLQ Message Logged: {@DlqMessage}",
            new
            {
                CorrelationId = correlationId,
                MessageId = message.MessageId,
                Url = message.Url,
                RetryCount = message.RetryCount,
                ErrorType = exception.GetType().Name,
                ErrorMessage = exception.Message,
                Timestamp = DateTime.UtcNow
            });
    }

    private string ExtractErrorDetails(Exception exception)
    {
        var details = new List<string>
        {
            $"Type: {exception.GetType().Name}",
            $"Message: {exception.Message}"
        };

        if (exception.InnerException != null)
        {
            details.Add($"Inner: {exception.InnerException.Message}");
        }

        // Extrair stack trace relevante (primeiras 5 linhas)
        if (!string.IsNullOrEmpty(exception.StackTrace))
        {
            var lines = exception.StackTrace
                .Split('\n')
                .Take(5)
                .Select(l => l.Trim());
            details.Add($"Stack: {string.Join(" | ", lines)}");
        }

        return string.Join(" | ", details);
    }
}
```

#### 3.5.2.2 Criar DlqConsumer

```csharp
// src/Cutube.Worker/Consumers/DlqConsumer.cs
using Cutube.Api.Queuing.Messages;
using Cutube.Worker.Handlers;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Cutube.Worker.Consumers;

/// <summary>
/// Consumer dedicado para a Dead Letter Queue.
/// Processa mensagens que falharam permanentemente.
/// </summary>
public class DlqConsumer : IConsumer<DownloadMessage>
{
    private readonly DeadLetterHandler _dlqHandler;
    private readonly ILogger<DlqConsumer> _logger;

    public DlqConsumer(
        DeadLetterHandler dlqHandler,
        ILogger<DlqConsumer> logger)
    {
        _dlqHandler = dlqHandler;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<DownloadMessage> context)
    {
        _logger.LogInformation(
            "Processing message from DLQ: {CorrelationId}",
            context.Message.CorrelationId);

        // Obter informações de erro do header MassTransit
        var faultMessage = context.Headers.Get<string>("MT-Fault-Message");
        var faultException = new Exception(faultMessage ?? "Unknown fault");

        await _dlqHandler.HandleAsync(context, faultException);

        // Acknowledge a mensagem (remover da DLQ após processar)
        // Se quisermos manter na DLQ para reprocessamento manual, não damos ack aqui
        // Mas para evitar duplicação, vamos ack e manter no repositório da API
    }
}
```

#### 3.5.2.3 Configurar DLQ no MassTransit

```csharp
// src/Cutube.Worker/Configuration/MassTransitConfiguration.cs
using Cutube.Worker.Configuration;
using Cutube.Worker.Consumers;
using MassTransit;
using Microsoft.Extensions.Options;

namespace Cutube.Worker.Configuration;

public static class MassTransitConfiguration
{
    public static void ConfigureMassTransit(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddMassTransit(x =>
        {
            // Consumer principal
            x.AddConsumer<DownloadConsumer>(cfg =>
            {
                cfg.UseMessageRetry(r =>
                {
                    r.Interval(3, TimeSpan.FromSeconds(5));
                });
                
                cfg.UseDelayedRedelivery(r =>
                {
                    r.Intervals(
                        TimeSpan.FromSeconds(5),
                        TimeSpan.FromSeconds(30),
                        TimeSpan.FromMinutes(2)
                    );
                });
            });

            // Consumer da DLQ
            x.AddConsumer<DlqConsumer>();

            x.UsingRabbitMq((context, cfg) =>
            {
                var options = context.GetRequiredService<IOptions<RabbitMqOptions>>().Value;

                cfg.Host(options.Host, options.Port, options.VirtualHost, h =>
                {
                    h.Username(options.UserName);
                    h.Password(options.Password);
                });

                // Configurar endpoint principal com DLQ
                cfg.ReceiveEndpoint(RabbitMqConfig.DownloadsQueue, e =>
                {
                    // Configurar DLQ
                    e.ConfigureDeadLetterQueue(
                        RabbitMqConfig.DlqExchange, 
                        RabbitMqConfig.DownloadsDlqQueue);
                    
                    // Prefetch count
                    e.PrefetchCount = RabbitMqConfig.PrefetchCount;
                    
                    // Configurar retry policy do MassTransit (redelivery)
                    e.UseDelayedRedelivery(r =>
                    {
                        r.Intervals(
                            TimeSpan.FromSeconds(5),
                            TimeSpan.FromSeconds(30),
                            TimeSpan.FromMinutes(2)
                        );
                    });

                    e.ConfigureConsumer<DownloadConsumer>(context);
                });

                // Configurar endpoint da DLQ
                cfg.ReceiveEndpoint(RabbitMqConfig.DownloadsDlqQueue, e =>
                {
                    e.ConfigureConsumer<DlqConsumer>(context);
                });
            });
        });
    }

    private static void ConfigureDeadLetterQueue(
        this IReceiveEndpointConfigurator endpoint,
        string exchangeName,
        string queueName)
    {
        // Configurar DLQ no RabbitMQ
        endpoint.Bind(exchangeName, e =>
        {
            e.RoutingKey = RabbitMqConfig.DlqRoutingKey;
            e.ExchangeType = "direct";
        });

        // Configurar dead letter exchange na fila
        endpoint.SetQuorumQueue();
        endpoint.DeadLetterExchange = exchangeName;
        endpoint.DeadLetterRoutingKey = queueName;
    }
}
```

**Checklist:**
- [ ] Criar `DeadLetterHandler` para processar mensagens na DLQ
- [ ] Criar `DlqConsumer` para consumir da fila DLQ
- [ ] Extrair e logar detalhes do erro
- [ ] Notificar API sobre mensagens na DLQ
- [ ] Configurar DLQ no MassTransit com dead letter exchange
- [ ] Configurar delayed redelivery para retry
- [ ] Registrar serviços no DI

**Critérios de aceite:**
- ✅ Mensagens falhas vão automaticamente para DLQ após retries
- ✅ DLQ consumer processa mensagens e loga detalhes
- ✅ API é notificada sobre mensagens na DLQ
- ✅ Headers de erro são extraídos e logados
- ✅ Dead letter exchange configurado corretamente


---

### 3.5.3 Criar Endpoints de Reprocessamento

**Estimativa:** 4-5 horas

**Arquivos:**
```
src/Cutube.Api/
  ├── Endpoints/
  │   └── DlqEndpoints.cs
  ├── Models/
  │   └── DlqMessageResponse.cs
  └── Services/
      └── DlqService.cs
```

#### 3.5.3.1 Criar DlqService

```csharp
// src/Cutube.Api/Services/DlqService.cs
using Cutube.Api.Data;
using Cutube.Api.Queuing;
using Cutube.Api.Queuing.Messages;
using Microsoft.Extensions.Logging;

namespace Cutube.Api.Services;

/// <summary>
/// Serviço para gerenciar a Dead Letter Queue.
/// </summary>
public class DlqService
{
    private readonly IDownloadStatusRepository _repository;
    private readonly IQueueProducer _producer;
    private readonly ILogger<DlqService> _logger;

    public DlqService(
        IDownloadStatusRepository repository,
        IQueueProducer producer,
        ILogger<DlqService> logger)
    {
        _repository = repository;
        _producer = producer;
        _logger = logger;
    }

    /// <summary>
    /// Lista todas as mensagens na DLQ (status DeadLetter).
    /// </summary>
    public async Task<IReadOnlyList<DownloadStatus>> GetDeadLetterMessagesAsync(
        CancellationToken cancellationToken = default)
    {
        return await _repository.GetByStatesAsync(
            new[] { DownloadState.DeadLetter },
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Reprocessa uma mensagem da DLQ.
    /// </summary>
    public async Task<bool> RetryAsync(
        string correlationId, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Attempting to retry DLQ message: {CorrelationId}",
            correlationId);

        // Buscar o status atual
        var status = await _repository.GetByCorrelationIdAsync(correlationId, cancellationToken);
        
        if (status == null)
        {
            _logger.LogWarning(
                "DLQ message not found: {CorrelationId}",
                correlationId);
            return false;
        }

        if (status.State != DownloadState.DeadLetter && status.State != DownloadState.Failed)
        {
            _logger.LogWarning(
                "Message {CorrelationId} is not in DLQ state. Current state: {State}",
                correlationId, status.State);
            return false;
        }

        // Criar nova mensagem para reprocessamento
        var message = new DownloadMessage
        {
            MessageId = Guid.NewGuid().ToString(),
            CorrelationId = correlationId, // Manter mesmo correlation ID para tracking
            Url = status.Url,
            StartTime = status.Metadata?.Duration, // Se tiver metadados
            EndTime = null,
            OutputPath = status.OutputPath ?? "/tmp/downloads",
            AudioOnly = status.AudioOnly,
            OutputFilename = status.OutputFilename,
            Priority = "normal",
            CreatedAt = DateTime.UtcNow,
            Metadata = new DownloadMetadata
            {
                Title = status.Metadata?.Title,
                Duration = status.Metadata?.Duration,
                Thumbnail = status.Metadata?.Thumbnail,
                Channel = status.Metadata?.Channel
            }
        };

        // Republicar na fila
        await _producer.PublishDownloadAsync(message, cancellationToken);

        // Atualizar status
        await _repository.UpdateAsync(correlationId, s =>
        {
            s.State = DownloadState.Queued;
            s.RetryCount++;
            s.ErrorMessage = null;
            s.UpdatedAt = DateTime.UtcNow;
        }, cancellationToken);

        _logger.LogInformation(
            "DLQ message requeued successfully: {CorrelationId}, RetryCount: {RetryCount}",
            correlationId, status.RetryCount + 1);

        return true;
    }

    /// <summary>
    /// Remove uma mensagem da DLQ (descarta permanentemente).
    /// </summary>
    public async Task<bool> DiscardAsync(
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Discarding DLQ message: {CorrelationId}",
            correlationId);

        var status = await _repository.GetByCorrelationIdAsync(correlationId, cancellationToken);
        
        if (status == null)
        {
            return false;
        }

        // Atualizar status para Cancelled (ou remover do repositório)
        await _repository.UpdateAsync(correlationId, s =>
        {
            s.State = DownloadState.Cancelled;
            s.ErrorMessage = "Discarded by user from DLQ";
            s.UpdatedAt = DateTime.UtcNow;
        }, cancellationToken);

        _logger.LogInformation(
            "DLQ message discarded: {CorrelationId}",
            correlationId);

        return true;
    }

    /// <summary>
    /// Reprocessa todas as mensagens da DLQ.
    /// </summary>
    public async Task<BatchRetryResult> RetryAllAsync(
        CancellationToken cancellationToken = default)
    {
        var messages = await GetDeadLetterMessagesAsync(cancellationToken);
        var results = new BatchRetryResult();

        foreach (var message in messages)
        {
            try
            {
                var success = await RetryAsync(message.CorrelationId, cancellationToken);
                if (success)
                    results.SuccessCount++;
                else
                    results.FailedCount++;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to retry message: {CorrelationId}",
                    message.CorrelationId);
                results.FailedCount++;
            }
        }

        results.TotalCount = messages.Count;
        return results;
    }
}

/// <summary>
/// Resultado de reprocessamento em lote.
/// </summary>
public class BatchRetryResult
{
    public int TotalCount { get; set; }
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
}
```

#### 3.5.3.2 Criar DlqEndpoints

```csharp
// src/Cutube.Api/Endpoints/DlqEndpoints.cs
using Cutube.Api.Data;
using Cutube.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Cutube.Api.Endpoints;

/// <summary>
/// Endpoints para gerenciamento da Dead Letter Queue.
/// </summary>
public static class DlqEndpoints
{
    public static void MapDlqEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/downloads/dlq")
            .WithTags("Dead Letter Queue")
            .WithOpenApi();

        // GET /api/downloads/dlq - Listar mensagens na DLQ
        group.MapGet("/", async (
            DlqService dlqService,
            CancellationToken ct) =>
        {
            var messages = await dlqService.GetDeadLetterMessagesAsync(ct);
            
            return Results.Ok(new
            {
                TotalCount = messages.Count,
                Messages = messages.Select(m => new
                {
                    m.CorrelationId,
                    m.Url,
                    m.State,
                    m.ErrorMessage,
                    m.RetryCount,
                    m.CreatedAt,
                    m.UpdatedAt
                })
            });
        })
        .WithName("GetDlqMessages")
        .WithSummary("List all messages in Dead Letter Queue")
        .WithOpenApi();

        // POST /api/downloads/dlq/{correlationId}/retry - Reprocessar mensagem
        group.MapPost("/{correlationId}/retry", async (
            string correlationId,
            DlqService dlqService,
            ILogger<Program> logger,
            CancellationToken ct) =>
        {
            try
            {
                var success = await dlqService.RetryAsync(correlationId, ct);
                
                if (!success)
                {
                    return Results.NotFound(new
                    {
                        error = "Message not found or not in DLQ state",
                        correlationId
                    });
                }

                logger.LogInformation(
                    "DLQ message retried: {CorrelationId}",
                    correlationId);

                return Results.Ok(new
                {
                    message = "Message requeued for processing",
                    correlationId,
                    status = "queued"
                });
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Failed to retry DLQ message: {CorrelationId}",
                    correlationId);
                
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: 500,
                    title: "Failed to retry message");
            }
        })
        .WithName("RetryDlqMessage")
        .WithSummary("Retry a message from Dead Letter Queue")
        .WithOpenApi();

        // POST /api/downloads/dlq/retry-all - Reprocessar todas as mensagens
        group.MapPost("/retry-all", async (
            DlqService dlqService,
            ILogger<Program> logger,
            CancellationToken ct) =>
        {
            try
            {
                var result = await dlqService.RetryAllAsync(ct);

                logger.LogInformation(
                    "DLQ batch retry completed. Total: {Total}, Success: {Success}, Failed: {Failed}",
                    result.TotalCount, result.SuccessCount, result.FailedCount);

                return Results.Ok(new
                {
                    message = "Batch retry completed",
                    totalCount = result.TotalCount,
                    successCount = result.SuccessCount,
                    failedCount = result.FailedCount
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to execute batch retry");
                
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: 500,
                    title: "Failed to retry messages");
            }
        })
        .WithName("RetryAllDlqMessages")
        .WithSummary("Retry all messages in Dead Letter Queue")
        .WithOpenApi();

        // DELETE /api/downloads/dlq/{correlationId} - Descartar mensagem
        group.MapDelete("/{correlationId}", async (
            string correlationId,
            DlqService dlqService,
            ILogger<Program> logger,
            CancellationToken ct) =>
        {
            try
            {
                var success = await dlqService.DiscardAsync(correlationId, ct);
                
                if (!success)
                {
                    return Results.NotFound(new
                    {
                        error = "Message not found",
                        correlationId
                    });
                }

                logger.LogInformation(
                    "DLQ message discarded: {CorrelationId}",
                    correlationId);

                return Results.NoContent();
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Failed to discard DLQ message: {CorrelationId}",
                    correlationId);
                
                return Results.Problem(
                    detail: ex.Message,
                    statusCode: 500,
                    title: "Failed to discard message");
            }
        })
        .WithName("DiscardDlqMessage")
        .WithSummary("Discard a message from Dead Letter Queue")
        .WithOpenApi();
    }
}
```

**Checklist:**
- [ ] Criar `DlqService` para gerenciar reprocessamento
- [ ] Implementar `RetryAsync` para reprocessar mensagem individual
- [ ] Implementar `RetryAllAsync` para reprocessar todas
- [ ] Implementar `DiscardAsync` para descartar mensagem
- [ ] Criar `DlqEndpoints` com endpoints REST
- [ ] Registrar serviço e endpoints no DI
- [ ] Adicionar logging de auditoria

**Critérios de aceite:**
- ✅ Endpoint GET lista mensagens na DLQ
- ✅ Endpoint POST retry reprocessa mensagem individual
- ✅ Endpoint POST retry-all reprocessa todas as mensagens
- ✅ Endpoint DELETE descarta mensagem
- ✅ Retry incrementa o contador de tentativas
- ✅ Correlation ID é preservado no reprocessamento
- ✅ Logs de auditoria para todas as operações

