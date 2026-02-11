# Cutube.Worker

Serviço background (Worker) para processamento assíncrono de downloads de vídeo usando yt-dlp e RabbitMQ.

## Visão Geral

O Cutube.Worker é um serviço dedicado que consome mensagens da fila RabbitMQ e processa downloads de vídeos usando o yt-dlp. Ele notifica a API Cutube sobre o progresso e status de cada download.

## Funcionalidades

- ✅ **Consumo de mensagens RabbitMQ** via MassTransit
- ✅ **Processamento de downloads** com yt-dlp
- ✅ **Suporte a recortes de tempo** (start/end time)
- ✅ **Download apenas áudio** (MP3)
- ✅ **Parsing de progresso** em tempo real
- ✅ **Notificações HTTP** para a API
- ✅ **Retry com exponential backoff** (Polly)
- ✅ **Graceful shutdown** (termina downloads em andamento)
- ✅ **Serilog** para logging estruturado

## Pré-requisitos

### Obrigatórios
- **.NET 10 SDK** instalado
- **yt-dlp** instalado e no PATH
- **RabbitMQ** rodando (Docker ou local)
- **Cutube.Api** rodando (para callbacks)

### Instalar yt-dlp

```bash
# Via pip
pip install yt-dlp

# Via curl (Linux/macOS)
curl -L https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp -o /usr/local/bin/yt-dlp
chmod +x /usr/local/bin/yt-dlp

# Verificar instalação
yt-dlp --version
```

## Configuração

O arquivo `appsettings.json` contém todas as configurações:

```json
{
  "RabbitMq": {
    "Host": "localhost",
    "Port": 5672,
    "VirtualHost": "/",
    "UserName": "cutube",
    "Password": "cutube123"
  },
  "Worker": {
    "MaxConcurrentDownloads": 3,
    "ProgressUpdateInterval": 2000,
    "ApiBaseUrl": "http://localhost:5000",
    "DownloadTimeoutMinutes": 30,
    "EnableGracefulShutdown": true,
    "GracefulShutdownTimeoutSeconds": 30
  }
}
```

### Configurações do Worker

| Configuração | Descrição | Padrão |
|--------------|------------|---------|
| `MaxConcurrentDownloads` | Número máximo de downloads simultâneos | 3 |
| `ProgressUpdateInterval` | Intervalo (ms) entre updates de progresso | 2000 |
| `ApiBaseUrl` | URL base da API para callbacks | `http://localhost:5000` |
| `DownloadTimeoutMinutes` | Timeout máximo para um download | 30 |
| `EnableGracefulShutdown` | Habilita graceful shutdown | true |
| `GracefulShutdownTimeoutSeconds` | Timeout para graceful shutdown | 30 |

## Executar

### Development

```bash
cd src/Cutube.Worker
dotnet run
```

### Release

```bash
dotnet publish -c Release -o out
./out/Cutube.Worker
```

### Docker (pendente)

```bash
# TODO: Criar Dockerfile
docker build -t cutube-worker .
docker run -d cutube-worker
```

## Arquitetura

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
│  │  • Prefetch count: 1                                           │   │
│  │  • Retry policy: 3 tentativas                                   │   │
│  └──────────────────────────┬──────────────────────────────────────┘   │
│                             │                                           │
│                             ▼                                           │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │         DownloadProcessingService (Domain Logic)                  │   │
│  │                                                                   │   │
│  │  1. Valida mensagem                                             │   │
│  │  2. Notifica API: status = "processing"                         │   │
│  │  3. Executa yt-dlp                                              │   │
│  │  4. Parse progress output                                         │   │
│  │  5. Report progress para API                                      │   │
│  └──────────────────────────┬──────────────────────────────────────┘   │
│                             │                                           │
│                             ▼                                           │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │        StatusNotificationService (API Communication)              │   │
│  │                                                                   │   │
│  │  • PATCH /api/downloads/{id}/status                              │   │
│  │  • POST /api/downloads/{id}/progress                            │   │
│  └──────────────────────────┬──────────────────────────────────────┘   │
└─────────────────────────────┼───────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                              yt-dlp / ffmpeg                            │
└─────────────────────────────────────────────────────────────────────────┘
```

## Logging

O worker usa Serilog para logging estruturado:

- **Console:** Output colorido com timestamp
- **Arquivo:** `logs/cutube-worker-{Date}.log` (rolling diário)
- **Níveis:** Information (default), Warning, Error

Níveis de log configuráveis via `appsettings.json`:

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "MassTransit": "Warning",
        "Microsoft": "Warning"
      }
    }
  }
}
```

## Mensagens (DownloadMessage)

O worker consome mensagens `DownloadMessage` da fila `cutube.downloads`:

```csharp
public record DownloadMessage
{
    public string MessageId { get; init; }
    public required string CorrelationId { get; init; }
    public required string Url { get; init; }
    public string? StartTime { get; init; }  // Para recortes: "00:01:00"
    public string? EndTime { get; init; }    // Para recortes: "00:02:00"
    public required string OutputPath { get; init; }
    public bool AudioOnly { get; init; }
    public string? OutputFilename { get; init; }
}
```

## Callbacks HTTP

O worker faz callbacks para a API:

### Status Update

```http
PATCH /api/downloads/{correlationId}/status
Content-Type: application/json

{
  "state": "processing|completed|failed|cancelled",
  "errorMessage": null
}
```

### Progress Update

```http
POST /api/downloads/{correlationId}/progress
Content-Type: application/json

{
  "progress": 45,
  "speed": 3256789,
  "downloadedBytes": 5242880,
  "totalBytes": 11777216,
  "eta": "00:00:05"
}
```

## Graceful Shutdown

Quando o worker recebe SIGTERM (Ctrl+C, docker stop, etc.):

1. Para de aceitar novas mensagens
2. Aguarda downloads em andamento terminarem (configurável)
3. Após timeout, força encerramento
4. Notifica downloads interrompidos como "cancelled"

## Troubleshooting

### yt-dlp não encontrado

```bash
# Verificar se yt-dlp está no PATH
which yt-dlp

# Se não estiver, instalar
pip install yt-dlp
```

### RabbitMQ connection refused

```bash
# Verificar se RabbitMQ está rodando
docker ps | grep rabbitmq

# Verificar logs
docker logs rabbitmq
```

### API callbacks falhando

```bash
# Verificar se API está rodando
curl http://localhost:5000/api/health

# Verificar logs do worker para erros HTTP
tail -f src/Cutube.Worker/logs/cutube-worker-*.log
```

## Licença

MIT
