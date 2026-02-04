# Épico 3: Integração com RabbitMQ

**Objetivo:** Sistema de filas para processamento distribuído de downloads.

**Duração estimada:** 16-20 dias

**Dependências:** ✅ Épico 2 completado (API + Frontend funcionando)

---

## Fase 3.1: RabbitMQ Setup & Configuration

**Duração:** 2 dias

### Tarefas

#### 3.1.1 Criar Docker Compose para RabbitMQ
- [ ] Criar arquivo `docker-compose.yml` na raiz do projeto
- [ ] Configurar serviço RabbitMQ com imagem `rabbitmq:4-management`
- [ ] Expôr portas 5672 (AMQP) e 15672 (Management UI)
- [ ] Configurar variáveis de ambiente (user, password)
- [ ] Adicionar volumes para persistência de dados
- [ ] Criar script `docker-compose-up.sh` para facilitar setup
- [ ] Adicionar `docker-compose.yml` ao `.gitignore` (se tiver credentials)

**Arquivos:**
- `docker-compose.yml`
- `scripts/docker-compose-up.sh`

**Critérios de aceito:**
- ✅ `docker-compose up -d` inicia RabbitMQ
- ✅ Management UI acessível em http://localhost:15672
- ✅ Dados persistem após container restart

---

#### 3.1.2 Configurar Exchanges e Queues
- [ ] Instalar pacote `MassTransit.RabbitMQ` ou `RabbitMQ.Client`
- [ ] Criar classe `RabbitMqConfig` com configurações
- [ ] Definir exchanges: `cutube.direct`, `cutube.dlq`
- [ ] Definir queues: `cutube.downloads`, `cutube.downloads.dlq`
- [ ] Configurar bindings entre exchanges e queues
- [ ] Implementar health check para RabbitMQ connection

**Arquivos novos:**
```
Cutube.Worker/Configuration/
  ├── RabbitMqConfig.cs
  ├── ConnectionService.cs
  └── QueueDeclarationService.cs
```

**Pacotes NuGet:**
- `MassTransit.RabbitMQ` (recomendado) ou `RabbitMQ.Client`
- `Microsoft.Extensions.Diagnostics.HealthChecks`

**Critérios de aceito:**
- ✅ Exchanges e queues são criados automaticamente no startup
- ✅ Health check retorna healthy se RabbitMQ está conectado
- ✅ DLQ configurada corretamente

---

#### 3.1.3 Documentar Setup Local
- [ ] Criar README com instruções de setup
- [ ] Documentar como iniciar RabbitMQ via Docker
- [ ] Documentar como acessar Management UI
- [ ] Adicionar troubleshooting comum

**Arquivos:**
- `docs/setup-rabbitmq.md`

---

## Fase 3.2: Producer (Frontend → Fila)

**Duração:** 3-4 dias

### Tarefas

#### 3.2.1 Criar Interface e Mensagens
- [ ] Criar interface `IQueueProducer`
- [ ] Criar classe `DownloadMessage` com schema
- [ ] Implementar `RabbitMqProducer` usando MassTransit
- [ ] Configurar serialização JSON
- [ ] Implementar message ID e correlation ID
- [ ] Adicionar retry automático para publicação

**Arquivos:**
```
Cutube.Api/Queuing/
  ├── IQueueProducer.cs
  ├── RabbitMqProducer.cs
  └── Messages/
      ├── DownloadMessage.cs
      └── DownloadMessageResponse.cs
```

**Schema da mensagem:**
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

**Critérios de aceito:**
- ✅ Mensagem publicada em < 50ms
- ✅ Confirmação de entrega (ack) do RabbitMQ
- ✅ Retry automático se RabbitMQ desconectado
- ✅ Correlation ID permite tracking end-to-end

---

#### 3.2.2 Integrar Producer com API
- [ ] Modificar endpoint `POST /api/downloads` para usar producer
- [ ] Publicar mensagem na fila em vez de processar diretamente
- [ ] Retornar `correlationId` para o frontend
- [ ] Implementar fallback (se RabbitMQ down, processa localmente)
- [ ] Adicionar logs de publicação

**Modificações:**
- `Cutube.Api/Endpoints/DownloadsEndpoints.cs`

**Critérios de aceito:**
- ✅ Endpoint retorna 202 Accepted + correlationId
- ✅ Mensagem enfileirada com sucesso
- ✅ Fallback funciona se RabbitMQ indisponível

---

#### 3.2.3 Atualizar Frontend para Tracking
- [ ] Modificar `api.ts` para lidar com 202 Accepted
- [ ] Armazenar `correlationId` no estado do download
- [ ] Implementar polling de status via correlationId
- [ ] Mostrar mensagem "Enfileirado" com correlation ID

**Modificações:**
- `cutube-web/lib/api.ts`
- `cutube-web/components/DownloadCard.tsx`

**Critérios de aceito:**
- ✅ Frontend mostra status "queued"
- ✅ Usuário vê correlation ID para tracking
- ✅ Polling atualiza status periodicamente

---

## Fase 3.3: Consumer (Worker Dedicado)

**Duração:** 5-6 dias

### Tarefas

#### 3.3.1 Criar Projeto Worker Service
- [ ] Criar projeto `Cutube.Worker` (Worker Service template)
- [ ] Adicionar referência para `Cutube.Domain`
- [ ] Configurar Serilog para logging
- [ ] Configurar appsettings.json
- [ ] Implementar graceful shutdown (SIGTERM)

**Comando:**
```bash
dotnet new worker -n Cutube.Worker
```

**Arquivos:**
```
Cutube.Worker/
  ├── Program.cs
  ├── Worker.cs
  ├── appsettings.json
  └── appsettings.Development.json
```

**Critérios de aceito:**
- ✅ Worker inicia sem erros
- ✅ Graceful_shutdown funciona (Ctrl+C)
- ✅ Logs visíveis no console

---

#### 3.3.2 Implementar RabbitMqConsumer
- [ ] Criar interface `IQueueConsumer`
- [ ] Implementar `DownloadConsumer` usando MassTransit
- [ ] Configurar prefetch count (max concurrent messages)
- [ ] Implementar message acknowledgment manual
- [ ] Configurar retry policy (3 tentativas)
- [ ] Enviar mensagens com erro para DLQ após esgotar retries

**Arquivos:**
```
Cutube.Worker/Consumers/
  ├── IQueueConsumer.cs
  └── DownloadConsumer.cs
```

**Critérios de aceito:**
- ✅ Consumer recebe mensagens da fila
- ✅ Acks são enviados após processamento
- ✅ Mensagens com erro vão para DLQ após 3 tentativas

---

#### 3.3.3 Implementar DownloadProcessingService
- [ ] Criar `DownloadProcessingService`
- [ ] Injetar `IDownloadService` do Domain
- [ ] Processar mensagem `DownloadMessage`
- [ ] Executar download usando yt-dlp/ffmpeg
- [ ] Atualizar status durante progresso
- [ ] Enviar resultado para API (callback)

**Arquivos:**
```
Cutube.Worker/Services/
  ├── DownloadProcessingService.cs
  └── StatusNotificationService.cs
```

**Critérios de aceito:**
- ✅ Download é executado completamente
- ✅ Progresso é reportado para API
- ✅ Erros são capturados e logados

---

#### 3.3.4 Implementar StatusNotificationService
- [ ] Criar `StatusNotificationService`
- [ ] Enviar updates para API via REST
- [ ] Publicar eventos em SignalR (via API)
- [ ] Implementar retry para falhas de notificação
- [ ] Bufferizar updates (não flood a API)

**Endpoints de callback:**
```
PATCH /api/downloads/{id}/status
POST /api/downloads/{id}/progress
```

**Critérios de aceito:**
- ✅ API recebe updates de status
- ✅ SignalR broadcast eventos para frontend
- ✅ Não gera flood de requests

---

#### 3.3.5 Configurar Concurrency Control
- [ ] Configurar `ConcurrentMessageLimit` no MassTransit
- [ ] Limitar a N downloads simultâneos (ex: 3)
- [ ] Implementar semáforo interno se necessário
- [ ] Adicionar métricas de concurrency

**Arquivos:**
```
Cutube.Worker/Configuration/
  └── WorkerOptions.cs
```

**Critérios de aceito:**
- ✅ Máximo de N downloads ativos
- ✅ Demais downloads ficam enfileirados
- ✅ Métricas visíveis nos logs

---

## Fase 3.4: Status Tracking & Dashboard

**Duração:** 4-5 dias

### Tarefas

#### 3.4.1 Criar DownloadStatusRepository
- [ ] Criar interface `IDownloadStatusRepository`
- [ ] Implementar `InMemoryStatusRepository`
- [ ] (Opcional) Implementar `RedisStatusRepository`
- [ ] Métodos: Add, Update, Get, GetAll, GetByCorrelationId
- [ ] Limpeza automática de registros antigos (> 24h)

**Arquivos:**
```
Cutube.Api/Data/
  ├── IDownloadStatusRepository.cs
  ├── InMemoryStatusRepository.cs
  └── (opcional) RedisStatusRepository.cs
```

**Model:**
```csharp
public class DownloadStatus
{
    public string Id { get; set; }
    public string CorrelationId { get; set; }
    public DownloadState State { get; set; }
    public int Progress { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}
```

**Critérios de aceito:**
- ✅ Status persistido em memória (ou Redis)
- ✅ Consultas por correlationId funcionam
- ✅ Limpeza automática de registros antigos

---

#### 3.4.2 Criar Endpoints de Status
- [ ] `GET /api/downloads?status=queued` - Listar por status
- [ ] `GET /api/downloads/{correlationId}` - Buscar por correlationId
- [ ] `PATCH /api/downloads/{id}/status` - Atualizar status (callback)
- [ ] `POST /api/downloads/{id}/progress` - Atualizar progresso (callback)

**Arquivos:**
- `Cutube.Api/Endpoints/StatusEndpoints.cs`

**Critérios de aceito:**
- ✅ Endpoints retornam dados corretos
- ✅ Callbacks do worker funcionam

---

#### 3.4.3 Criar Dashboard no Frontend
- [ ] Criar página `/dashboard`
- [ ] Mostrar fila: downloads aguardando
- [ ] Mostrar processando: downloads ativos com progresso
- [ ] Mostrar concluídos: últimos 100 downloads
- [ ] Mostrar erros: DLQ com opção de retry
- [ ] Atualizar em tempo real via SignalR

**Arquivos:**
```
cutube-web/app/dashboard/page.tsx
cutube-web/components/
  ├── QueueStats.tsx
  ├── ActiveDownloads.tsx
  └── CompletedDownloads.tsx
```

**Critérios de aceito:**
- ✅ Dashboard atualiza em tempo real
- ✅ Responsivo (mobile, tablet, desktop)
- ✅ Métricas calculadas corretamente

---

#### 3.4.4 Implementar Métricas
- [ ] Taxa de processamento (downloads/minuto)
- [ ] Tempo médio de processamento
- [ ] Taxa de erro (%)
- [ ] Tamanho da fila (queued)
- [ ] Downloads ativos (processing)

**Arquivos:**
- `Cutube.Api/Services/MetricsService.cs`

**Critérios de aceito:**
- ✅ Métricas calculadas corretamente
- ✅ Expostas no dashboard

---

## Fase 3.5: Retry & Dead Letter Queue

**Duração:** 2-3 dias

### Tarefas

#### 3.5.1 Implementar Política de Retry
- [ ] Configurar retry policy no MassTransit
- [ ] 3 tentativas com backoff exponencial
- [ ] Intervalos: 5s, 30s, 2min
- [ ] Diferenciar erros temporários vs permanentes

**Arquivos:**
```
Cutube.Worker/Handlers/
  └── RetryPolicy.cs
```

**Critérios de aceito:**
- ✅ Erros temporários (network, timeout) são reprocessados
- ✅ Backoff exponencial respeitado

---

#### 3.5.2 Implementar Dead Letter Handler
- [ ] Criar `DeadLetterHandler`
- [ ] Logar mensagens que vão para DLQ
- [ ] Extrair motivo do erro
- [ ] Salvar em repositório para análise

**Arquivos:**
```
Cutube.Worker/Handlers/
  └── DeadLetterHandler.cs
```

**Critérios de aceito:**
- ✅ Mensagens com erro permanente vão para DLQ
- ✅ Logs detalhados para debugging

---

#### 3.5.3 Criar Interface de Reprocessamento
- [ ] Endpoint `POST /api/downloads/dlq/{id}/retry`
- [ ] Buscar mensagem da DLQ
- [ ] Republicar na fila principal
- [ ] Remover da DLQ

**Arquivos:**
- `Cutube.Api/Endpoints/DlqEndpoints.cs`

**Critérios de aceito:**
- ✅ Admin pode reprocessar mensagens da DLQ
- ✅ Mensagem reprocessada com sucesso

---

#### 3.5.4 Dashboard de DLQ
- [ ] Adicionar seção de DLQ no dashboard
- [ ] Listar mensagens na DLQ
- [ ] Mostrar motivo do erro
- [ ] Botão "Retry" para cada mensagem

**Arquivos:**
- `cutube-web/components/DeadLetterQueue.tsx`

**Critérios de aceito:**
- ✅ DLQ visível no dashboard
- ✅ Retry funciona via UI

---

## Critérios de Aceito do Épico

O Épico 3 é considerado **completo** quando:

- ✅ RabbitMQ rodando via Docker Compose
- ✅ Producer publica mensagens na fila
- ✅ Worker processa mensagens e executa downloads
- ✅ Status atualizado em tempo real no dashboard
- ✅ DLQ funciona com retry manual
- ✅ Todos os testes passam (`dotnet test`)
- ✅ Build sem warnings (`dotnet build`)
- ✅ Documentação atualizada

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

---

## Tecnologias

### Backend (.NET 10)
- `MassTransit.RabbitMQ` para abstração de filas
- `RabbitMQ.Client` (dependência do MassTransit)
- `Microsoft.Extensions.Diagnostics.HealthChecks`

### Infraestrutura
- `Docker` para RabbitMQ
- `Redis` (opcional) para status store

---

## Riscos e Mitigações

| Risco | Impacto | Mitigação |
|-------|---------|-----------|
| RabbitMQ downtime | Alto | Health checks, retry, fallback para direto |
| Worker crash perdendo mensagens | Médio | Ack manual após processamento completo |
| DLQ crescendo sem controle | Baixo | Alertas, limpeza automática, política de retenção |

---

## Próximos Passos

Após completar Épico 3:
1. Testes de carga e performance
2. Documentação de deploy em produção
3. Monitoramento e alertas (Prometheus, Grafana)

---

**Criado em:** 04/02/2026
**Status:** 🎯 Planejamento detalhado concluído
