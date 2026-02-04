# Plano Macro - Cutube: Evolução para Sistema Distribuído

## Visão Geral

Transformar o Cutube de uma CLI monolítica em um sistema distribuído com:
- ✅ Error handling resiliente (sem crashes)
- ✅ Arquitetura híbrida (CLI + Web com Next.js/React)
- ✅ Sistema de filas com RabbitMQ

---

## ÉPICO 1: Error Handler Resiliente

**Objetivo:** Sistema que nunca encerra o aplicativo por erros, registra tudo e continua operando.

### Fase 1.1: Logging Estruturado
**Duração:** 2-3 dias

#### Tarefas:
- [ ] Criar `ILoggerService` interface e `FileLoggerService` implementação
- [ ] Implementar log levels (Debug, Info, Warning, Error, Critical)
- [ ] Log rotation (tamanho máximo, retenção de 7 dias)
- [ ] Log em formato JSON estruturado
- [ ] Path: `~/.local/share/Cutube/logs/cutube-{date}.log`

**Arquivos novos:**
```
cutube/Logging/
  ├── ILoggerService.cs
  ├── FileLoggerService.cs
  ├── LogEntry.cs
  └── LogLevel.cs
```

**Critérios de aceito:**
- ✅ Logs são escritos em arquivo JSON
- ✅ Cada entrada tem: timestamp, level, message, exception, context
- ✅ Rotação automática quando arquivo > 10MB
- ✅ Testes unitários para todos os cenários

---

### Fase 1.2: Error Handler Centralizado
**Duração:** 3-4 dias

#### Tarefas:
- [ ] Criar `IErrorHandler` interface
- [ ] Implementar `ErrorHandler` com políticas de retry
- [ ] Criar `Result<T>` pattern para operações que podem falhar
- [ ] Envolver todas as operações críticas com try/catch estratégico
- [ ] Implementar fallback para cada tipo de erro

**Arquivos novos:**
```
cutube/ErrorHandling/
  ├── IErrorHandler.cs
  ├── ErrorHandler.cs
  ├── Result.cs
  ├── ErrorType.cs
  └── RetryPolicy.cs
```

**Modificações:**
- `ProgramWorkflow.cs` - usar `Result<T>` pattern
- `YtDlpHelper.cs` - envolver com error handler
- `FfmpegHelper.cs` - envolver com error handler

**Critérios de aceito:**
- ✅ Erros de rede trigger retry (3 tentativas)
- ✅ Erros de arquivo (disk full) logam e continuam
- ✅ Erros críticos (ffmepg não encontrado) oferecem solução
- ✅ Usuário vê mensagens amigáveis, não stack traces
- ✅ App nunca crasha, sempre continua ou encerra graciosamente

---

### Fase 1.3: Recovery & Resume
**Duração:** 2-3 dias

#### Tarefas:
- [ ] Criar `DownloadStateManager` para persistir progresso
- [ ] Salvar estado a cada 10% de progresso
- [ ] Implementar resume de downloads interrompidos
- [ ] Criar comando `--resume` para CLI
- [ ] Limpeza automática de estados antigos (> 7 dias)

**Arquivos novos:**
```
cutube/Recovery/
  ├── IDownloadStateManager.cs
  ├── DownloadStateManager.cs
  ├── DownloadState.cs
  └── StateCleanupService.cs
```

**Critérios de aceito:**
- ✅ Download interrompido pode ser retomado
- ✅ Estado persistido em `~/.local/share/Cutube/state/`
- ✅ Resume detecta arquivo parcial e continua de onde parou

---

## ÉPICO 2: Arquitetura Híbrida CLI + API

**Objetivo:** Separar lógica de domínio da apresentação, permitindo CLI e Web coexistirem.

### Fase 2.1: Refatoração para Domain Layer
**Duração:** 4-5 dias

#### Tarefas:
- [ ] Criar projeto `Cutube.Domain` (Class Library)
- [ ] Mover lógica de domínio do CLI para Domain
- [ ] Criar serviços sem dependência de Console
- [ ] Implementar interfaces para todas as dependências
- [ ] Mover testes para `Cutube.Domain.Tests`

**Estrutura:**
```
Cutube.Domain/
  ├── Services/
  │   ├── IDownloadService.cs
  │   ├── DownloadService.cs
  │   ├── IValidationService.cs
  │   └── ValidationService.cs
  ├── Models/
  │   ├── DownloadRequest.cs
  │   ├── DownloadProgress.cs
  │   └── DownloadResult.cs
  └── Interfaces/
      ├── IVideoMetadataProvider.cs
      └── IVideoProcessor.cs
```

**Critérios de aceito:**
- ✅ Domain tem 0 dependência de UI
- ✅ CLI vira "thin client" do Domain
- ✅ 100% dos testes continuam passando
- ✅ Build sem warnings

---

### Fase 2.2: REST API
**Duração:** 5-6 dias

#### Tarefas:
- [ ] Criar projeto `Cutube.Api` (ASP.NET Core Minimal API)
- [ ] Implementar endpoints REST
- [ ] CORS configuration para Next.js
- [ ] OpenAPI/Swagger documentation
- [ ] Health checks

**Endpoints:**
```
POST   /api/downloads          - Iniciar download
GET    /api/downloads          - Listar downloads
GET    /api/downloads/{id}     - Status do download
DELETE /api/downloads/{id}     - Cancelar download
GET    /api/videos/info?url=   - Metadados do vídeo
```

**Arquivos principais:**
```
Cutube.Api/
  ├── Program.cs
  ├── Endpoints/
  │   ├── DownloadsEndpoints.cs
  │   └── VideosEndpoints.cs
  └── Services/
      └── WebSocketProgressService.cs
```

**Critérios de aceito:**
- ✅ API responde em < 100ms para endpoints de metadados
- ✅ Download é executado de forma assíncrona (background)
- ✅ Swagger UI funcionando
- ✅ CORS configurado para localhost:3000

---

### Fase 2.3: WebSocket para Progresso
**Duração:** 3-4 dias

#### Tarefas:
- [ ] Implementar SignalR hub para progresso
- [ ] Broadcast de progresso de downloads ativos
- [ ] Connection lifecycle management
- [ ] Reconnection automática no frontend

**WebSocket events:**
```
download:started     { id, url, status }
download:progress    { id, progress, speed, eta }
download:completed   { id, path, size }
download:error       { id, error, message }
download:cancelled   { id }
```

**Arquivos:**
```
Cutube.Api/Hubs/
  ├── DownloadHub.cs
  └── IHubClient.cs
```

**Critérios de aceito:**
- ✅ Progresso atualizado em tempo real (< 500ms latência)
- ✅ Múltiplos clients conectados recebem updates
- ✅ Reconnect automático em caso de desconexão

---

### Fase 2.4: Frontend Next.js/React + TypeScript
**Duração:** 7-10 dias

#### Tarefas:
- [ ] Criar projeto Next.js 15 com TypeScript
- [ ] Setup TailwindCSS para estilização
- [ ] Página de download com form
- [ ] Lista de downloads ativos/completados
- [ ] Progress bars em tempo real
- [ ] Dark mode

**Estrutura:**
```
cutube-web/
  ├── app/
  │   ├── page.tsx                 - Dashboard
  │   ├── downloads/
  │   │   ├── page.tsx            - Lista de downloads
  │   │   └── [id]/page.tsx       - Detalhes do download
  │   └── api/                    - API routes (proxy se necessário)
  ├── components/
  │   ├── DownloadForm.tsx
  │   ├── DownloadCard.tsx
  │   ├── ProgressBar.tsx
  │   └── StatusBadge.tsx
  ├── lib/
  │   ├── api.ts                  - REST client
  │   └── websocket.ts            - SignalR client
  └── types/
      └── download.ts
```

**Features:**
- ✅ Form com validações (URL, timerange)
- ✅ Preview de metadados do vídeo antes de baixar
- ✅ Cards com download progress, speed, ETA
- ✅ Actions: pause, cancel, retry
- ✅ Download completed com link para arquivo

**Critérios de aceito:**
- ✅ Lighthouse score > 90
- ✅ Responsivo (mobile, tablet, desktop)
- ✅ Acessibilidade (WCAG AA)

---

### Fase 2.5: Integração CLI ↔ API
**Duração:** 2-3 dias

#### Tarefas:
- [ ] CLI pode funcionar em modo standalone
- [ ] CLI pode usar API remota (flag `--api-url`)
- [ ] Config file para salvar preferências

**Arquivos:**
```
cutube/Configuration/
  ├── AppConfig.cs
  └── ConfigService.cs
```

**Critérios de aceito:**
- ✅ `cutube --api-url http://localhost:5000` usa API
- ✅ `cutube` (sem flag) usa modo local atual
- ✅ Config salvo em `~/.config/cutube/config.json`

---

## ÉPICO 3: Integração com RabbitMQ

**Objetivo:** Sistema de filas para processamento distribuído de downloads.

### Fase 3.1: RabbitMQ Setup & Configuration
**Duração:** 2 dias

#### Tarefas:
- [ ] Docker Compose para RabbitMQ + Management UI
- [ ] Configuração de exchanges e queues
- [ ] Setup de dead letter queue
- [ ] Health checks para RabbitMQ

**Docker Compose:**
```yaml
services:
  rabbitmq:
    image: rabbitmq:4-management
    ports:
      - "5672:5672"   # AMQP
      - "15672:15672" # Management UI
    environment:
      RABBITMQ_DEFAULT_USER: cutube
      RABBITMQ_DEFAULT_PASS: cutube123
```

**Arquivos:**
```
docker-compose.yml
Cutube.Worker/Configuration/
  ├── RabbitMqConfig.cs
  └── ConnectionService.cs
```

**Critérios de aceito:**
- ✅ RabbitMQ rodando via Docker
- ✅ Management UI acessível em localhost:15672
- ✅ Filas criadas automaticamente no startup

---

### Fase 3.2: Producer (Frontend → Fila)
**Duração:** 3-4 dias

#### Tarefas:
- [ ] Criar `IQueueProducer` interface
- [ ] Implementar `RabbitMqProducer` no projeto API
- [ ] Serialização de mensagens (JSON)
- [ ] Message ID e correlation tracking
- [ ] Endpoint na API para enfileirar downloads

**Message schema:**
```json
{
  "messageId": "guid",
  "correlationId": "guid",
  "url": "https://youtube.com/...",
  "startTime": "00:01:30",
  "endTime": "00:02:00",
  "outputPath": "/path/to/output",
  "audioOnly": false,
  "priority": "normal",
  "createdAt": "2025-02-04T10:00:00Z"
}
```

**Arquivos:**
```
Cutube.Api/Queuing/
  ├── IQueueProducer.cs
  ├── RabbitMqProducer.cs
  └── Messages/
      └── DownloadMessage.cs
```

**Critérios de aceito:**
- ✅ Mensagem publicada em < 50ms
- ✅ Confirmação de entrega ao producer
- ✅ Retry automático se RabbitMQ desconectado

---

### Fase 3.3: Consumer (Worker Dedicado)
**Duração:** 5-6 dias

#### Tarefas:
- [ ] Criar projeto `Cutube.Worker` (Worker Service)
- [ ] Implementar `RabbitMqConsumer`
- [ ] Processamento de mensagens da fila
- [ ] Concurrency control (max N downloads simultâneos)
- [ ] Graceful shutdown (termina downloads ativos)
- [ ] Status updates via API (callback)

**Arquivos:**
```
Cutube.Worker/
  ├── Program.cs
  ├── Consumers/
  │   ├── DownloadConsumer.cs
  │   └── IQueueConsumer.cs
  ├── Services/
  │   ├── DownloadProcessingService.cs
  │   └── StatusNotificationService.cs
  └── Configuration/
      └── WorkerOptions.cs
```

**Critérios de aceito:**
- ✅ Worker processa mensagens sequencialmente ou em paralelo (configurável)
- ✅ Mensagens com erro vão para DLQ (Dead Letter Queue)
- ✅ Status do download é enviado para API (via REST/WS)
- ✅ Worker não crasha, reinicia processamento

---

### Fase 3.4: Status Tracking & Dashboard
**Duração:** 4-5 dias

#### Tarefas:
- [ ] Criar `DownloadStatusRepository` (em memória ou Redis)
- [ ] Atualizar status: queued → processing → completed/failed
- [ ] Dashboard no frontend para monitorar fila
- [ ] Métricas: taxa de processamento, tempo médio, erros

**Tela de Dashboard:**
- ✅ Fila: quantos downloads aguardando
- ✅ Processando: downloads ativos com progresso
- ✅ Concluídos: últimos 100 downloads
- ✅ Erros: DLQ com opção de retry

**Arquivos:**
```
Cutube.Api/Data/
  ├── IDownloadStatusRepository.cs
  └── InMemoryStatusRepository.cs  // ou RedisStatusRepository

cutube-web/app/dashboard/page.tsx
```

**Critérios de aceito:**
- ✅ Status atualizado em tempo real
- ✅ Dashboard responsivo
- ✅ Métricas calculadas corretamente

---

### Fase 3.5: Retry & Dead Letter Queue
**Duração:** 2-3 dias

#### Tarefas:
- [ ] Implementar política de retry (3 tentativas)
- [ ] DLQ para mensagens que falharam todas as tentativas
- [ ] Interface para reprocessar mensagens da DLQ
- [ ] Logs detalhados para debugging

**Arquivos:**
```
Cutube.Worker/Handlers/
  ├── RetryPolicy.cs
  └── DeadLetterHandler.cs
```

**Critérios de aceito:**
- ✅ Mensagens com erro temporário (network) são reprocessadas
- ✅ Mensagens com erro permanente (invalid URL) vão para DLQ
- ✅ Admin pode reprocessar mensagens da DLQ via API

---

## Arquitetura Final

```
┌─────────────────┐
│  Next.js App    │
│  (Frontend)     │
└────────┬────────┘
         │ REST + WebSocket
┌────────▼────────┐
│  ASP.NET API    │
│  (Producer)     │
└────────┬────────┘
         │ Publish
┌────────▼────────┐
│   RabbitMQ      │
│   (Queue)       │
└────────┬────────┘
         │ Consume
┌────────▼────────┐
│   Worker        │
│  (Consumer)     │
└────────┬────────┘
         │ yt-dlp + ffmpeg
┌────────▼────────┐
│  File System    │
└─────────────────┘
```

**Modos de operação:**
1. **CLI Standalone:** `cutube` (funciona como hoje)
2. **CLI → API:** `cutube --api-url http://api:5000`
3. **Web → API:** Interface web
4. **Web → Fila:** Downloads enfileirados, worker processa

---

## Cronograma Estimado

| Épico | Fases | Dias | Dependências |
|-------|-------|------|--------------|
| **1. Error Handler** | 1.1, 1.2, 1.3 | 7-10 | - |
| **2. CLI + API** | 2.1, 2.2, 2.3, 2.4, 2.5 | 21-28 | ✅ Épico 1 |
| **3. RabbitMQ** | 3.1, 3.2, 3.3, 3.4, 3.5 | 16-20 | ✅ Épico 2 |

**Total:** 44-58 dias (~1.5 - 2 meses)

---

## Tecnologias

### Backend (.NET 10)
- `Serilog` ou `NLog` para logging estruturado
- `ASP.NET Core Minimal API` para REST
- `SignalR` para WebSocket
- `MassTransit` ou `RabbitMQ.Client` para filas
- `FluentResults` para `Result<T>` pattern

### Frontend (Next.js)
- `Next.js 15` com App Router
- `TypeScript`
- `TailwindCSS`
- `shadcn/ui` para componentes
- `@microsoft/signalr` para WebSocket
- `React Query` para cache de requisições

### Infraestrutura
- `Docker` para RabbitMQ
- `Redis` (opcional) para status store
- `Nginx` (produção) para reverse proxy

---

## Próximos Passos

### Imediato (semana 1-2):
1. ✅ Iniciar **Épico 1 - Fase 1.1**: Implementar logging
2. ✅ Criar tarefas no bd para tracking

### Curto prazo (mês 1):
1. ✅ Completar **Épico 1** (Error Handler)
2. ✅ Iniciar **Épico 2** (refatoração + API)

### Médio prazo (mês 2):
1. ✅ Completar **Épico 2** (Frontend)
2. ✅ Iniciar **Épico 3** (RabbitMQ)

---

## Riscos e Mitigações

| Risco | Impacto | Mitigação |
|-------|---------|-----------|
| Complexidade do sistema aumenta | Alto | Refatorações incrementais, testes abrangentes |
| Performance do WebSocket | Médio | Load testing, otimização de mensagens |
| RabbitMQ downtime | Alto | Health checks, retry, fallback para direto |
| Manter CLI funcional | Alto | Feature flags, testes E2E para CLI |

---

## Definição de Pronto

Épico é considerado **completo** quando:
- ✅ Todos os testes passam (`dotnet test`)
- ✅ Build sem warnings (`dotnet build`)
- ✅ Documentação atualizada
- ✅ Code review aprovado
- ✅ Deploy em staging testado

---

**Criado em:** 04/02/2026
**Status:** 🎯 Planejamento aprovado, aguardando início
