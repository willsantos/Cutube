# Fase 4.2: Criar Docker Compose na Raiz

**Status:** 🎯 Planejamento
**Épico:** Cutube-858 (Épico 4: Preparação para Deploy)
**Duração:** 2-3 horas
**Responsável:** DevOps Engineer
**Prioridade:** 🔥 Alta
**Dependência:** ✅ Fase 4.1 completa (CI/CD funcionando)

---

## Objetivo

Criar orquestração dos serviços core usando Docker Compose, permitindo deploy local com um comando.

**Benefícios:**
- ✅ One-command deploy - `docker-compose up -d`
- ✅ Ambiente consistente - Mesmo stack em dev/prod
- ✅ Serviços integrados - API, Worker, Web, RabbitMQ
- ✅ Persistência de dados - Volumes para downloads

---

## Visão Arquitetural

### Arquitetura Docker Compose

```
┌─────────────────────────────────────────────────────────────────────────┐
│                      Docker Compose Stack                               │
│                                                                         │
│   ┌──────────────┐  ┌──────────────┐  ┌──────────────┐               │
│   │ cutube-web   │  │ cutube-api   │  │cutube-worker │               │
│   │ Port: 3000   │  │ Port: 5000   │  │  Replicas: 2 │               │
│   │ Next.js      │  │  ASP.NET     │  │  .NET        │               │
│   └──────┬───────┘  └──────┬───────┘  └──────┬───────┘               │
│          │                 │                 │                        │
│          └─────────────────┼─────────────────┘                        │
│                            │                                          │
│                    ┌───────┴───────┐                                  │
│                    │   RabbitMQ    │                                  │
│                    │   Port: 5672  │                                  │
│                    │   Mgmt: 15672 │                                  │
│                    └───────────────┘                                  │
│                                                                         │
│   Volumes:                                                              │
│   • rabbitmq-data  → /var/lib/rabbitmq                                 │
│   • downloads-data → /downloads (API + Worker)                         │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## Tarefas

### 4.2.1 Criar docker-compose.yml

**Estimativa:** 1.5 horas

**Arquivo:** `docker-compose.yml` (raiz do projeto)

```yaml
version: '3.8'

services:
  # ─────────────────────────────────────────────────────────────
  # RabbitMQ - Message Broker
  # ─────────────────────────────────────────────────────────────
  rabbitmq:
    image: rabbitmq:4-management-alpine
    container_name: cutube-rabbitmq
    hostname: rabbitmq
    ports:
      - "5672:5672"     # AMQP
      - "15672:15672"   # Management UI
    environment:
      RABBITMQ_DEFAULT_USER: ${RABBITMQ_USER:-cutube}
      RABBITMQ_DEFAULT_PASS: ${RABBITMQ_PASS:-cutube123}
      RABBITMQ_DEFAULT_VHOST: /
    volumes:
      - rabbitmq-data:/var/lib/rabbitmq
    healthcheck:
      test: rabbitmq-diagnostics -q ping
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 10s
    networks:
      - cutube-network
    restart: unless-stopped

  # ─────────────────────────────────────────────────────────────
  # API - ASP.NET Core Minimal API
  # ─────────────────────────────────────────────────────────────
  api:
    build:
      context: .
      dockerfile: src/Cutube.Api/Dockerfile
    container_name: cutube-api
    ports:
      - "5000:8080"
      - "5001:8081"
    environment:
      ASPNETCORE_ENVIRONMENT: ${ASPNETCORE_ENVIRONMENT:-Production}
      ASPNETCORE_URLS: http://+:8080
      ConnectionStrings__RabbitMQ: amqp://${RABBITMQ_USER:-cutube}:${RABBITMQ_PASS:-cutube123}@rabbitmq:5672/
      RabbitMQ__Host: rabbitmq
      RabbitMQ__Port: 5672
      RabbitMQ__UserName: ${RABBITMQ_USER:-cutube}
      RabbitMQ__Password: ${RABBITMQ_PASS:-cutube123}
      RabbitMQ__VirtualHost: /
      SignalR__Enabled: "true"
      Logging__LogLevel__Default: Information
      Logging__LogLevel__Microsoft.AspNetCore: Warning
    depends_on:
      rabbitmq:
        condition: service_healthy
    volumes:
      - downloads-data:/app/downloads
      - ./logs/api:/app/logs
    healthcheck:
      test: ["CMD", "wget", "-q", "--spider", "http://localhost:8080/health"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 40s
    networks:
      - cutube-network
    restart: unless-stopped

  # ─────────────────────────────────────────────────────────────
  # Worker - Background Download Processor
  # ─────────────────────────────────────────────────────────────
  worker:
    build:
      context: .
      dockerfile: src/Cutube.Worker/Dockerfile
    container_name: cutube-worker
    deploy:
      replicas: ${WORKER_REPLICAS:-2}
    environment:
      DOTNET_ENVIRONMENT: ${DOTNET_ENVIRONMENT:-Production}
      ConnectionStrings__RabbitMQ: amqp://${RABBITMQ_USER:-cutube}:${RABBITMQ_PASS:-cutube123}@rabbitmq:5672/
      RabbitMQ__Host: rabbitmq
      RabbitMQ__Port: 5672
      RabbitMQ__UserName: ${RABBITMQ_USER:-cutube}
      RabbitMQ__Password: ${RABBITMQ_PASS:-cutube123}
      RabbitMQ__VirtualHost: /
      Worker__MaxConcurrentDownloads: ${MAX_CONCURRENT:-3}
      Worker__DownloadsPath: /downloads
      CutubeApi__BaseUrl: http://api:8080
      Logging__LogLevel__Default: Information
      Logging__LogLevel__Cutube: Debug
    depends_on:
      rabbitmq:
        condition: service_healthy
      api:
        condition: service_healthy
    volumes:
      - downloads-data:/downloads
      - ./logs/worker:/app/logs
    networks:
      - cutube-network
    restart: unless-stopped

  # ─────────────────────────────────────────────────────────────
  # Web - Next.js Frontend
  # ─────────────────────────────────────────────────────────────
  web:
    build:
      context: ./cutube-web
      dockerfile: Dockerfile
    container_name: cutube-web
    ports:
      - "3000:3000"
    environment:
      NODE_ENV: ${NODE_ENV:-production}
      NEXT_PUBLIC_API_URL: ${NEXT_PUBLIC_API_URL:-http://localhost:5000}
      NEXT_PUBLIC_SIGNALR_URL: ${NEXT_PUBLIC_SIGNALR_URL:-http://localhost:5000/hub}
    depends_on:
      api:
        condition: service_healthy
    healthcheck:
      test: ["CMD", "wget", "-q", "--spider", "http://localhost:3000"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 40s
    networks:
      - cutube-network
    restart: unless-stopped

# ─────────────────────────────────────────────────────────────
# Volumes
# ─────────────────────────────────────────────────────────────
volumes:
  rabbitmq-data:
    driver: local
  downloads-data:
    driver: local

# ─────────────────────────────────────────────────────────────
# Networks
# ─────────────────────────────────────────────────────────────
networks:
  cutube-network:
    driver: bridge
```

**Checklist:**
- [ ] Criar `docker-compose.yml` na raiz
- [ ] Configurar RabbitMQ com management UI
- [ ] Configurar API com healthcheck
- [ ] Configurar Worker com 2 réplicas
- [ ] Configurar Web Next.js
- [ ] Adicionar volumes persistentes
- [ ] Adicionar network cutube-network
- [ ] Healthchecks em todos serviços

**Critérios de aceito:**
- ✅ `docker-compose up -d` sobe serviços core
- ✅ Healthchecks funcionam
- ✅ Volumes persistem dados
- ✅ Services se comunicam via network

---

### 4.2.2 Criar docker-compose.override.yml

**Estimativa:** 30 minutos

**Arquivo:** `docker-compose.override.yml`

```yaml
version: '3.8'

services:
  api:
    environment:
      ASPNETCORE_ENVIRONMENT: Development
      Logging__LogLevel__Default: Debug

  worker:
    environment:
      DOTNET_ENVIRONMENT: Development
      Logging__LogLevel__Default: Debug
    deploy:
      replicas: 1

  web:
    environment:
      NODE_ENV: development
      NEXT_PUBLIC_API_URL: http://localhost:5000
    volumes:
      - ./cutube-web:/app:cached
      - /app/node_modules
    command: npm run dev
```

**Checklist:**
- [ ] Criar `docker-compose.override.yml`
- [ ] Override environment para Development
- [ ] Worker com 1 réplica em dev
- [ ] Web em modo dev com hot reload

**Critérios de aceito:**
- ✅ Override funciona automaticamente
- ✅ Hot reload funciona

---

### 4.2.3 Criar .env.example

**Estimativa:** 15 minutos

**Arquivo:** `.env.example`

```bash
# Cutube Environment Configuration

# RabbitMQ
RABBITMQ_USER=cutube
RABBITMQ_PASS=your-secure-password-here

# API
ASPNETCORE_ENVIRONMENT=Production

# Worker
DOTNET_ENVIRONMENT=Production
WORKER_REPLICAS=2
MAX_CONCURRENT=3

# Web
NODE_ENV=production
NEXT_PUBLIC_API_URL=http://localhost:5000
NEXT_PUBLIC_SIGNALR_URL=http://localhost:5000/hub
```

**Checklist:**
- [ ] Criar `.env.example`
- [ ] Documentar variáveis essenciais

**Critérios de aceito:**
- ✅ `.env.example` completo

---

### 4.2.4 Criar Scripts de Setup

**Estimativa:** 30 minutos

**Arquivo:** `scripts/docker-up.sh`

```bash
#!/bin/bash
set -e

GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

echo -e "${GREEN}🚀 Iniciando Cutube...${NC}"

if [ ! -f .env ]; then
    echo -e "${YELLOW}⚠️  Criando .env a partir de .env.example...${NC}"
    cp .env.example .env
fi

mkdir -p logs/api logs/worker downloads

if docker compose version &> /dev/null; then
    COMPOSE_CMD="docker compose"
else
    COMPOSE_CMD="docker-compose"
fi

echo -e "${GREEN}📦 Pulling images...${NC}"
$COMPOSE_CMD pull

echo -e "${GREEN}🚀 Iniciando serviços...${NC}"
$COMPOSE_CMD up -d

echo ""
echo -e "${GREEN}✅ Cutube iniciado!${NC}"
echo ""
echo "URLs disponíveis:"
echo "  • Web:            http://localhost:3000"
echo "  • API:            http://localhost:5000"
echo "  • RabbitMQ Mgmt:  http://localhost:15672"
echo ""
```

**Arquivo:** `scripts/docker-down.sh`

```bash
#!/bin/bash
set -e

if docker compose version &> /dev/null; then
    COMPOSE_CMD="docker compose"
else
    COMPOSE_CMD="docker-compose"
fi

echo "🛑 Parando Cutube..."
$COMPOSE_CMD down
echo "✅ Pronto."
```

**Checklist:**
- [ ] Criar scripts
- [ ] Adicionar permissão +x

**Critérios de aceito:**
- ✅ Scripts funcionam

---

## Qualidade Gates

**ANTES de considerar esta fase completa:**

- [ ] **docker-compose.yml criado** - 4 serviços core
- [ ] **Healthchecks funcionando** - Todos respondem
- [ ] **Volumes persistentes** - Downloads salvos
- [ ] **Scripts funcionando** - Up/Down
- [ ] **Teste manual** - Deploy completo funciona

---

## Cronograma

| Tarefa | Estimativa | Dependencies |
|--------|-----------|--------------|
| 4.2.1 Compose Principal | 1.5h | Nenhuma |
| 4.2.2 Override Dev | 30m | 4.2.1 |
| 4.2.3 .env.example | 15m | Nenhuma |
| 4.2.4 Scripts Setup | 30m | Nenhuma |

**Total:** 2.5-3 horas

---

## Tecnologias

- **Docker Compose** - Orquestração
- **Docker Networks** - Comunicação
- **Docker Volumes** - Persistência
- **Healthchecks** - Auto-restart

---

## Entregáveis

- [ ] `docker-compose.yml`
- [ ] `docker-compose.override.yml`
- [ ] `.env.example`
- [ ] `scripts/docker-up.sh`
- [ ] `scripts/docker-down.sh`
- [ ] Deploy testado

---

**Fim do Plano - Fase 4.2**
