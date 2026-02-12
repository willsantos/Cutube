# Epico 5: Refatoracao do Worker

**Status:** 🎯 Planejamento  
**Duracao:** 8-12 dias  
**Responsavel:** Backend Developer  
**Prioridade:** 🔥 Alta  
**Dependencia:** ✅ Epico 3 (Integracao RabbitMQ) completo

---

## Indice

- [Objetivo](#objetivo)
- [Diagnostico: Problemas Encontrados](#diagnostico-problemas-encontrados)
- [Visao Arquitetural](#visao-arquitetural)
- [Tarefas](#tarefas)
  - [Fase 5.1: Correcoes Criticas do Fluxo](#fase-51-correcoes-criticas-do-fluxo)
  - [Fase 5.2: Desacoplamento Arquitetural](#fase-52-desacoplamento-arquitetural)
  - [Fase 5.3: Robustez e Observabilidade](#fase-53-robustez-e-observabilidade)
  - [Fase 5.4: Correcoes de Frontend](#fase-54-correcoes-de-frontend)
- [Checklist Geral do Epico](#checklist-geral-do-epico)
- [Proximos Passos](#proximos-passos)
- [Notas](#notas)

---

## Objetivo

Corrigir bugs criticos e problemas arquiteturais no Worker e no fluxo de download via RabbitMQ que foram identificados durante testes end-to-end com docker-compose + Playwright. O fluxo atual esta completamente quebrado em producao: downloads sao processados pelo yt-dlp mas o frontend nunca mostra progresso, o Worker gasta ~14 segundos em retries inuteis por notificacao, e mensagens sao roteadas incorretamente para a DLQ.

Este epico inclui:

1. **Correcoes Criticas** - Fixes que desbloqueiam o fluxo basico de download
2. **Desacoplamento** - Separar Worker do projeto API, criar shared contracts
3. **Robustez** - Health checks reais, retry policies inteligentes, observabilidade
4. **Frontend fixes** - Corrigir hydration mismatch do tema

**Beneficios:**
- ✅ **Fluxo funcional**: Downloads aparecem no frontend e progresso e exibido em tempo real
- ✅ **Performance**: Eliminar ~14s de retry loops inuteis por notificacao HTTP
- ✅ **Estabilidade**: Mensagens nao sao mais roteadas incorretamente para a DLQ
- ✅ **Build otimizado**: Worker image menor (~50% menos) sem dependencias da API
- ✅ **Manutencao**: Base de codigo simplificada sem path legacy de download

---

## Diagnostico: Problemas Encontrados

Teste realizado em 12/02/2026 com docker-compose full stack (RabbitMQ + API + Worker + Web).
URL testada: `https://www.youtube.com/watch?v=jNQXAC9IVRw`

### BUG CRITICO 1: API nao registra download no repository

**Severidade:** Bloqueante  
**Arquivo:** `src/Cutube.Api/Endpoints/DownloadsEndpoints.cs:23-106`

O `POST /api/downloads` cria a `DownloadMessage`, publica no RabbitMQ e retorna 202 Accepted -- mas **nunca chama `repository.AddAsync()`**. O download existe no RabbitMQ mas nao existe no `InMemoryStatusRepository`.

**Consequencias em cascata:**
- Frontend: `GET /api/downloads` retorna `{ downloads: [], totalCount: 0 }` sempre
- Worker: Todas as callbacks `PATCH /api/downloads/{id}/status` retornam **404**
- SignalR: Nenhum evento e emitido (sem registro, sem grupo)

**Evidencia:**
```
cutube-api  | info: DownloadsEndpoints[0]
cutube-api  |       Download enqueued: e8a96b62-...
cutube-api  |       # Nenhum log de AddAsync!

curl http://localhost:5000/api/downloads
# { "downloads": [], "totalCount": 0 }
```

### BUG CRITICO 2: DLQ Consumer recebe mensagem ANTES do DownloadConsumer

**Severidade:** Bloqueante  
**Arquivo:** `src/Cutube.Worker/Program.cs:52-87` e `src/Cutube.Worker/Consumers/DlqConsumer.cs`

O MassTransit esta configurado com dois consumers do mesmo tipo `IConsumer<DownloadMessage>` em filas diferentes. Quando a mensagem e publicada via `IPublishEndpoint.Publish<DownloadMessage>()`, o MassTransit faz **fan-out** para TODOS os endpoints que consomem `DownloadMessage` -- enviando para AMBAS as filas.

**Evidencia:**
```
[11:27:20] Processing message from DLQ: e8a96b62...    <-- DLQ primeiro!
[11:27:20] Processing download: e8a96b62... Attempt: 0 <-- depois consumer normal
[11:27:20] ERR: Message moved to DLQ, Error: Unknown fault
```

### BUG CRITICO 3: Loop de retries HTTP 404

**Severidade:** Alta  
**Arquivo:** `src/Cutube.Worker/Program.cs:96-108`

A Polly retry policy faz retry em qualquer status nao-2xx (`!msg.IsSuccessStatusCode`), incluindo 404. Um 404 nunca se resolve com retry. Cada notificacao gasta ~14 segundos (4 tentativas: 2s + 4s + 8s).

**Evidencia:**
```
PATCH http://api:8080/api/downloads/{id}/status -> 404
Retry 1 after 2s due to:
Retry 2 after 4s due to:
Retry 3 after 8s due to:
End processing HTTP request after 14112ms - 404
```

### BUG CRITICO 4: yt-dlp sem runtime JavaScript

**Severidade:** Alta  
**Arquivo:** `src/Cutube.Worker/Dockerfile`

O Dockerfile instala `python3` e `yt-dlp` mas nao instala `deno` (nem `nodejs`). O yt-dlp moderno requer um JS runtime para extrair formatos do YouTube.

**Evidencia:**
```
WARNING: [youtube] No supported JavaScript runtime could be found.
Only deno is enabled by default...
YouTube extraction without a JS runtime has been deprecated
```

### PROBLEMA ARQUITETURAL 5: Worker referencia projeto inteiro da API

**Severidade:** Media  
**Arquivo:** `src/Cutube.Worker/Cutube.Worker.csproj`

`<ProjectReference Include="..\Cutube.Api\Cutube.Api.csproj" />` puxa TODO o codigo da API para dentro do Worker. Necessidade real: apenas o tipo `DownloadMessage`.

### PROBLEMA 6: Duas arquiteturas de download coexistindo

**Severidade:** Media  

A API tem Path A (RabbitMQ) e Path B (legado in-process com `BackgroundDownloadWorker`). Apenas Path A e usado, mas Path B ocupa recursos.

### PROBLEMA 7: Hydration mismatch no frontend

**Severidade:** Baixa  
**Arquivo:** `cutube-web/components/theme/theme-toggle.tsx`

O `ThemeToggle` renderiza "Moon" no servidor mas "Sun" no cliente (next-themes).

---

## Visao Arquitetural

### Fluxo Atual (Quebrado)

```
┌──────────┐  POST /api/downloads  ┌──────────┐  Publish   ┌──────────┐
│  Web UI  │ ────────────────────► │   API    │ ─────────► │ RabbitMQ │
└──────────┘                       └──────────┘            └────┬─────┘
     │                                  │                       │
     │  GET /api/downloads              │ (nao registra!)       │ Fan-out
     │  → { downloads: [] }             │                  ┌────┴─────┐
     │                                  │             ┌────▼────┐ ┌──▼──┐
     │                                  │             │ Consumer│ │ DLQ │
     │                                  │             │  (main) │ │Cons.│
     │                                  │             └────┬────┘ └──┬──┘
     │                                  │                  │         │
     │                                  │◄── PATCH /{id}/status ────┘
     │                                  │        → 404!!             
     │                                  │        → Retry 14s         
```

### Fluxo Corrigido (Alvo)

```
┌──────────┐  POST /api/downloads  ┌──────────┐  1. AddAsync()  ┌────────────┐
│  Web UI  │ ────────────────────► │   API    │ ──────────────► │ Repository │
└──────────┘                       └──────────┘                 └────────────┘
     │                                  │
     │  GET /api/downloads              │  2. Publish (SendEndpoint)
     │  → { downloads: [{ status:       │
     │      "queued", ... }] }     ┌────▼────┐
     │                             │ RabbitMQ│  (apenas cutube.downloads)
     │                             └────┬────┘
     │                                  │
     │                             ┌────▼────────┐
     │                             │  Consumer   │
     │                             │  (Worker)   │
     │                             └────┬────────┘
     │                                  │
     │                                  │  PATCH /{id}/status → 204 OK
     │                                  │  POST /{id}/progress → 204 OK
     │  ◄── SignalR push ──────────────┘
```

---

## Tarefas

---

### Fase 5.1: Correcoes Criticas do Fluxo

**Duracao:** 3-4 dias  
**Prioridade:** 🔥 Alta

Estas correcoes desbloqueiam o fluxo basico. Sem elas, nada funciona.

#### 5.1.1 API: Registrar download no repository antes de publicar no RabbitMQ

**Arquivos:**
```
src/Cutube.Api/Endpoints/
  └── DownloadsEndpoints.cs (modificar)
```

**Descricao:**
No `POST /api/downloads`, apos criar a `DownloadMessage` e antes de publicar no RabbitMQ, criar um `DownloadStatusRecord` com status `Queued` e chamar `repository.AddAsync()`. Injetar `IDownloadStatusRepository` no endpoint.

**Checklist:**
- [ ] Injetar `IDownloadStatusRepository` no endpoint POST
- [ ] Criar `DownloadStatusRecord` com correlationId, URL, outputPath, status=Queued
- [ ] Chamar `repository.AddAsync()` ANTES de `queueProducer.PublishDownloadAsync()`
- [ ] Se publish falhar, atualizar status para Failed
- [ ] Verificar: `GET /api/downloads` retorna o download recem-criado
- [ ] Verificar: Worker callbacks recebem 204 (nao 404)

**Criterios de aceite:**
- ✅ `GET /api/downloads` retorna downloads recem-enfileirados com status "queued"
- ✅ Worker `PATCH /api/downloads/{id}/status` retorna 204 No Content
- ✅ Frontend mostra o download na lista imediatamente apos submit

---

#### 5.1.2 Worker: Corrigir Polly retry policy para nao retriar 404/4xx

**Arquivos:**
```
src/Cutube.Worker/
  └── Program.cs (modificar GetRetryPolicy)
```

**Descricao:**
Mudar a policy de `OrResult(msg => !msg.IsSuccessStatusCode)` para retriar apenas erros de servidor (5xx) e timeouts. Status 404, 400, 401, 403 sao erros permanentes que nao devem ser retriados.

**Checklist:**
- [ ] Modificar `GetRetryPolicy()` para filtrar apenas 5xx e timeouts
- [ ] Status 404/4xx nao devem acionar retry
- [ ] Manter retry para 502, 503, 504, 408 (Request Timeout)
- [ ] Adicionar log com status code no `onRetry`
- [ ] Testar: notificacao para endpoint inexistente nao bloqueia por 14s

**Criterios de aceite:**
- ✅ Notificacao para endpoint 404 completa em < 1 segundo (sem retry)
- ✅ Erros 5xx continuam sendo retriados com backoff
- ✅ Logs mostram status code correto no onRetry

---

#### 5.1.3 Worker: Corrigir roteamento de mensagens MassTransit (DLQ fan-out)

**Arquivos:**
```
src/Cutube.Worker/
  ├── Program.cs (modificar MassTransit config)
  └── Consumers/
      └── DlqConsumer.cs (modificar ou remover)
```

**Descricao:**
O `DlqConsumer` registrado como `IConsumer<DownloadMessage>` causa fan-out do MassTransit. Opcoes:

- **Opcao A (recomendada):** Remover `DlqConsumer` como consumer MassTransit. Usar a error queue nativa do MassTransit (`cutube.downloads_error`) para faults. Mover a logica de DLQ para um endpoint de admin ou poller periodico.
- **Opcao B:** Criar tipo wrapper `DlqDownloadMessage` para evitar fan-out.
- **Opcao C:** Desabilitar auto-topology na DLQ endpoint e fazer bind manual.

**Checklist:**
- [ ] Escolher e implementar uma das opcoes acima
- [ ] Verificar: mensagem publicada chega APENAS ao DownloadConsumer
- [ ] Verificar: DLQ nao recebe mensagens novas diretamente
- [ ] Verificar: mensagens com erro permanente sao capturadas na error queue
- [ ] Manter funcionalidade de retry manual via admin endpoint

**Criterios de aceite:**
- ✅ Mensagem publicada e consumida exatamente 1 vez pelo DownloadConsumer
- ✅ DLQ nao recebe copias de mensagens novas
- ✅ Mensagens que falham todas as tentativas ficam na error queue
- ✅ Admin endpoint `/api/downloads/dlq` continua funcional

---

#### 5.1.4 Dockerfile: Instalar runtime JavaScript para yt-dlp

**Arquivos:**
```
src/Cutube.Worker/
  └── Dockerfile (modificar)
src/Cutube.Api/
  └── Dockerfile (modificar)
```

**Descricao:**
Instalar `deno` nos containers que usam yt-dlp. O yt-dlp moderno requer um JS runtime para extrair formatos do YouTube.

**Checklist:**
- [ ] Adicionar instalacao do deno no Dockerfile do Worker
- [ ] Adicionar instalacao do deno no Dockerfile da API (tambem usa yt-dlp)
- [ ] Verificar que `yt-dlp --version` e `deno --version` funcionam no container
- [ ] Testar download sem WARNING de JS runtime
- [ ] Manter imagem o menor possivel (usar binary install, nao npm)

**Criterios de aceite:**
- ✅ `yt-dlp` extrai todos os formatos do YouTube sem warnings de JS runtime
- ✅ Download funciona com qualidade maxima disponivel
- ✅ Container nao cresce mais que ~30MB com o deno adicionado

---

### Fase 5.2: Desacoplamento Arquitetural

**Duracao:** 2-3 dias  
**Prioridade:** ⚡ Media  
**Dependencia:** ⏳ Fase 5.1 completa

#### 5.2.1 Criar projeto Cutube.Contracts (shared messages)

**Arquivos:**
```
src/Cutube.Contracts/
  ├── Cutube.Contracts.csproj
  └── Messages/
      ├── DownloadMessage.cs (mover de Cutube.Api)
      └── DownloadMetadata.cs (mover de Cutube.Api)
```

**Descricao:**
Criar um projeto class library minimo que contenha apenas os tipos compartilhados entre API e Worker (mensagens RabbitMQ). Worker e API referenciam `Cutube.Contracts` em vez do Worker referenciar `Cutube.Api`.

**Checklist:**
- [ ] Criar `Cutube.Contracts.csproj` (class library, net10.0)
- [ ] Mover `DownloadMessage` e `DownloadMetadata` para Contracts
- [ ] Atualizar `Cutube.Api.csproj` para referenciar Contracts
- [ ] Atualizar `Cutube.Worker.csproj` para referenciar Contracts (remover ref a Api)
- [ ] Remover `ErrorOnDuplicatePublishOutputFiles=false` do Worker
- [ ] Mudar Dockerfile do Worker de `aspnet:10.0-alpine` para `runtime:10.0-alpine`
- [ ] Atualizar namespaces e usings em todo o codigo
- [ ] Verificar: `dotnet build` sem erros/warnings
- [ ] Verificar: `dotnet test` passa

**Criterios de aceite:**
- ✅ Worker nao referencia `Cutube.Api.csproj`
- ✅ Dockerfile do Worker usa `runtime:10.0-alpine` (nao aspnet)
- ✅ Build sem warnings e sem `ErrorOnDuplicatePublishOutputFiles`
- ✅ Imagem Docker do Worker ~50% menor

---

#### 5.2.2 Remover caminho legacy de download (Path B)

**Arquivos:**
```
src/Cutube.Api/
  ├── Services/BackgroundDownloadWorker.cs (remover)
  ├── Services/FluentDownloadService.cs (remover se nao usado)
  ├── Queuing/IDownloadQueue.cs (avaliar)
  └── Program.cs (remover hosted service registration)
```

**Descricao:**
O `BackgroundDownloadWorker` e o `IDownloadQueue` channel-based representam o Path B legado. Apenas o Path A (RabbitMQ) e usado. Remover para simplificar.

**Checklist:**
- [ ] Identificar todos os arquivos do Path B legado
- [ ] Verificar que nenhum endpoint usa Path B
- [ ] Remover registracoes de DI
- [ ] Remover classes nao referenciadas
- [ ] Verificar: `dotnet build` passa sem warnings
- [ ] Verificar: `dotnet test` passa (adaptar testes se necessario)

**Criterios de aceite:**
- ✅ Nenhum `BackgroundDownloadWorker` registrado como hosted service
- ✅ Base de codigo tem menos ~200-300 linhas
- ✅ Nenhuma funcionalidade quebrada

---

#### 5.2.3 Unificar RabbitMqOptions duplicados

**Arquivos:**
```
src/Cutube.Api/Configuration/RabbitMqOptions.cs
src/Cutube.Worker/Configuration/RabbitMqOptions.cs
src/Cutube.Contracts/Configuration/RabbitMqOptions.cs (novo, unificado)
```

**Descricao:**
Mover `RabbitMqOptions` para `Cutube.Contracts` e remover as copias em Api e Worker.

**Checklist:**
- [ ] Mover para Contracts
- [ ] Atualizar usings em Api e Worker
- [ ] Remover arquivos duplicados
- [ ] Build + test passa

**Criterios de aceite:**
- ✅ Uma unica classe `RabbitMqOptions` no projeto Contracts

---

### Fase 5.3: Robustez e Observabilidade

**Duracao:** 2-3 dias  
**Prioridade:** ⚡ Media  
**Dependencia:** ⏳ Fase 5.2 completa

#### 5.3.1 Worker health check real

**Arquivos:**
```
src/Cutube.Worker/
  ├── Dockerfile (modificar HEALTHCHECK)
  └── Program.cs (adicionar health endpoint ou usar MassTransit health)
```

**Descricao:**
Substituir `pgrep -x "dotnet"` por health check que verifica conexao RabbitMQ via MassTransit.

**Checklist:**
- [ ] Expor endpoint HTTP `/health` no Worker (ou usar `IHealthCheck` do MassTransit)
- [ ] Verificar conexao RabbitMQ no health check
- [ ] Atualizar HEALTHCHECK no Dockerfile
- [ ] Testar: container fica unhealthy quando RabbitMQ cai

**Criterios de aceite:**
- ✅ Health check reflete estado real da conexao RabbitMQ
- ✅ Container reinicia via docker-compose quando unhealthy

---

#### 5.3.2 Notification service resiliente

**Arquivos:**
```
src/Cutube.Worker/Services/
  └── DownloadStatusNotificationService.cs (modificar)
```

**Descricao:**
Separar erros transientes (5xx) de permanentes (404) nas notificacoes HTTP. Nao retriar erros permanentes. Adicionar circuit breaker apos falhas consecutivas.

**Checklist:**
- [ ] Separar tratamento de 4xx (log warning, nao retry) vs 5xx (retry)
- [ ] Adicionar circuit breaker (Polly) apos 5 falhas consecutivas
- [ ] Log level adequado: 404 = Warning (nao Error)
- [ ] Metricas: contar notificacoes sucesso/falha

**Criterios de aceite:**
- ✅ Notificacao 404 completa em < 100ms (sem retry)
- ✅ Circuit breaker abre apos 5 falhas e fecha apos 30s
- ✅ Logs distinguem erros transientes de permanentes

---

#### 5.3.3 Melhorar logs estruturados do Worker

**Arquivos:**
```
src/Cutube.Worker/
  ├── Consumers/DownloadConsumer.cs
  └── Services/DownloadProcessingService.cs
```

**Descricao:**
Adicionar correlation IDs consistentes em todos os logs. Usar scopes do Serilog para rastreabilidade end-to-end.

**Checklist:**
- [ ] Adicionar `LogContext.PushProperty("CorrelationId", ...)` em todos os consumers
- [ ] Incluir duracao de cada etapa (notify, download, progress)
- [ ] Log de metricas: tempo total, bytes baixados, formato
- [ ] Formato JSON para facilitar parsing (Serilog JSON formatter)

**Criterios de aceite:**
- ✅ Todo log do Worker inclui CorrelationId
- ✅ Possivel reconstruir timeline completa de um download via logs
- ✅ Logs em formato JSON para ferramentas de agregacao

---

### Fase 5.4: Correcoes de Frontend

**Duracao:** 1 dia  
**Prioridade:** 💤 Baixa  
**Dependencia:** Nenhuma (pode ser feita em paralelo)

#### 5.4.1 Corrigir hydration mismatch do ThemeToggle

**Arquivos:**
```
cutube-web/components/theme/
  └── theme-toggle.tsx (modificar)
```

**Descricao:**
O ThemeToggle renderiza icones diferentes no servidor (Moon) e cliente (Sun) porque `next-themes` so resolve o tema no client-side. Usar estado `mounted` para renderizar o icone apenas apos hydration.

**Checklist:**
- [ ] Adicionar `useEffect` + state `mounted` no ThemeToggle
- [ ] Renderizar placeholder/skeleton ate montar
- [ ] Verificar: sem erros de hydration no console
- [ ] Manter acessibilidade (aria-label correto)

**Criterios de aceite:**
- ✅ Zero erros de hydration no console do browser
- ✅ Toggle de tema funciona sem flash visual

---

## Checklist Geral do Epico

### Correcoes Criticas
- [ ] API registra download no repository antes de publicar
- [ ] Worker Polly policy nao retria 404
- [ ] DLQ consumer nao recebe mensagens novas
- [ ] yt-dlp tem runtime JavaScript disponivel

### Desacoplamento
- [ ] Projeto Cutube.Contracts criado
- [ ] Worker nao referencia Cutube.Api
- [ ] Path B legacy removido
- [ ] RabbitMqOptions unificado

### Robustez
- [ ] Worker health check verifica RabbitMQ
- [ ] Notification service com circuit breaker
- [ ] Logs estruturados com correlation IDs

### Frontend
- [ ] Hydration mismatch corrigido

### Testes Finais
- [ ] `dotnet test` 100% passando
- [ ] `dotnet build` sem warnings
- [ ] Docker compose up: todos os servicos healthy
- [ ] Playwright: fluxo completo de download funciona
- [ ] Playwright: download aparece na lista e mostra progresso
- [ ] Playwright: download completa e status = "completed"

### Documentacao
- [ ] README do Worker atualizado
- [ ] Dockerfiles documentados
- [ ] Guia de troubleshooting atualizado

---

## Proximos Passos

Apos completar este epico:

1. **Testes de carga**: Submeter multiplos downloads simultaneos e verificar concorrencia
2. **Persistencia**: Substituir InMemoryStatusRepository por SQLite/Redis
3. **Monitoring**: Adicionar Prometheus metrics + Grafana dashboard
4. **Cancellation**: Implementar cancelamento real de downloads em progresso via RabbitMQ

---

## Notas

- O diagnostico foi feito em 12/02/2026 com docker-compose full stack + Playwright
- Video de teste: `https://www.youtube.com/watch?v=jNQXAC9IVRw` ("Me at the zoo", 19s)
- O download via yt-dlp funciona tecnicamente, mas toda a camada de estado/notificacao esta quebrada
- A Fase 5.1 e bloqueante: sem ela, as demais fases nao podem ser validadas
- O Worker faz HTTP callbacks para a API -- considerar migrar para eventos via RabbitMQ no futuro

---

**Criado em:** 12/02/2026  
**Status:** 🎯 Planejamento
