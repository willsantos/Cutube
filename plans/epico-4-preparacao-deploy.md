# Épico 4: Preparação para Deploy

**Status:** 🎯 Planejamento  
**Duração:** 10-14 dias  
**Responsável:** DevOps/Backend Developer  
**Prioridade:** 🔥 Alta  
**Dependência:** ✅ Fase 3.5 (Retry & DLQ) completa

---

## Objetivo

Preparar o sistema Cutube para deploy em produção, criando toda a infraestrutura necessária para rodar os serviços de forma confiável e escalável. Este épico inclui:

1. **CI/CD Pipeline** - Automação de build, testes e release via GitHub Actions
2. **Docker Compose** - Orquestração completa de todos os serviços
3. **Instalador CLI** - Script de instalação automática para usuários finais

**Benefícios:**
- ✅ **Deploy Automatizado**: Push para main dispara deploy automático
- ✅ **Ambiente Consistente**: Docker garante mesmo ambiente em dev/prod
- ✅ **Fácil Instalação**: Usuários instalam CLI com um comando
- ✅ **Escalabilidade**: Arquitetura pronta para múltiplos workers
- ✅ **Observability**: Logs centralizados e métricas

---

## Visão Arquitetural

### Stack de Deploy

```
┌─────────────────────────────────────────────────────────────────────────┐
│                           Deploy Stack                                  │
│                                                                         │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │                    GitHub Actions CI/CD                          │   │
│  │                                                                   │   │
│  │   ┌──────────┐   ┌──────────┐   ┌──────────┐   ┌──────────┐    │   │
│  │   │  Build   │──►│  Test    │──►│  Docker  │──►│ Release  │    │   │
│  │   │          │   │  dotnet  │   │  Build   │   │  GitHub  │    │   │
│  │   └──────────┘   └──────────┘   └──────────┘   └──────────┘    │   │
│  │                        │                                         │   │
│  │                        ▼                                         │   │
│  │              ┌─────────────────────┐                            │   │
│  │              │   Artifacts/Packages │                            │   │
│  │              │   • Docker Images    │                            │   │
│  │              │   • CLI Binaries     │                            │   │
│  │              └─────────────────────┘                            │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                                                                         │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │                    Docker Compose (Root)                         │   │
│  │                                                                   │   │
│  │   ┌──────────────┐  ┌──────────────┐  ┌──────────────┐          │   │
│  │   │  cutube-web  │  │  cutube-api  │  │ cutube-worker│          │   │
│  │   │   (Next.js)  │  │  (.NET API)  │  │  (.NET)      │          │   │
│  │   │   Port: 3000 │  │  Port: 5000  │  │   Workers    │          │   │
│  │   └──────┬───────┘  └──────┬───────┘  └──────┬───────┘          │   │
│  │          │                 │                 │                   │   │
│  │          └─────────────────┼─────────────────┘                   │   │
│  │                            │                                      │   │
│  │                    ┌───────┴───────┐                              │   │
│  │                    │   RabbitMQ    │                              │   │
│  │                    │   Port: 5672  │                              │   │
│  │                    │   Mgmt: 15672 │                              │   │
│  │                    └───────────────┘                              │   │
│  │                                                                   │   │
│  │   Networks: cutube-network                                        │   │
│  │   Volumes: rabbitmq-data, downloads-data                          │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                                                                         │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │                    CLI Installer                                 │   │
│  │                                                                   │   │
│  │   curl -sSL https://get.cutube.dev | bash                        │   │
│  │                                                                   │   │
│  │   ┌─────────────────────────────────────────────────────┐        │   │
│  │   │  1. Detect OS (Linux/macOS/Windows)                 │        │   │
│  │   │  2. Download latest binary from GitHub Releases     │        │   │
│  │   │  3. Install to /usr/local/bin or ~/.local/bin       │        │   │
│  │   │  4. Check dependencies (yt-dlp, ffmpeg)             │        │   │
│  │   │  5. Create config directory ~/.cutube               │        │   │
│  │   └─────────────────────────────────────────────────────┘        │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### Fluxo CI/CD

```
┌──────────────┐     ┌──────────────┐     ┌──────────────┐     ┌──────────────┐
│   Push to    │────►│   Build &    │────►│   Run Tests  │────►│   Security   │
│   feature/*  │     │   Compile    │     │   dotnet     │     │   Scan       │
└──────────────┘     └──────────────┘     └──────────────┘     └──────────────┘
                                                                        │
                                                                        ▼
┌──────────────┐     ┌──────────────┐     ┌──────────────┐     ┌──────────────┐
│   Deploy     │◄────│   Push to    │◄────│   Create     │◄────│   Build      │
│   to Prod    │     │   Registry   │     │   Release    │     │   Images     │
└──────────────┘     └──────────────┘     └──────────────┘     └──────────────┘
```

---

## Tarefas

---

### 4.1 Implementar CI/CD com GitHub Actions

**Estimativa:** 6-8 horas  
**Prioridade:** 🔥 Alta

#### 4.1.1 Criar Workflow de CI (Pull Requests)

**Arquivos:**
```
.github/workflows/
├── ci.yml                 # Build e testes em PRs
├── docker-build.yml       # Build de imagens Docker
├── release.yml            # Release de versões
└── security-scan.yml      # Análise de segurança
```

**Implementação - ci.yml:**

```yaml
# .github/workflows/ci.yml
name: CI

on:
  push:
    branches: [main, dev]
  pull_request:
    branches: [main, dev]

jobs:
  build-and-test:
    runs-on: ubuntu-latest
    
    strategy:
      matrix:
        dotnet-version: ['10.0.x']
        
    steps:
      - name: Checkout code
        uses: actions/checkout@v4
        
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: ${{ matrix.dotnet-version }}
          
      - name: Cache NuGet packages
        uses: actions/cache@v4
        with:
          path: ~/.nuget/packages
          key: ${{ runner.os }}-nuget-${{ hashFiles('**/*.csproj') }}
          restore-keys: |
            ${{ runner.os }}-nuget-
            
      - name: Restore dependencies
        run: dotnet restore
        
      - name: Build solution
        run: dotnet build --configuration Release --no-restore
        
      - name: Run tests
        run: dotnet test --no-build --verbosity normal --logger trx
        
      - name: Upload test results
        uses: actions/upload-artifact@v4
        if: always()
        with:
          name: test-results
          path: '**/TestResults/*.trx'
          
      - name: Code coverage
        run: |
          dotnet test --no-build --collect:"XPlat Code Coverage"
          
      - name: Upload coverage to Codecov
        uses: codecov/codecov-action@v4
        with:
          files: '**/coverage.cobertura.xml'
          fail_ci_if_error: false
```

**Implementação - docker-build.yml:**

```yaml
# .github/workflows/docker-build.yml
name: Docker Build

on:
  push:
    branches: [main]
    tags: ['v*']
  pull_request:
    branches: [main]

env:
  REGISTRY: ghcr.io
  IMAGE_PREFIX: cutube

jobs:
  build-api:
    runs-on: ubuntu-latest
    permissions:
      contents: read
      packages: write
      
    steps:
      - name: Checkout
        uses: actions/checkout@v4
        
      - name: Setup Docker Buildx
        uses: docker/setup-buildx-action@v3
        
      - name: Login to Container Registry
        uses: docker/login-action@v3
        with:
          registry: ${{ env.REGISTRY }}
          username: ${{ github.actor }}
          password: ${{ secrets.GITHUB_TOKEN }}
          
      - name: Extract metadata
        id: meta
        uses: docker/metadata-action@v5
        with:
          images: ${{ env.REGISTRY }}/${{ github.repository }}/api
          tags: |
            type=ref,event=branch
            type=ref,event=pr
            type=semver,pattern={{version}}
            type=semver,pattern={{major}}.{{minor}}
            type=sha
            
      - name: Build and push API
        uses: docker/build-push-action@v5
        with:
          context: .
          file: ./src/Cutube.Api/Dockerfile
          push: ${{ github.event_name != 'pull_request' }}
          tags: ${{ steps.meta.outputs.tags }}
          labels: ${{ steps.meta.outputs.labels }}
          cache-from: type=gha
          cache-to: type=gha,mode=max
          platforms: linux/amd64,linux/arm64

  build-worker:
    runs-on: ubuntu-latest
    permissions:
      contents: read
      packages: write
      
    steps:
      - name: Checkout
        uses: actions/checkout@v4
        
      - name: Setup Docker Buildx
        uses: docker/setup-buildx-action@v3
        
      - name: Login to Container Registry
        uses: docker/login-action@v3
        with:
          registry: ${{ env.REGISTRY }}
          username: ${{ github.actor }}
          password: ${{ secrets.GITHUB_TOKEN }}
          
      - name: Extract metadata
        id: meta
        uses: docker/metadata-action@v5
        with:
          images: ${{ env.REGISTRY }}/${{ github.repository }}/worker
          tags: |
            type=ref,event=branch
            type=ref,event=pr
            type=semver,pattern={{version}}
            type=semver,pattern={{major}}.{{minor}}
            type=sha
            
      - name: Build and push Worker
        uses: docker/build-push-action@v5
        with:
          context: .
          file: ./src/Cutube.Worker/Dockerfile
          push: ${{ github.event_name != 'pull_request' }}
          tags: ${{ steps.meta.outputs.tags }}
          labels: ${{ steps.meta.outputs.labels }}
          cache-from: type=gha
          cache-to: type=gha,mode=max
          platforms: linux/amd64,linux/arm64

  build-web:
    runs-on: ubuntu-latest
    permissions:
      contents: read
      packages: write
      
    steps:
      - name: Checkout
        uses: actions/checkout@v4
        
      - name: Setup Docker Buildx
        uses: docker/setup-buildx-action@v3
        
      - name: Login to Container Registry
        uses: docker/login-action@v3
        with:
          registry: ${{ env.REGISTRY }}
          username: ${{ github.actor }}
          password: ${{ secrets.GITHUB_TOKEN }}
          
      - name: Extract metadata
        id: meta
        uses: docker/metadata-action@v5
        with:
          images: ${{ env.REGISTRY }}/${{ github.repository }}/web
          tags: |
            type=ref,event=branch
            type=ref,event=pr
            type=semver,pattern={{version}}
            type=semver,pattern={{major}}.{{minor}}
            type=sha
            
      - name: Build and push Web
        uses: docker/build-push-action@v5
        with:
          context: ./cutube-web
          push: ${{ github.event_name != 'pull_request' }}
          tags: ${{ steps.meta.outputs.tags }}
          labels: ${{ steps.meta.outputs.labels }}
          cache-from: type=gha
          cache-to: type=gha,mode=max
          platforms: linux/amd64,linux/arm64
```

**Implementação - release.yml:**

```yaml
# .github/workflows/release.yml
name: Release

on:
  push:
    tags:
      - 'v*'

permissions:
  contents: write

jobs:
  release-cli:
    runs-on: ${{ matrix.os }}
    strategy:
      matrix:
        include:
          - os: ubuntu-latest
            target: linux-x64
            artifact_name: cutube
            asset_name: cutube-linux-amd64
          - os: ubuntu-latest
            target: linux-arm64
            artifact_name: cutube
            asset_name: cutube-linux-arm64
          - os: macos-latest
            target: osx-x64
            artifact_name: cutube
            asset_name: cutube-macos-amd64
          - os: macos-latest
            target: osx-arm64
            artifact_name: cutube
            asset_name: cutube-macos-arm64
          - os: windows-latest
            target: win-x64
            artifact_name: cutube.exe
            asset_name: cutube-windows-amd64.exe
            
    steps:
      - name: Checkout
        uses: actions/checkout@v4
        
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'
          
      - name: Publish CLI
        run: |
          dotnet publish src/cutube/cutube.csproj \
            -c Release \
            -r ${{ matrix.target }} \
            --self-contained true \
            -p:PublishSingleFile=true \
            -p:PublishTrimmed=true \
            -p:TrimMode=partial \
            -o ./publish
            
      - name: Package (Unix)
        if: matrix.os != 'windows-latest'
        run: |
          cd publish
          tar -czf ../${{ matrix.asset_name }}.tar.gz ${{ matrix.artifact_name }}
          
      - name: Package (Windows)
        if: matrix.os == 'windows-latest'
        run: |
          cd publish
          Compress-Archive -Path ${{ matrix.artifact_name }} -DestinationPath ../${{ matrix.asset_name }}.zip
          
      - name: Upload artifacts
        uses: actions/upload-artifact@v4
        with:
          name: ${{ matrix.asset_name }}
          path: |
            ${{ matrix.asset_name }}.*

  create-release:
    needs: release-cli
    runs-on: ubuntu-latest
    
    steps:
      - name: Download all artifacts
        uses: actions/download-artifact@v4
        
      - name: Create Release
        uses: softprops/action-gh-release@v1
        with:
          files: |
            cutube-linux-amd64/*
            cutube-linux-arm64/*
            cutube-macos-amd64/*
            cutube-macos-arm64/*
            cutube-windows-amd64.exe/*
          generate_release_notes: true
          draft: false
          prerelease: ${{ contains(github.ref, '-rc') || contains(github.ref, '-beta') }}
```

**Checklist:**
- [ ] Criar `.github/workflows/ci.yml` com build e testes
- [ ] Criar `.github/workflows/docker-build.yml` para imagens
- [ ] Criar `.github/workflows/release.yml` para releases
- [ ] Configurar cache de NuGet packages
- [ ] Configurar cache de Docker layers
- [ ] Configurar code coverage com Codecov
- [ ] Testar workflows em PR

**Critérios de aceite:**
- ✅ CI executa em todo PR
- ✅ Docker images buildam e publicam no GHCR
- ✅ Releases automáticos em tags v*
- ✅ Binários CLI gerados para Linux, macOS e Windows
- ✅ Multi-arch (amd64, arm64) suportado

---

### 4.2 Criar Docker Compose na Raiz

**Estimativa:** 4-6 horas  
**Prioridade:** 🔥 Alta

#### 4.2.1 Criar docker-compose.yml Completo

**Arquivo:** `docker-compose.yml` (raiz do projeto)

```yaml
# docker-compose.yml
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
      - ./config/rabbitmq/rabbitmq.conf:/etc/rabbitmq/rabbitmq.conf:ro
      - ./config/rabbitmq/definitions.json:/etc/rabbitmq/definitions.json:ro
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
      test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
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
      # Mount yt-dlp e ffmpeg do host (opcional)
      # - /usr/local/bin/yt-dlp:/usr/local/bin/yt-dlp:ro
      # - /usr/bin/ffmpeg:/usr/bin/ffmpeg:ro
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
  # Reverse Proxy (opcional - nginx)
  # ─────────────────────────────────────────────────────────────
  nginx:
    image: nginx:alpine
    container_name: cutube-nginx
    ports:
      - "80:80"
      - "443:443"
    volumes:
      - ./config/nginx/nginx.conf:/etc/nginx/nginx.conf:ro
      - ./config/nginx/ssl:/etc/nginx/ssl:ro
      - downloads-data:/var/www/downloads:ro
    depends_on:
      - api
      - web
    networks:
      - cutube-network
    restart: unless-stopped
    profiles:
      - production

  # ─────────────────────────────────────────────────────────────
  # Redis (opcional - para caching/metrics)
  # ─────────────────────────────────────────────────────────────
  redis:
    image: redis:7-alpine
    container_name: cutube-redis
    ports:
      - "6379:6379"
    volumes:
      - redis-data:/data
    healthcheck:
      test: ["CMD", "redis-cli", "ping"]
      interval: 10s
      timeout: 3s
      retries: 3
    networks:
      - cutube-network
    restart: unless-stopped
    profiles:
      - cache
      - full

  # ─────────────────────────────────────────────────────────────
  # Prometheus (opcional - métricas)
  # ─────────────────────────────────────────────────────────────
  prometheus:
    image: prom/prometheus:latest
    container_name: cutube-prometheus
    ports:
      - "9090:9090"
    volumes:
      - ./config/prometheus/prometheus.yml:/etc/prometheus/prometheus.yml:ro
      - prometheus-data:/prometheus
    command:
      - '--config.file=/etc/prometheus/prometheus.yml'
      - '--storage.tsdb.path=/prometheus'
      - '--web.console.libraries=/etc/prometheus/console_libraries'
      - '--web.console.templates=/etc/prometheus/consoles'
      - '--storage.tsdb.retention.time=200h'
      - '--web.enable-lifecycle'
    networks:
      - cutube-network
    restart: unless-stopped
    profiles:
      - monitoring
      - full

  # ─────────────────────────────────────────────────────────────
  # Grafana (opcional - dashboards)
  # ─────────────────────────────────────────────────────────────
  grafana:
    image: grafana/grafana:latest
    container_name: cutube-grafana
    ports:
      - "3001:3000"
    environment:
      GF_SECURITY_ADMIN_USER: ${GRAFANA_USER:-admin}
      GF_SECURITY_ADMIN_PASSWORD: ${GRAFANA_PASS:-admin}
    volumes:
      - grafana-data:/var/lib/grafana
      - ./config/grafana/dashboards:/etc/grafana/provisioning/dashboards:ro
      - ./config/grafana/datasources:/etc/grafana/provisioning/datasources:ro
    depends_on:
      - prometheus
    networks:
      - cutube-network
    restart: unless-stopped
    profiles:
      - monitoring
      - full

# ─────────────────────────────────────────────────────────────
# Volumes
# ─────────────────────────────────────────────────────────────
volumes:
  rabbitmq-data:
    driver: local
  downloads-data:
    driver: local
  redis-data:
    driver: local
  prometheus-data:
    driver: local
  grafana-data:
    driver: local

# ─────────────────────────────────────────────────────────────
# Networks
# ─────────────────────────────────────────────────────────────
networks:
  cutube-network:
    driver: bridge
```

#### 4.2.2 Criar docker-compose.override.yml (Desenvolvimento)

**Arquivo:** `docker-compose.override.yml`

```yaml
# docker-compose.override.yml - Desenvolvimento
version: '3.8'

services:
  api:
    environment:
      ASPNETCORE_ENVIRONMENT: Development
      Logging__LogLevel__Default: Debug
    volumes:
      - ./src/Cutube.Api:/app/src:ro
      - ./src/Cutube.Domain:/app/domain:ro
    
  worker:
    environment:
      DOTNET_ENVIRONMENT: Development
      Logging__LogLevel__Default: Debug
    volumes:
      - ./src/Cutube.Worker:/app/src:ro
      - ./src/Cutube.Domain:/app/domain:ro
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

#### 4.2.3 Criar .env.example

**Arquivo:** `.env.example`

```bash
# Cutube Environment Configuration
# Copy this file to .env and customize the values

# ─────────────────────────────────────────────────────────────
# RabbitMQ Configuration
# ─────────────────────────────────────────────────────────────
RABBITMQ_USER=cutube
RABBITMQ_PASS=your-secure-password-here

# ─────────────────────────────────────────────────────────────
# API Configuration
# ─────────────────────────────────────────────────────────────
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:8080

# ─────────────────────────────────────────────────────────────
# Worker Configuration
# ─────────────────────────────────────────────────────────────
DOTNET_ENVIRONMENT=Production
WORKER_REPLICAS=2
MAX_CONCURRENT=3

# ─────────────────────────────────────────────────────────────
# Web Frontend Configuration
# ─────────────────────────────────────────────────────────────
NODE_ENV=production
NEXT_PUBLIC_API_URL=http://localhost:5000
NEXT_PUBLIC_SIGNALR_URL=http://localhost:5000/hub

# ─────────────────────────────────────────────────────────────
# Monitoring (opcional)
# ─────────────────────────────────────────────────────────────
GRAFANA_USER=admin
GRAFANA_PASS=admin

# ─────────────────────────────────────────────────────────────
# Advanced Configuration
# ─────────────────────────────────────────────────────────────
COMPOSE_PROJECT_NAME=cutube
COMPOSE_PROFILES=default
```

#### 4.2.4 Criar Scripts de Setup

**Arquivo:** `scripts/docker-up.sh`

```bash
#!/bin/bash
# Script para iniciar Cutube com Docker Compose

set -e

# Cores para output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${GREEN}🚀 Iniciando Cutube...${NC}"

# Verificar se .env existe
if [ ! -f .env ]; then
    echo -e "${YELLOW}⚠️  Arquivo .env não encontrado. Criando a partir de .env.example...${NC}"
    cp .env.example .env
    echo -e "${YELLOW}⚠️  Por favor, edite o arquivo .env com suas configurações.${NC}"
fi

# Verificar Docker
if ! command -v docker &> /dev/null; then
    echo -e "${RED}❌ Docker não está instalado.${NC}"
    exit 1
fi

if ! command -v docker-compose &> /dev/null && ! docker compose version &> /dev/null; then
    echo -e "${RED}❌ Docker Compose não está instalado.${NC}"
    exit 1
fi

# Detectar docker compose command
if docker compose version &> /dev/null; then
    COMPOSE_CMD="docker compose"
else
    COMPOSE_CMD="docker-compose"
fi

# Verificar argumentos
PROFILE=""
if [ "$1" == "--dev" ]; then
    echo -e "${YELLOW}🛠️  Modo desenvolvimento ativado${NC}"
    PROFILE=""
elif [ "$1" == "--full" ]; then
    echo -e "${YELLOW}🏗️  Modo completo (com monitoring) ativado${NC}"
    export COMPOSE_PROFILES=full
elif [ "$1" == "--monitoring" ]; then
    echo -e "${YELLOW}📊 Modo monitoring ativado${NC}"
    export COMPOSE_PROFILES=monitoring
fi

# Criar diretórios necessários
mkdir -p logs/api logs/worker downloads config

echo -e "${GREEN}📦 Pulling images e construindo serviços...${NC}"
$COMPOSE_CMD pull
$COMPOSE_CMD build

echo -e "${GREEN}🚀 Iniciando serviços...${NC}"
$COMPOSE_CMD up -d

echo ""
echo -e "${GREEN}✅ Cutube iniciado com sucesso!${NC}"
echo ""
echo -e "${GREEN}📍 URLs disponíveis:${NC}"
echo "  • Web Frontend:    http://localhost:3000"
echo "  • API:             http://localhost:5000"
echo "  • RabbitMQ Mgmt:   http://localhost:15672 (user: cutube)"
echo ""

if [ "$1" == "--full" ] || [ "$1" == "--monitoring" ]; then
    echo -e "${GREEN}📊 Monitoring:${NC}"
    echo "  • Prometheus:      http://localhost:9090"
    echo "  • Grafana:         http://localhost:3001"
    echo ""
fi

echo -e "${YELLOW}💡 Comandos úteis:${NC}"
echo "  Ver logs:        $COMPOSE_CMD logs -f"
echo "  Parar:           $COMPOSE_CMD down"
echo "  Reiniciar:       $COMPOSE_CMD restart"
echo "  Status:          $COMPOSE_CMD ps"
echo ""
```

**Arquivo:** `scripts/docker-down.sh`

```bash
#!/bin/bash
# Script para parar Cutube

set -e

# Detectar docker compose command
if docker compose version &> /dev/null; then
    COMPOSE_CMD="docker compose"
else
    COMPOSE_CMD="docker-compose"
fi

echo "🛑 Parando Cutube..."
$COMPOSE_CMD down

echo "✅ Cutube parado."
```

**Checklist:**
- [ ] Criar `docker-compose.yml` na raiz
- [ ] Criar `docker-compose.override.yml` para dev
- [ ] Criar `.env.example` com todas as variáveis
- [ ] Criar `scripts/docker-up.sh` para facilitar startup
- [ ] Criar `scripts/docker-down.sh` para parar serviços
- [ ] Adicionar healthchecks em todos os serviços
- [ ] Configurar volumes para persistência
- [ ] Configurar networks isoladas
- [ ] Testar `docker-compose up` localmente

**Critérios de aceite:**
- ✅ `docker-compose up -d` sobe todos os serviços
- ✅ RabbitMQ, API, Worker e Web comunicam-se corretamente
- ✅ Downloads persistem em volume compartilhado
- ✅ Healthchecks funcionam para todos os serviços
- ✅ Logs são escritos em diretório ./logs
- ✅ Script `docker-up.sh` funciona em Linux/macOS

---

### 4.3 Criar Instalador para CLI

**Estimativa:** 4-6 horas  
**Prioridade:** 🔥 Alta

#### 4.3.1 Criar Script de Instalação

**Arquivo:** `scripts/install.sh` (instalador oficial)

```bash
#!/bin/bash
# Cutube CLI Installer
# Usage: curl -sSL https://get.cutube.dev | bash
#    or: curl -sSL https://raw.githubusercontent.com/user/cutube/main/scripts/install.sh | bash

set -e

# Configuration
REPO="user/cutube"
BINARY_NAME="cutube"
INSTALL_DIR="/usr/local/bin"
USE_SUDO=true

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

# Logging functions
log_info() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

log_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

log_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Detect OS and architecture
detect_platform() {
    local os=""
    local arch=""
    
    # Detect OS
    case "$(uname -s)" in
        Linux*)     os="linux";;
        Darwin*)    os="macos";;
        CYGWIN*|MINGW*|MSYS*) os="windows";;
        *)
            log_error "Sistema operacional não suportado: $(uname -s)"
            exit 1
            ;;
    esac
    
    # Detect architecture
    case "$(uname -m)" in
        x86_64|amd64)   arch="amd64";;
        arm64|aarch64)  arch="arm64";;
        armv7l)         arch="arm";;
        *)
            log_error "Arquitetura não suportada: $(uname -m)"
            exit 1
            ;;
    esac
    
    echo "${os}-${arch}"
}

# Get latest release version
get_latest_version() {
    local api_url="https://api.github.com/repos/${REPO}/releases/latest"
    local version
    
    if command -v curl &> /dev/null; then
        version=$(curl -s "$api_url" | grep '"tag_name":' | sed -E 's/.*"([^"]+)".*/\1/')
    elif command -v wget &> /dev/null; then
        version=$(wget -qO- "$api_url" | grep '"tag_name":' | sed -E 's/.*"([^"]+)".*/\1/')
    else
        log_error "curl ou wget são necessários para instalação"
        exit 1
    fi
    
    if [ -z "$version" ]; then
        log_error "Não foi possível determinar a versão mais recente"
        exit 1
    fi
    
    echo "$version"
}

# Download binary
download_binary() {
    local version="$1"
    local platform="$2"
    local temp_dir="$3"
    
    local download_url
    local file_name
    
    # Handle Windows differently
    if [[ "$platform" == *"windows"* ]]; then
        file_name="${BINARY_NAME}-${platform}.exe"
        download_url="https://github.com/${REPO}/releases/download/${version}/${file_name}.zip"
    else
        file_name="${BINARY_NAME}-${platform}"
        download_url="https://github.com/${REPO}/releases/download/${version}/${file_name}.tar.gz"
    fi
    
    log_info "Baixando ${BINARY_NAME} ${version} para ${platform}..."
    
    if command -v curl &> /dev/null; then
        curl -sSL "$download_url" -o "${temp_dir}/download.tmp"
    else
        wget -q "$download_url" -O "${temp_dir}/download.tmp"
    fi
    
    if [ ! -f "${temp_dir}/download.tmp" ] || [ ! -s "${temp_dir}/download.tmp" ]; then
        log_error "Falha ao baixar o binário"
        exit 1
    fi
    
    # Extract
    log_info "Extraindo binário..."
    cd "$temp_dir"
    
    if [[ "$platform" == *"windows"* ]]; then
        unzip -q "download.tmp"
        mv "${file_name}.exe" "${BINARY_NAME}.exe"
    else
        tar -xzf "download.tmp"
        mv "$file_name" "$BINARY_NAME"
    fi
    
    chmod +x "$BINARY_NAME"
    log_success "Binário extraído com sucesso"
}

# Install binary
install_binary() {
    local temp_dir="$1"
    local binary_path="${temp_dir}/${BINARY_NAME}"
    
    # Check if we need sudo
    if [ "$USE_SUDO" = true ]; then
        if [ ! -w "$INSTALL_DIR" ]; then
            log_info "Solicitando permissões de administrador..."
            if ! sudo -v; then
                log_error "Permissões de administrador são necessárias para instalar em ${INSTALL_DIR}"
                log_info "Alternativa: instale em ~/.local/bin"
                INSTALL_DIR="$HOME/.local/bin"
                USE_SUDO=false
                mkdir -p "$INSTALL_DIR"
            fi
        fi
    fi
    
    log_info "Instalando ${BINARY_NAME} em ${INSTALL_DIR}..."
    
    if [ "$USE_SUDO" = true ] && [ ! -w "$INSTALL_DIR" ]; then
        sudo mv "$binary_path" "${INSTALL_DIR}/${BINARY_NAME}"
        sudo chmod +x "${INSTALL_DIR}/${BINARY_NAME}"
    else
        mv "$binary_path" "${INSTALL_DIR}/${BINARY_NAME}"
        chmod +x "${INSTALL_DIR}/${BINARY_NAME}"
    fi
    
    log_success "${BINARY_NAME} instalado com sucesso em ${INSTALL_DIR}/${BINARY_NAME}"
}

# Check dependencies
check_dependencies() {
    log_info "Verificando dependências..."
    
    local missing_deps=()
    
    # Check yt-dlp
    if ! command -v yt-dlp &> /dev/null && ! command -v youtube-dl &> /dev/null; then
        missing_deps+=("yt-dlp")
    fi
    
    # Check ffmpeg
    if ! command -v ffmpeg &> /dev/null; then
        missing_deps+=("ffmpeg")
    fi
    
    if [ ${#missing_deps[@]} -ne 0 ]; then
        log_warning "Dependências opcionais não encontradas: ${missing_deps[*]}"
        log_info "Para funcionalidade completa, instale:"
        
        if [[ "$OSTYPE" == "linux-gnu"* ]]; then
            log_info "  Ubuntu/Debian: sudo apt-get install ffmpeg && pip install yt-dlp"
            log_info "  Fedora: sudo dnf install ffmpeg && pip install yt-dlp"
            log_info "  Arch: sudo pacman -S ffmpeg yt-dlp"
        elif [[ "$OSTYPE" == "darwin"* ]]; then
            log_info "  macOS: brew install ffmpeg yt-dlp"
        fi
    else
        log_success "Todas as dependências estão instaladas"
    fi
}

# Update PATH if necessary
update_path() {
    if [[ ":$PATH:" != *":${INSTALL_DIR}:"* ]]; then
        log_warning "${INSTALL_DIR} não está no PATH"
        log_info "Adicione a seguinte linha ao seu ~/.bashrc ou ~/.zshrc:"
        log_info "  export PATH=\"\$PATH:${INSTALL_DIR}\""
    fi
}

# Create config directory
create_config() {
    local config_dir="$HOME/.cutube"
    mkdir -p "$config_dir"
    
    if [ ! -f "$config_dir/config.json" ]; then
        cat > "$config_dir/config.json" << 'EOF'
{
  "outputDirectory": "~/Downloads",
  "defaultQuality": "1080p",
  "api": {
    "enabled": false,
    "url": "http://localhost:5000"
  }
}
EOF
        log_info "Arquivo de configuração criado em ${config_dir}/config.json"
    fi
}

# Main installation flow
main() {
    echo ""
    echo "╔═══════════════════════════════════════╗"
    echo "║      Cutube CLI Installer             ║"
    echo "║      Download YouTube videos          ║"
    echo "╚═══════════════════════════════════════╝"
    echo ""
    
    # Parse arguments
    while [[ $# -gt 0 ]]; do
        case $1 in
            --version)
                VERSION="$2"
                shift 2
                ;;
            --install-dir)
                INSTALL_DIR="$2"
                USE_SUDO=false
                shift 2
                ;;
            --no-sudo)
                USE_SUDO=false
                shift
                ;;
            -h|--help)
                echo "Uso: $0 [OPTIONS]"
                echo ""
                echo "Options:"
                echo "  --version VERSION    Instalar versão específica"
                echo "  --install-dir DIR    Instalar em diretório customizado"
                echo "  --no-sudo            Não usar sudo"
                echo "  -h, --help           Mostrar esta ajuda"
                exit 0
                ;;
            *)
                log_error "Opção desconhecida: $1"
                exit 1
                ;;
        esac
    done
    
    # Detect platform
    PLATFORM=$(detect_platform)
    log_info "Plataforma detectada: ${PLATFORM}"
    
    # Get version
    if [ -z "${VERSION:-}" ]; then
        VERSION=$(get_latest_version)
    fi
    log_info "Versão a instalar: ${VERSION}"
    
    # Create temp directory
    TEMP_DIR=$(mktemp -d)
    trap "rm -rf $TEMP_DIR" EXIT
    
    # Download and install
    download_binary "$VERSION" "$PLATFORM" "$TEMP_DIR"
    install_binary "$TEMP_DIR"
    
    # Verify installation
    if command -v "$BINARY_NAME" &> /dev/null; then
        log_success "Instalação concluída!"
        echo ""
        echo "Versão instalada:"
        "$BINARY_NAME" --version
    else
        log_warning "${BINARY_NAME} pode não estar no PATH"
    fi
    
    # Post-installation steps
    check_dependencies
    update_path
    create_config
    
    echo ""
    echo "═══════════════════════════════════════════════════"
    log_success "Cutube CLI instalado com sucesso! 🎉"
    echo ""
    echo "Para começar:"
    echo "  cutube --help"
    echo ""
    echo "Documentação: https://github.com/${REPO}/wiki"
    echo "═══════════════════════════════════════════════════"
    echo ""
}

# Run main function
main "$@"
```

#### 4.3.2 Criar Dockerfile para CLI

**Arquivo:** `src/cutube/Dockerfile.standalone`

```dockerfile
# Dockerfile para CLI standalone
FROM mcr.microsoft.com/dotnet/runtime:10.0-alpine AS runtime

# Install dependencies
RUN apk add --no-cache \
    ffmpeg \
    python3 \
    py3-pip \
    curl

# Install yt-dlp
RUN pip3 install yt-dlp

# Create app directory
WORKDIR /app

# Copy published binary
COPY ./publish/cutube /app/cutube

# Make executable
RUN chmod +x /app/cutube

# Create downloads directory
RUN mkdir -p /downloads

# Set environment
ENV CUTUBE_DOWNLOADS_PATH=/downloads

# Entrypoint
ENTRYPOINT ["/app/cutube"]
```

#### 4.3.3 Criar Script de Release Manual

**Arquivo:** `scripts/release-cli.sh`

```bash
#!/bin/bash
# Script para criar release manual de CLI

set -e

VERSION="${1:-}"
if [ -z "$VERSION" ]; then
    echo "Uso: $0 <version> (ex: v1.0.0)"
    exit 1
fi

echo "🚀 Criando release ${VERSION}..."

# Ensure clean state
if [ -n "$(git status --porcelain)" ]; then
    echo "❌ Working directory não está limpo"
    exit 1
fi

# Create tag
git tag -a "$VERSION" -m "Release ${VERSION}"
git push origin "$VERSION"

echo "✅ Tag ${VERSION} criada e enviada"
echo "🔄 GitHub Actions irá buildar e publicar automaticamente"
echo "📊 Acompanhe em: https://github.com/user/cutube/actions"
```

#### 4.3.4 Criar Documentação de Instalação

**Arquivo:** `docs/installation.md`

```markdown
# Instalação do Cutube CLI

## Instalação Rápida (Recomendada)

### Linux/macOS

```bash
curl -sSL https://get.cutube.dev | bash
```

Ou com wget:

```bash
wget -qO- https://get.cutube.dev | bash
```

### Windows (PowerShell)

```powershell
iwr -useb https://get.cutube.dev/win | iex
```

## Instalação Manual

### Download do Binário

1. Acesse a [página de releases](https://github.com/user/cutube/releases)
2. Baixe o binário para sua plataforma:
   - Linux: `cutube-linux-amd64.tar.gz`
   - macOS: `cutube-macos-amd64.tar.gz`
   - Windows: `cutube-windows-amd64.exe.zip`
3. Extraia e mova para um diretório no PATH

### Exemplo (Linux):

```bash
tar -xzf cutube-linux-amd64.tar.gz
sudo mv cutube /usr/local/bin/
chmod +x /usr/local/bin/cutube
```

## Dependências

### Obrigatórias
- **.NET 10 Runtime** (incluído no binário standalone)

### Opcionais (Recomendadas)
- **yt-dlp**: Para download de vídeos
  ```bash
  # macOS
  brew install yt-dlp
  
  # Linux (Ubuntu/Debian)
  pip3 install yt-dlp
  ```

- **ffmpeg**: Para processamento de vídeo
  ```bash
  # macOS
  brew install ffmpeg
  
  # Linux (Ubuntu/Debian)
  sudo apt-get install ffmpeg
  ```

## Verificação

Após instalação:

```bash
cutube --version
cutube --help
```

## Desinstalação

### Linux/macOS

```bash
sudo rm /usr/local/bin/cutube
rm -rf ~/.cutube
```

### Windows

Remova o executável do diretório de instalação e delete `%USERPROFILE%\.cutube`.

## Solução de Problemas

### "cutube: command not found"

O diretório de instalação pode não estar no PATH. Adicione ao seu shell:

```bash
# ~/.bashrc ou ~/.zshrc
export PATH="$PATH:/usr/local/bin"
```

### Erro de permissão

```bash
chmod +x /usr/local/bin/cutube
```

### Dependências não encontradas

Instale yt-dlp e ffmpeg conforme instruções acima.
```

**Checklist:**
- [ ] Criar `scripts/install.sh` instalador universal
- [ ] Criar `scripts/docker-down.sh` helper
- [ ] Criar `scripts/release-cli.sh` para releases manuais
- [ ] Criar `src/cutube/Dockerfile.standalone` para CLI containerizada
- [ ] Criar `docs/installation.md` documentação
- [ ] Testar instalador em Linux
- [ ] Testar instalador em macOS
- [ ] Configurar GitHub Pages para `get.cutube.dev`

**Critérios de aceite:**
- ✅ `curl -sSL https://get.cutube.dev | bash` instala CLI
- ✅ Instalador detecta corretamente OS e arquitetura
- ✅ Binário é instalado e está acessível no PATH
- ✅ Dependências são verificadas e reportadas
- ✅ Documentação clara de instalação disponível
- ✅ Releases automáticos em tags v*

---

## Dockerfile dos Serviços

### API Dockerfile

**Arquivo:** `src/Cutube.Api/Dockerfile`

```dockerfile
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and restore
COPY Cutube.sln ./
COPY src/Cutube.Api/Cutube.Api.csproj src/Cutube.Api/
COPY src/Cutube.Domain/Cutube.Domain.csproj src/Cutube.Domain/
COPY src/Cutube.Infrastructure/Cutube.Infrastructure.csproj src/Cutube.Infrastructure/
RUN dotnet restore src/Cutube.Api/Cutube.Api.csproj

# Copy source and build
COPY src/Cutube.Api/ src/Cutube.Api/
COPY src/Cutube.Domain/ src/Cutube.Domain/
COPY src/Cutube.Infrastructure/ src/Cutube.Infrastructure/
RUN dotnet publish src/Cutube.Api/Cutube.Api.csproj -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
WORKDIR /app

# Install curl for healthcheck
RUN apk add --no-cache curl

# Copy published app
COPY --from=build /app/publish .

# Create downloads directory
RUN mkdir -p /app/downloads

# Expose ports
EXPOSE 8080 8081

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Run
ENTRYPOINT ["dotnet", "Cutube.Api.dll"]
```

### Worker Dockerfile

**Arquivo:** `src/Cutube.Worker/Dockerfile`

```dockerfile
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and restore
COPY Cutube.sln ./
COPY src/Cutube.Worker/Cutube.Worker.csproj src/Cutube.Worker/
COPY src/Cutube.Domain/Cutube.Domain.csproj src/Cutube.Domain/
COPY src/Cutube.Infrastructure/Cutube.Infrastructure.csproj src/Cutube.Infrastructure/
RUN dotnet restore src/Cutube.Worker/Cutube.Worker.csproj

# Copy source and build
COPY src/Cutube.Worker/ src/Cutube.Worker/
COPY src/Cutube.Domain/ src/Cutube.Domain/
COPY src/Cutube.Infrastructure/ src/Cutube.Infrastructure/
RUN dotnet publish src/Cutube.Worker/Cutube.Worker.csproj -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/runtime:10.0-alpine AS runtime
WORKDIR /app

# Install dependencies
RUN apk add --no-cache \
    ffmpeg \
    python3 \
    py3-pip \
    curl

# Install yt-dlp
RUN pip3 install yt-dlp

# Copy published app
COPY --from=build /app/publish .

# Create downloads directory
RUN mkdir -p /downloads

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
    CMD pgrep -x "dotnet" > /dev/null || exit 1

# Run
ENTRYPOINT ["dotnet", "Cutube.Worker.dll"]
```

### Web Dockerfile

**Arquivo:** `cutube-web/Dockerfile`

```dockerfile
# Build stage
FROM node:20-alpine AS build

WORKDIR /app

# Copy package files
COPY package*.json ./

# Install dependencies
RUN npm ci

# Copy source
COPY . .

# Build
ENV NEXT_TELEMETRY_DISABLED=1
RUN npm run build

# Production stage
FROM node:20-alpine AS runtime

WORKDIR /app

# Copy necessary files
COPY --from=build /app/package*.json ./
COPY --from=build /app/.next ./.next
COPY --from=build /app/public ./public
COPY --from=build /app/node_modules ./node_modules

# Environment
ENV NODE_ENV=production
ENV NEXT_TELEMETRY_DISABLED=1
ENV PORT=3000

EXPOSE 3000

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
    CMD wget -q --spider http://localhost:3000 || exit 1

# Run
CMD ["npm", "start"]
```

---

## Checklist Geral do Épico

### CI/CD
- [ ] Workflows GitHub Actions criados
- [ ] Docker images publicando no GHCR
- [ ] Releases automáticos funcionando
- [ ] Code coverage reportando
- [ ] Security scan configurado

### Docker Compose
- [ ] docker-compose.yml na raiz
- [ ] Todos os serviços configurados
- [ ] Healthchecks implementados
- [ ] Volumes persistentes
- [ ] Networks isoladas
- [ ] Scripts de setup funcionando

### CLI Installer
- [ ] Script install.sh criado
- [ ] Testado em múltiplas plataformas
- [ ] Documentação de instalação
- [ ] Releases automáticos de binários

### Testes Finais
- [ ] `docker-compose up` sobe todos os serviços
- [ ] CLI instalável via curl
- [ ] Downloads funcionam end-to-end
- [ ] Healthchecks respondem corretamente
- [ ] Logs são escritos corretamente

### Documentação
- [ ] README atualizado com instruções de deploy
- [ ] Documentação de instalação CLI
- [ ] Documentação de Docker Compose
- [ ] Troubleshooting guide

---

## Próximos Passos

Após completar este épico:

1. **Deploy em Staging**: Testar deploy completo em ambiente de staging
2. **Monitoramento**: Configurar alertas e dashboards
3. **Backup**: Implementar backup de dados críticos
4. **Documentação**: Escrever runbooks de operações

---

## Notas

- Todas as imagens Docker usam Alpine Linux para menor tamanho
- Multi-arch suportado (amd64, arm64)
- Healthchecks garantem que o orchestrator saiba quando reiniciar containers
- Scripts de setup facilitam onboarding de novos desenvolvedores
