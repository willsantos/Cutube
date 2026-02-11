# Fase 3.3: Consumer (Worker Dedicado)

**Status:** 🎯 Planejamento
**Épico:** Cutube-858 (Épico 3: Integração com RabbitMQ)
**Duração:** 5-6 dias
**Responsável:** Backend Developer
**Prioridade:** 🔥 Alta
**Dependência:** ✅ Fase 3.2 (Producer Queue) completa

---

## Objetivo

Implementar o **Worker Service dedicado** (`Cutube.Worker`) que consumirá mensagens da fila RabbitMQ, processará downloads usando yt-dlp/ffmpeg, e notificará a API sobre o progresso e status. Esta fase cria o coração do processamento assíncrono do sistema Cutube.

**Benefícios:**
- ✅ **Desacoplamento completo:** API responde instantaneamente, worker processa assincronamente
- ✅ **Escalabilidade horizontal:** Múltiplas instâncias do worker podem rodar em paralelo
- ✅ **Resiliência:** Mensagens persistem na fila até serem processadas
- ✅ **Control de concurrency:** Limite configurável de downloads simultâneos
- ✅ **Graceful shutdown:** Worker termina downloads em andamento antes de parar

---

## Visão Arquitetural

### Fluxo de Processamento do Worker

```
┌─────────────────────────────────────────────────────────────────────────┐
│                         Cutube.Worker Service                           │
│                                                                         │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │                   Program.cs (Startup)                           │   │
│  │                                                                   │   │
│  │  • Configurar MassTransit (Consumer)                             │   │
│  │  • Configurar Serilog                                           │   │
│  │  • Registrar DI services                                         │   │
│  │  • Configurar hosted services                                   │   │
│  └──────────────────────────┬──────────────────────────────────────┘   │
│                             │                                           │
│  ┌──────────────────────────▼──────────────────────────────────────┐   │
│  │              DownloadConsumer (MassTransit)                      │   │
│  │                                                                   │   │
│  │  • Consome mensagens de cutube.downloads                         │   │
│  │  • Prefetch count: 1 (processamento sequencial por mensagem)   │   │
│  │  • Ack manual após processamento completo                        │   │
│  │  • Retry policy: 3 tentativas com backoff                         │   │
│  │  • Dead Letter Queue após esgotar retries                        │   │
│  └──────────────────────────┬──────────────────────────────────────┘   │
│                             │                                           │
│                             ▼                                           │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │         DownloadProcessingService (Domain Logic)                  │   │
│  │                                                                   │   │
│  │  1. Validar mensagem (URL, output path)                          │   │
│  │  2. Notificar API: status = "processing"                         │   │
│  │  3. Executar download:                                           │   │
│  │     • Invocar yt-dlp via Process/CLI                             │   │
│  │     • Parse progress output                                      │   │
│  │     • Report progress para API (periodicamente)                   │   │
│  │  4. Handle resultado:                                            │   │
│  │     • Sucesso: notificar "completed"                              │   │
│  │     • Erro temporário: retry                                     │   │
│  │     • Erro permanente: enviar para DLQ                          │   │
│  └──────────────────────────┬──────────────────────────────────────┘   │
│                             │                                           │
│                             ▼                                           │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │        StatusNotificationService (API Communication)              │   │
│  │                                                                   │   │
│  │  • PATCH /api/downloads/{id}/status                              │   │
│  │  • POST /api/downloads/{id}/progress                            │   │
│  │  • SignalR Hub broadcast (via API)                               │   │
│  │  • Retry com exponential backoff                                 │   │
│  │  • Buffer de updates (não flood a API)                           │   │
│  └──────────────────────────┬──────────────────────────────────────┘   │
│                             │                                           │
└─────────────────────────────┼───────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                              yt-dlp / ffmpeg                            │
│                                                                         │
│  • Download de vídeos YouTube                                          │
│  • Conversão de formatos                                               │
│  • Extração de áudio                                                    │
│  • Corte por timestamps                                                │
└─────────────────────────────────────────────────────────────────────────┘
```

### Estados de uma Mensagem no Worker

```
┌──────────┐     ┌──────────┐     ┌──────────────┐     ┌─────────────┐
│ Received │ ──► │ Consumed │ ──► │ Processing   │ ──► │ Acked       │
│          │     │          │     │              │     │             │
└──────────┘     └──────────┘     └──────────────┘     └─────────────┘
                      │                   │                    │
                      │                   ▼                    │
                      │            ┌──────────┐               │
                      │            │ Failed   │               │
                      │            │ (retry)  │               │
                      │            └──────────┘               │
                      │                   │                    │
                      ▼                   ▼                    ▼
               ┌──────────────┐   ┌─────────────┐     ┌─────────────┐
               │ DLQ          │   │ Cancelled   │     │ Completed   │
               │ (3 retries)  │   │ (SIGTERM)   │     │             │
               └──────────────┘   └─────────────┘     └─────────────┘
```

### Configuração de Concurrency

```
┌─────────────────────────────────────────────────────────────────┐
│            Worker Concurrency Control                           │
│                                                                 │
│  MaxConcurrentMessages: 3                                       │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐                     │
│  │ Download │  │ Download │  │ Download │  ← Ativos           │
│  │    #1    │  │    #2    │  │    #3    │                     │
│  └──────────┘  └──────────┘  └──────────┘                     │
│                                                                 │
│  [Message #4] [Message #5] [Message #6]                        │
│       ▼          ▼          ▼                                   │
│   ┌─────────────────────────────────────────────────────┐     │
│   │            RabbitMQ Queue (waiting)                  │     │
│   └─────────────────────────────────────────────────────┘     │
└─────────────────────────────────────────────────────────────────┘
```

---

## Tarefas

### 3.3.1 Criar Projeto Worker Service

**Estimativa:** 3-4 horas

**Arquivos:**
```
Cutube.Worker/
  ├── Program.cs
  ├── Worker.cs
  ├── appsettings.json
  ├── appsettings.Development.json
  ├── Cutube.Worker.csproj
  └── Configuration/
      └── WorkerOptions.cs
```

#### 3.3.1.1 Criar Projeto

**Comando:**
```bash
# Na raiz do projeto
dotnet new worker -n Cutube.Worker -o src/Cutube.Worker

# Adicionar referência ao Domain
dotnet add src/Cutube.Worker reference src/Cutube.Domain

# Adicionar pacotes NuGet
dotnet add src/Cutube.Worker package MassTransit.RabbitMQ
dotnet add src/Cutube.Worker package MassTransit.AspNetCore
dotnet add src/Cutube.Worker package Serilog.AspNetCore
dotnet add src/Cutube.Worker package Serilog.Sinks.Console
dotnet add src/Cutube.Worker package Serilog.Sinks.File
dotnet add src/Cutube.Worker package Microsoft.Extensions.Hosting
dotnet add src/Cutube.Worker package Polly
```

#### 3.3.1.2 Configurar .csproj

```xml
<!-- src/Cutube.Worker/Cutube.Worker.csproj -->
<Project Sdk="Microsoft.NET.Sdk.Worker">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <RootNamespace>Cutube.Worker</RootNamespace>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="MassTransit.RabbitMQ" Version="8.3.0" />
    <PackageReference Include="MassTransit.AspNetCore" Version="8.3.0" />
    <PackageReference Include="Serilog.AspNetCore" Version="8.0.0" />
    <PackageReference Include="Serilog.Sinks.Console" Version="5.0.0" />
    <PackageReference Include="Serilog.Sinks.File" Version="5.0.0" />
    <PackageReference Include="Microsoft.Extensions.Hosting" Version="9.0.0" />
    <PackageReference Include="Polly" Version="8.4.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\Cutube.Domain\Cutube.Domain.csproj" />
  </ItemGroup>
</Project>
```

#### 3.3.1.3 Configurar appsettings.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  },
  "RabbitMq": {
    "Host": "localhost",
    "Port": 5672,
    "VirtualHost": "/",
    "UserName": "cutube",
    "Password": "cutube123",
    "RetryCount": 5,
    "Timeout": 30
  },
  "Worker": {
    "MaxConcurrentDownloads": 3,
    "ProgressUpdateInterval": 2000,
    "ApiBaseUrl": "http://localhost:5000",
    "DownloadTimeoutMinutes": 30,
    "EnableGracefulShutdown": true,
    "GracefulShutdownTimeoutSeconds": 30
  },
  "Serilog": {
    "Using": [ "Serilog.Sinks.Console", "Serilog.Sinks.File" ],
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "MassTransit": "Warning",
        "Microsoft": "Warning"
      }
    },
    "WriteTo": [
      { "Name": "Console", "Args": { "outputTemplate": "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}" } },
      { "Name": "File", "Args": { "path": "logs/cutube-worker-.log", "rollingInterval": "Day" } }
    ]
  }
}
```

#### 3.3.1.4 Criar WorkerOptions

```csharp
// src/Cutube.Worker/Configuration/WorkerOptions.cs
namespace Cutube.Worker.Configuration;

public class WorkerOptions
{
    public const string SectionName = "Worker";

    /// <summary>
    /// Número máximo de downloads simultâneos.
    /// Padrão: 3
    /// </summary>
    public int MaxConcurrentDownloads { get; set; } = 3;

    /// <summary>
    /// Intervalo em ms entre updates de progresso para a API.
    /// Padrão: 2000ms (2 segundos)
    /// </summary>
    public int ProgressUpdateInterval { get; set; } = 2000;

    /// <summary>
    /// URL base da API Cutube para callbacks de status.
    /// </summary>
    public required string ApiBaseUrl { get; set; } = "http://localhost:5000";

    /// <summary>
    /// Timeout máximo para um download em minutos.
    /// Padrão: 30 minutos
    /// </summary>
    public int DownloadTimeoutMinutes { get; set; } = 30;

    /// <summary>
    /// Habilita graceful shutdown (termina downloads em andamento).
    /// </summary>
    public bool EnableGracefulShutdown { get; set; } = true;

    /// <summary>
    /// Timeout em segundos para graceful shutdown.
    /// Padrão: 30 segundos
    /// </summary>
    public int GracefulShutdownTimeoutSeconds { get; set; } = 30;
}
```

#### 3.3.1.5 Configurar Program.cs

```csharp
// src/Cutube.Worker/Program.cs
using Cutube.Worker.Configuration;
using Cutube.Worker.Consumers;
using Cutube.Worker.Services;
using MassTransit;
using Serilog;
using Cutube.Api.Queuing.Messages;

IHost host = Host.CreateDefaultBuilder(args)
    .UseSerilog((context, services, loggerConfiguration) =>
    {
        loggerConfiguration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext();
    })
    .ConfigureServices((context, services) =>
    {
        // Configuration
        services.Configure<WorkerOptions>(
            context.Configuration.GetSection(WorkerOptions.SectionName)
        );

        // HttpClient para comunicação com API
        services.AddHttpClient<IDownloadStatusNotificationService, DownloadStatusNotificationService>(client =>
        {
            var apiBaseUrl = context.Configuration.GetValue<string>("Worker:ApiBaseUrl") ?? "http://localhost:5000";
            client.BaseAddress = new Uri(apiBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
        })
        .AddPolicyHandler(GetRetryPolicy());

        // Domain services
        services.AddSingleton<IDownloadProcessingService, DownloadProcessingService>();

        // MassTransit (Consumer)
        services.AddMassTransit(x =>
        {
            x.AddConsumer<DownloadConsumer>();

            x.UsingRabbitMq((context, cfg) =>
            {
                var rabbitMqConfig = context.GetRequiredService<RabbitMqOptions>();

                cfg.Host(rabbitMqConfig.Host, rabbitMqConfig.Port, rabbitMqConfig.VirtualHost, h =>
                {
                    h.Username(rabbitMqConfig.UserName);
                    h.Password(rabbitMqConfig.Password);
                });

                cfg.ReceiveEndpoint("cutube.downloads", e =>
                {
                    e.ConfigureConsumer<DownloadConsumer>(context);

                    // Prefetch count: 1 mensagem por vez (evita sobrecarga)
                    e.PrefetchCount = 1;

                    // Concurrent message limit
                    var workerOptions = context.GetRequiredService<IOptions<WorkerOptions>>().Value;
                    e.ConcurrentMessageLimit = workerOptions.MaxConcurrentDownloads;

                    // Retry policy: 3 tentativas com backoff exponencial
                    e.UseMessageRetry(r =>
                    {
                        r.Intervals(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(30), TimeSpan.FromMinutes(2));
                        r.Handle<Exception>();
                    });

                    // Dead Letter Queue após esgotar retries
                    e.UseMessageUoW();
                });
            });
        });

        services.AddMassTransitHostedService(true);

        // Worker service
        services.AddHostedService<Worker>();
    })
    .Build();

await host.RunAsync();

static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    return Policy<HttpResponseMessage>
        .Handle<HttpRequestException>()
        .OrResult(msg => !msg.IsSuccessStatusCode)
        .WaitAndRetryAsync(
            retryCount: 3,
            sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
            onRetry: (outcome, timespan, retryAttempt, context) =>
            {
                Console.WriteLine($"Retry {retryAttempt} after {timespan.TotalSeconds}s due to: {outcome.Exception?.Message}");
            });
}
```

**Checklist:**
- [ ] Criar projeto Worker Service com dotnet new worker
- [ ] Adicionar referência ao Cutube.Domain
- [ ] Instalar pacotes NuGet (MassTransit, Serilog, Polly)
- [ ] Configurar appsettings.json
- [ ] Criar WorkerOptions.cs
- [ ] Configurar Serilog
- [ ] Configurar MassTransit consumer
- [ ] Configurar HttpClient com retry policy
- [ ] Testar startup do worker

**Critérios de aceite:**
- ✅ Worker inicia sem erros
- ✅ Serilog loga no console e arquivo
- ✅ MassTransit conecta ao RabbitMQ
- ✅ WorkerOptions carregado corretamente
- ✅ Graceful shutdown funciona (Ctrl+C)

---

### 3.3.2 Implementar RabbitMqConsumer

**Estimativa:** 4-5 horas

**Arquivos:**
```
Cutube.Worker/Consumers/
  ├── DownloadConsumer.cs
  └── DownloadConsumerDefinition.cs (opcional)
```

#### 3.3.2.1 Criar DownloadConsumer

```csharp
// src/Cutube.Worker/Consumers/DownloadConsumer.cs
using Cutube.Api.Queuing.Messages;
using Cutube.Worker.Configuration;
using Cutube.Worker.Services;
using MassTransit;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;

namespace Cutube.Worker.Consumers;

/// <summary>
/// Consumer MassTransit para processar mensagens de download da fila RabbitMQ.
/// </summary>
public class DownloadConsumer : IConsumer<DownloadMessage>
{
    private readonly IDownloadProcessingService _processingService;
    private readonly ILogger<DownloadConsumer> _logger;
    private readonly WorkerOptions _options;

    public DownloadConsumer(
        IDownloadProcessingService processingService,
        ILogger<DownloadConsumer> logger,
        IOptions<WorkerOptions> options)
    {
        _processingService = processingService;
        _logger = logger;
        _options = options.Value;
    }

    public async Task Consume(ConsumeContext<DownloadMessage> context)
    {
        var message = context.Message;
        var correlationId = message.CorrelationId;

        _logger.LogInformation(
            "Received download message: {CorrelationId}, URL: {Url}",
            correlationId,
            message.Url);

        try
        {
            // Validar mensagem
            ValidateMessage(message);

            // Processar download com timeout
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(_options.DownloadTimeoutMinutes));

            // Linkar cancelamento do contexto com o CTS
            await using var registration = context.CancellationToken.Register(() => cts.Cancel());

            await _processingService.ProcessDownloadAsync(message, cts.Token);

            _logger.LogInformation(
                "Download completed successfully: {CorrelationId}",
                correlationId);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning(
                "Download cancelled: {CorrelationId}",
                correlationId);
            throw; // Re-throw para MassTransit handle (retry ou DLQ)
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error processing download: {CorrelationId}. Error: {Error}",
                correlationId,
                ex.Message);

            // Re-throw para MassTransit retry policy
            throw;
        }
    }

    private void ValidateMessage(DownloadMessage message)
    {
        if (string.IsNullOrWhiteSpace(message.Url))
        {
            throw new ArgumentException("URL is required", nameof(message));
        }

        if (!Uri.TryCreate(message.Url, UriKind.Absolute, out _))
        {
            throw new ArgumentException("Invalid URL format", nameof(message));
        }

        if (string.IsNullOrWhiteSpace(message.OutputPath))
        {
            throw new ArgumentException("Output path is required", nameof(message));
        }
    }
}
```

**Checklist:**
- [ ] Criar diretório Consumers/
- [ ] Criar DownloadConsumer.cs
- [ ] Implementar Consume method
- [ ] Adicionar validação de mensagem
- [ ] Adicionar logging detalhado
- [ ] Configurar timeout
- [ ] Testar consumo de mensagens

**Critérios de aceite:**
- ✅ Consumer recebe mensagens da fila
- ✅ Validação funciona corretamente
- ✅ Timeout configurado e respeitado
- ✅ Exceções são propagadas para retry/DLQ
- ✅ Logging detalhado em cada etapa

---

### 3.3.3 Implementar DownloadProcessingService

**Estimativa:** 8-10 horas

**Arquivos:**
```
Cutube.Worker/Services/
  ├── IDownloadProcessingService.cs
  ├── DownloadProcessingService.cs
  └── Models/
      └── DownloadProgress.cs
```

#### 3.3.3.1 Criar Interface e Models

```csharp
// src/Cutube.Worker/Services/IDownloadProcessingService.cs
using Cutube.Api.Queuing.Messages;

namespace Cutube.Worker.Services;

/// <summary>
/// Serviço responsável por processar downloads de vídeo.
/// </summary>
public interface IDownloadProcessingService
{
    /// <summary>
    /// Processa um download de vídeo.
    /// </summary>
    /// <param name="message">Mensagem de download com parâmetros.</param>
    /// <param name="cancellationToken">Token de cancelamento.</param>
    Task ProcessDownloadAsync(DownloadMessage message, CancellationToken cancellationToken = default);
}
```

```csharp
// src/Cutube.Worker/Services/Models/DownloadProgress.cs
namespace Cutube.Worker.Services.Models;

/// <summary>
/// Progresso de download reportado pelo yt-dlp.
/// </summary>
public class DownloadProgress
{
    public required string CorrelationId { get; init; }
    public int Progress { get; set; }
    public double Speed { get; set; }
    public long DownloadedBytes { get; set; }
    public long? TotalBytes { get; set; }
    public string? Eta { get; set; }
    public string? Status { get; set; }
}
```

#### 3.3.3.2 Criar DownloadProcessingService

```csharp
// src/Cutube.Worker/Services/DownloadProcessingService.cs
using Cutube.Api.Queuing.Messages;
using Cutube.Worker.Configuration;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Cutube.Worker.Services;

/// <summary>
/// Implementação do serviço de processamento de downloads usando yt-dlp.
/// </summary>
public partial class DownloadProcessingService : IDownloadProcessingService
{
    private readonly IDownloadStatusNotificationService _notificationService;
    private readonly ILogger<DownloadProcessingService> _logger;
    private readonly WorkerOptions _options;

    public DownloadProcessingService(
        IDownloadStatusNotificationService notificationService,
        ILogger<DownloadProcessingService> logger,
        IOptions<WorkerOptions> options)
    {
        _notificationService = notificationService;
        _logger = logger;
        _options = options.Value;
    }

    public async Task ProcessDownloadAsync(DownloadMessage message, CancellationToken cancellationToken = default)
    {
        var correlationId = message.CorrelationId;

        _logger.LogInformation("Starting download: {CorrelationId}, URL: {Url}", correlationId, message.Url);

        try
        {
            // Notificar início do processamento
            await _notificationService.NotifyStatusAsync(correlationId, "processing", cancellationToken);

            // Criar diretório de saída se não existir
            var outputDirectory = Path.GetDirectoryName(message.OutputPath);
            if (!string.IsNullOrEmpty(outputDirectory) && !Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
                _logger.LogDebug("Created output directory: {Dir}", outputDirectory);
            }

            // Construir argumentos do yt-dlp
            var arguments = BuildYtDlpArguments(message);

            _logger.LogDebug("yt-dlp arguments: {Args}", arguments);

            // Executar yt-dlp
            var exitCode = await ExecuteYtDlpAsync(
                arguments,
                correlationId,
                cancellationToken);

            if (exitCode == 0)
            {
                // Sucesso
                await _notificationService.NotifyStatusAsync(correlationId, "completed", cancellationToken);
                _logger.LogInformation("Download completed: {CorrelationId}", correlationId);
            }
            else if (exitCode == 1 && cancellationToken.IsCancellationRequested)
            {
                // Cancelado pelo usuário (CTRL+C no worker, não CTRL+C no yt-dlp)
                await _notificationService.NotifyStatusAsync(correlationId, "cancelled", cancellationToken);
                _logger.LogWarning("Download cancelled: {CorrelationId}", correlationId);
            }
            else
            {
                // Erro
                await _notificationService.NotifyStatusAsync(
                    correlationId,
                    "failed",
                    cancellationToken,
                    $"yt-dlp exited with code {exitCode}");

                throw new Exception($"yt-dlp failed with exit code {exitCode}");
            }
        }
        catch (OperationCanceledException)
        {
            await _notificationService.NotifyStatusAsync(correlationId, "cancelled", cancellationToken);
            _logger.LogWarning("Download cancelled by user: {CorrelationId}", correlationId);
            throw;
        }
        catch (Exception ex)
        {
            await _notificationService.NotifyStatusAsync(
                correlationId,
                "failed",
                cancellationToken,
                ex.Message);

            _logger.LogError(ex, "Download failed: {CorrelationId}", correlationId);
            throw;
        }
    }

    private string BuildYtDlpArguments(DownloadMessage message)
    {
        var args = new List<string>();

        // Output filename
        var outputFilename = !string.IsNullOrEmpty(message.OutputFilename)
            ? message.OutputFilename
            : "%(title)s.%(ext)s";

        args.Add("--newline"); // Progress em uma linha
        args.Add("--no-playlist"); // Não baixar playlist inteira
        args.Add("--ignore-errors"); // Continuar em caso de erro
        args.Add("--no-overwrites"); // Não sobrescrever arquivos existentes

        // Audio only
        if (message.AudioOnly)
        {
            args.Add("-x"); // Extração de áudio
            args.Add("--audio-format"); // Formato de áudio
            args.Add("mp3");
        }

        // Time range (recorte)
        if (!string.IsNullOrEmpty(message.StartTime) && !string.IsNullOrEmpty(message.EndTime))
        {
            args.Add("--download-sections");
            args.Add($"*{message.StartTime}-{message.EndTime}");
        }

        // Output path
        args.Add("-o");
        args.Add(Path.Combine(message.OutputPath, outputFilename));

        // URL (último argumento)
        args.Add(message.Url);

        return string.Join(" ", args);
    }

    private async Task<int> ExecuteYtDlpAsync(
        string arguments,
        string correlationId,
        CancellationToken cancellationToken)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "yt-dlp",
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();

        // Task para ler output e parse progress
        var progressTask = ReadProgressAsync(
            process.StandardOutput,
            correlationId,
            cancellationToken);

        // Task para ler erros
        var errorTask = ReadErrorsAsync(
            process.StandardError,
            correlationId,
            cancellationToken);

        // Esperar processo terminar
        await process.WaitForExitAsync(cancellationToken);

        // Esperar tasks de leitura terminarem
        await Task.WhenAll(progressTask, errorTask);

        return process.ExitCode;
    }

    private async Task ReadProgressAsync(
        StreamReader reader,
        string correlationId,
        CancellationToken cancellationToken)
    {
        var lastUpdate = DateTime.MinValue;
        var updateInterval = TimeSpan.FromMilliseconds(_options.ProgressUpdateInterval);

        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            // Parse progress line do yt-dlp
            var progress = ParseYtDlpProgress(line, correlationId);

            if (progress != null)
            {
                _logger.LogDebug(
                    "Download progress: {CorrelationId}, {Progress}%, Speed: {Speed}",
                    correlationId,
                    progress.Progress,
                    progress.Speed);

                // Throttle updates para não flood a API
                var now = DateTime.UtcNow;
                if (now - lastUpdate >= updateInterval || progress.Progress == 100)
                {
                    await _notificationService.NotifyProgressAsync(
                        progress.CorrelationId,
                        progress.Progress,
                        progress.Speed,
                        progress.DownloadedBytes,
                        progress.TotalBytes,
                        progress.Eta,
                        cancellationToken);

                    lastUpdate = now;
                }
            }
        }
    }

    private async Task ReadErrorsAsync(
        StreamReader reader,
        string correlationId,
        CancellationToken cancellationToken)
    {
        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            _logger.LogWarning("yt-dlp stderr [{CorrelationId}]: {Line}", correlationId, line);
        }
    }

    private DownloadProgress? ParseYtDlpProgress(string line, string correlationId)
    {
        // Exemplo de line do yt-dlp:
        // [download]  45.0% of 100.00MiB at  2.00MiB/s ETA 00:00:05

        var match = YtDlpProgressRegex().Match(line);
        if (!match.Success)
        {
            return null;
        }

        var progress = new DownloadProgress
        {
            CorrelationId = correlationId,
            Progress = int.Parse(match.Groups["percent"].Value),
            Speed = ParseSpeed(match.Groups["speed"].Value),
            DownloadedBytes = ParseBytes(match.Groups["downloaded"].Value, match.Groups["downloadedUnit"].Value),
            Eta = match.Groups["eta"].Value
        };

        return progress;
    }

    private double ParseSpeed(string speed)
    {
        // Ex: "2.00MiB/s", "500.00KiB/s"
        var match = SpeedRegex().Match(speed);
        if (!match.Success)
        {
            return 0;
        }

        var value = double.Parse(match.Groups["value"].Value, CultureInfo.InvariantCulture);
        var unit = match.Groups["unit"].Value.ToUpperInvariant();

        return unit switch
        {
            "MIB/S" => value * 1024 * 1024,
            "KIB/S" => value * 1024,
            "GIB/S" => value * 1024 * 1024 * 1024,
            _ => value
        };
    }

    private long ParseBytes(string value, string unit)
    {
        var num = double.Parse(value, CultureInfo.InvariantCulture);
        var unitUpper = unit.ToUpperInvariant();

        return unitUpper switch
        {
            "MIB" => (long)(num * 1024 * 1024),
            "KIB" => (long)(num * 1024),
            "GIB" => (long)(num * 1024 * 1024 * 1024),
            _ => (long)num
        };
    }

    [GeneratedRegex(@"\[download\]\s+(?<percent>\d+\.?\d*)% of (?<downloaded>[\d.]+)(?<downloadedUnit>MiB|GiB|KiB) at (?<speed>[\d.]+\s*[KMG]iB/s) ETA (?<eta>[\d:]+)")]
    private static partial Regex YtDlpProgressRegex();

    [GeneratedRegex(@"(?<value>[\d.]+)\s*(?<unit>KiB/s|MiB/s|GiB/s)")]
    private static partial Regex SpeedRegex();
}
```

**Checklist:**
- [ ] Criar IDownloadProcessingService interface
- [ ] Criar DownloadProgress model
- [ ] Criar DownloadProcessingService
- [ ] Implementar BuildYtDlpArguments
- [ ] Implementar ExecuteYtDlpAsync
- [ ] Implementar ReadProgressAsync
- [ ] Implementar ParseYtDlpProgress (com regex)
- [ ] Testar download simples (sem recorte)
- [ ] Testar download com recorte de tempo
- [ ] Testar download audio-only
- [ ] Testar cancelamento

**Critérios de aceite:**
- ✅ yt-dlp é invocado corretamente
- ✅ Progress é parseado e reportado
- ✅ Sucesso é notificado como "completed"
- ✅ Erro é notificado como "failed"
- ✅ Cancelamento é notificado como "cancelled"
- ✅ Throttle de updates funciona
- ✅ Timeout é respeitado

---

### 3.3.4 Implementar StatusNotificationService

**Estimativa:** 5-6 horas

**Arquivos:**
```
Cutube.Worker/Services/
  ├── IDownloadStatusNotificationService.cs
  └── DownloadStatusNotificationService.cs
```

#### 3.3.4.1 Criar Interface

```csharp
// src/Cutube.Worker/Services/IDownloadStatusNotificationService.cs
namespace Cutube.Worker.Services;

/// <summary>
/// Serviço para notificar a API sobre status e progresso de downloads.
/// </summary>
public interface IDownloadStatusNotificationService
{
    /// <summary>
    /// Notifica mudança de status do download.
    /// </summary>
    Task NotifyStatusAsync(
        string correlationId,
        string status,
        CancellationToken cancellationToken = default,
        string? errorMessage = null);

    /// <summary>
    /// Notifica progresso do download.
    /// </summary>
    Task NotifyProgressAsync(
        string correlationId,
        int progress,
        double speed,
        long downloadedBytes,
        long? totalBytes = null,
        string? eta = null,
        CancellationToken cancellationToken = default);
}
```

#### 3.3.4.2 Criar DownloadStatusNotificationService

```csharp
// src/Cutube.Worker/Services/DownloadStatusNotificationService.cs
using System.Text;
using System.Text.Json;

namespace Cutube.Worker.Services;

/// <summary>
/// Implementação do serviço de notificação de status via HTTP callbacks para a API.
/// </summary>
public partial class DownloadStatusNotificationService : IDownloadStatusNotificationService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<DownloadStatusNotificationService> _logger;

    public DownloadStatusNotificationService(
        HttpClient httpClient,
        ILogger<DownloadStatusNotificationService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task NotifyStatusAsync(
        string correlationId,
        string status,
        CancellationToken cancellationToken = default,
        string? errorMessage = null)
    {
        var url = $"/api/downloads/{correlationId}/status";

        var payload = new
        {
            state = status,
            errorMessage = errorMessage
        };

        try
        {
            var content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PatchAsync(url, content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Failed to notify status: {CorrelationId}, Status: {StatusCode}",
                    correlationId,
                    response.StatusCode);
            }
            else
            {
                _logger.LogDebug(
                    "Status notified: {CorrelationId}, Status: {Status}",
                    correlationId,
                    status);
            }
        }
        catch (Exception ex)
        {
            // Não throw - falha de notificação não deve quebrar o download
            _logger.LogError(ex,
                "Error notifying status: {CorrelationId}",
                correlationId);
        }
    }

    public async Task NotifyProgressAsync(
        string correlationId,
        int progress,
        double speed,
        long downloadedBytes,
        long? totalBytes = null,
        string? eta = null,
        CancellationToken cancellationToken = default)
    {
        var url = $"/api/downloads/{correlationId}/progress";

        var payload = new
        {
            progress,
            speed,
            downloadedBytes,
            totalBytes,
            eta
        };

        try
        {
            var content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json");

            var response = await _httpClient.PostAsync(url, content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogDebug(
                    "Failed to notify progress: {CorrelationId}, StatusCode: {StatusCode}",
                    correlationId,
                    response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            // Não throw - falha de notificação não deve quebrar o download
            _logger.LogDebug(ex,
                "Error notifying progress: {CorrelationId}",
                correlationId);
        }
    }
}
```

**Checklist:**
- [ ] Criar IDownloadStatusNotificationService interface
- [ ] Criar DownloadStatusNotificationService
- [ ] Implementar NotifyStatusAsync
- [ ] Implementar NotifyProgressAsync
- [ ] Adicionar retry policy no HttpClient
- [ ] Testar comunicação com API
- [ ] Testar erro de conexão (API down)
- [ ] Testar throttling de updates

**Critérios de aceite:**
- ✅ Status é notificado via PATCH
- ✅ Progresso é notificado via POST
- ✅ Falhas de comunicação não quebram o download
- ✅ Retries funcionam com Polly
- ✅ Logs de sucesso e falha

---

### 3.3.5 Configurar Graceful Shutdown

**Estimativa:** 3-4 horas

**Arquivos:**
```
Cutube.Worker/
  └── Worker.cs (modificar)
```

#### 3.3.5.1 Modificar Worker.cs

```csharp
// src/Cutube.Worker/Worker.cs
namespace Cutube.Worker;

public partial class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly WorkerOptions _options;

    public Worker(
        ILogger<Worker> logger,
        IServiceProvider serviceProvider,
        IOptions<WorkerOptions> options)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Cutube.Worker started at: {Time}", DateTimeOffset.Now);

        while (!stoppingToken.IsCancellationRequested)
        {
            // Worker é passivo - MassTransit consome mensagens
            // Este loop mantém o serviço vivo
            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }

        _logger.LogInformation("Cutube.Worker stopping at: {Time}", DateTimeOffset.Now);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Cutube.Worker is shutting down gracefully...");

        if (_options.EnableGracefulShutdown)
        {
            _logger.LogInformation(
                "Waiting for downloads to finish (timeout: {Timeout}s)...",
                _options.GracefulShutdownTimeoutSeconds);

            // Dar tempo para downloads em andamento terminarem
            await Task.Delay(
                TimeSpan.FromSeconds(_options.GracefulShutdownTimeoutSeconds),
                cancellationToken);
        }
        else
        {
            _logger.LogWarning("Graceful shutdown disabled. Downloads will be interrupted.");
        }

        await base.StopAsync(cancellationToken);

        _logger.LogInformation("Cutube.Worker stopped at: {Time}", DateTimeOffset.Now);
    }
}
```

**Checklist:**
- [ ] Modificar Worker.cs para graceful shutdown
- [ ] Configurar timeout de shutdown
- [ ] Testar shutdown com downloads ativos
- [ ] Testar shutdown com worker idle
- [ ] Testar CTRL+C (SIGTERM)

**Critérios de aceite:**
- ✅ Worker termina downloads em andamento antes de parar
- ✅ Timeout de shutdown é respeitado
- ✅ CTRL+C (SIGTERM) inicia graceful shutdown
- ✅ Logs indicam shutdown progress

---

## Checklist de Fase

### Implementação
- [ ] Projeto Cutube.Worker criado
- [ ] MassTransit configurado como consumer
- [ ] Serilog configurado
- [ ] WorkerOptions criado
- [ ] DownloadConsumer implementado
- [ ] DownloadProcessingService implementado
- [ ] DownloadStatusNotificationService implementado
- [ ] Graceful shutdown configurado
- [ ] HttpClient com Polly retry

### Testes
- [ ] Worker inicia e conecta ao RabbitMQ
- [ ] Consumer recebe mensagens da fila
- [ ] Download simples funciona
- [ ] Download com recorte funciona
- [ ] Download audio-only funciona
- [ ] Progresso é reportado corretamente
- [ ] Cancelamento funciona (CTRL+C)
- [ ] Erros vão para DLQ após 3 tentativas
- [ ] Graceful shutdown funciona

### Documentação
- [ ] Código documentado com XML comments
- [ ] README criado para o worker
- [ ] Configurações documentadas

### Validação
- [ ] Todos os critérios de aceite atendidos
- [ ] Build sem warnings
- [ ] Logs estruturados funcionando
- [ ] yt-dlp instalado e funcionando

---

## Qualidade Gates - Fase 3.3

**ANTES de considerar esta fase completa, TODOS os itens abaixo devem ser verdadeiros:**

- [ ] **dotnet build** - **0 warnings** (build limpo)
- [ ] **Worker inicia** sem erros e conecta ao RabbitMQ
- [ ] **yt-dlp instalado** e funcional no PATH
- [ ] **Consumer processa** mensagens da fila
- [ ] **Downloads completam** com sucesso
- [ ] **Progresso reportado** para API corretamente
- [ ] **Graceful shutdown** funciona
- [ ] **DLQ recebe** mensagens com erro após 3 tentativas
- [ ] **Logs estruturados** em arquivo e console
- [ ] **Documentação completa**

---

## Cronograma Detalhado

| Tarefa | Estimativa | Dependencies | Blocker |
|--------|-----------|--------------|---------|
| 3.3.1 Criar Worker Service | 3-4h | 3.2 completa | Não |
| 3.3.2 Consumer MassTransit | 4-5h | 3.3.1 | **Sim** |
| 3.3.3 DownloadProcessingService | 8-10h | 3.3.2 | **Sim** |
| 3.3.4 StatusNotificationService | 5-6h | 3.3.2 | **Sim** |
| 3.3.5 Graceful Shutdown | 3-4h | 3.3.3 | **Sim** |

**Total:** 23-29 horas (~5-6 dias)

---

## Tecnologias

- **.NET 10 Worker Service** - Template de serviço background
- **MassTransit 8** - Abstração para RabbitMQ consumer
- **RabbitMQ.Client** - Cliente AMQP
- **Serilog** - Logging estruturado
- **Polly** - Retry policies para HTTP
- **yt-dlp** - Download de vídeos (YouTube, etc.)

---

## Riscos e Mitigações

| Risco | Impacto | Mitigação |
|-------|---------|-----------|
| yt-dlp não está instalado | Alto | Verificar no startup, logar erro claro |
| Worker crash perdendo mensagens | Médio | Ack manual APÓS processamento completo |
| API indisponível para callbacks | Médio | Retry com Polly, não throw em falhas |
| Downloads infinitos (sem progresso) | Médio | Timeout configurável, kill de processo |
| Flood de updates de progresso | Baixo | Throttle configurável (2s) |

---

## Pré-requisitos de Sistema

### Obrigatórios
- **.NET 10 SDK** instalado
- **yt-dlp** instalado e no PATH
- **RabbitMQ** rodando (Docker ou local)
- **Cutube.Api** rodando (para callbacks)

### Opcionais
- **ffmpeg** instalado (para conversões avançadas)
- **Redis** (para status store - fase 3.4)

### Verificar yt-dlp
```bash
# Verificar instalação
yt-dlp --version

# Instalar se necessário
pip install yt-dlp
# ou
curl -L https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp -o /usr/local/bin/yt-dlp
chmod +x /usr/local/bin/yt-dlp
```

---

## Exemplo de Fluxo Completo

### 1. Producer publica mensagem (Fase 3.2)

```json
{
  "messageId": "msg-123",
  "correlationId": "corr-456",
  "url": "https://youtube.com/watch?v=dQw4w9WgXcQ",
  "startTime": "00:01:00",
  "endTime": "00:02:00",
  "outputPath": "/tmp/downloads",
  "audioOnly": false
}
```

### 2. Worker recebe mensagem

```
[10:00:00 INF] Received download message: corr-456, URL: https://youtube.com/...
[10:00:00 INF] Starting download: corr-456
[10:00:00 INF] Notifying status: processing
```

### 3. yt-dlp processa

```
[10:00:01 DBG] yt-dlp arguments: --newline --no-playlist --download-sections *00:01:00-00:02:00 -o /tmp/downloads/%(title)s.%(ext)s https://youtube.com/...
[10:00:02 DBG] Download progress: corr-456, 15%, Speed: 2.5MiB/s
[10:00:04 DBG] Download progress: corr-456, 45%, Speed: 3.1MiB/s
[10:00:06 DBG] Download progress: corr-456, 100%, Speed: 3.0MiB/s
```

### 4. Worker notifica API

```bash
PATCH /api/downloads/corr-456/status
{"state": "processing"}

POST /api/downloads/corr-456/progress
{"progress": 45, "speed": 3256789, "downloadedBytes": 5242880}

PATCH /api/downloads/corr-456/status
{"state": "completed"}
```

### 5. API atualiza frontend via SignalR

```typescript
// Frontend recebe evento em tempo real
downloadProgress(correlationId, {
  progress: 45,
  speed: 3.1,
  eta: "00:00:03"
})

downloadStatusChanged(correlationId, "completed")
```

---

## Entregáveis (Deliverables)

- [x] Projeto Cutube.Worker criado
- [ ] DownloadConsumer implementado
- [ ] DownloadProcessingService implementado com yt-dlp
- [ ] DownloadStatusNotificationService implementado
- [ ] Graceful shutdown configurado
- [ ] Testes manuais executados
- [ ] Documentação completa
- [ ] Logs funcionando

---

## Próximos Passos

Após completar Fase 3.3:

1. **Fase 3.4: Status Tracking** - Criar dashboard de monitoramento
2. Implementar IDownloadStatusRepository
3. Criar endpoints de status
4. Criar página /monitor no frontend
5. Integrar SignalR para updates em tempo real

---

**Criado em:** 11/02/2026
**Status:** 🎯 Planejamento detalhado concluído
