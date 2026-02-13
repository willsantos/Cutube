# Fase 6.6: Docker (Contêineres)

**Status:** 🎯 Planejamento  
**Épico:** Cutube-vxo (Épico 6: Restructure Monorepo)  
**Duração:** 40 minutos  
**Responsável:** Backend / Fullstack  
**Prioridade:** 🔥 Alta  
**Dependência:** ✅ Fase 6.5 (Scripts)

---

## Objetivo

Atualizar arquivos Docker (docker-compose.yml, Dockerfiles) para refletir nova estrutura de monorepo. Dockerfiles devem copiar de localizações corretas e docker-compose deve orquestrar serviços em ordem correta.

**Benefícios:**
- ✅ **Consistência**: Docker files seguem mesma estrutura que código
- ✅ **Orquestração**: docker-compose inicia infra → worker → api → web
- ✅ **CI/CD**: Pipeline Docker builda sem erros

---

## Visão Arquitetural

### Docker Compose Services

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    DOCKER COMPOSE SERVICES                          │
└─────────────────────────────────────────────────────────────────────────┘

rabbitmq (infra)
   │
   ├─→ worker (background, consome de rabbitmq)
   │     │
   │     └─→ api (REST, escreve em rabbitmq)
   │           │
   │           └─→ web (frontend, consome de api)
   │
   └─→ (CLI não roda em container - development local)
```

### Context Build

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    DOCKER BUILD CONTEXT                              │
└─────────────────────────────────────────────────────────────────────────┘

docker/Dockerfile.api
  Context: .. (root do monorepo)
  COPY src/Cutube.Api/ ./api/
  COPY src/Cutube.Core/ ./core/
  COPY src/Cutube.Application/ ./application/
  COPY src/Cutube.Domain/ ./domain/
  COPY src/Cutube.Contracts/ ./contracts/

docker/Dockerfile.worker
  Context: .. (root do monorepo)
  COPY src/Cutube.Worker/ ./worker/
  COPY src/Cutube.Core/ ./core/
  COPY src/Cutube.Application/ ./application/
  COPY src/Cutube.Contracts/ ./contracts/

docker/Dockerfile.web
  Context: .. (root do monorepo)
  COPY src/Cutube.Web/ ./web/
```

---

## Tarefas

---

### 6.6.1 Atualizar Docker Compose

**Estimativa:** 20 min

**Arquivos:**
```
docker/docker-compose.yml
```

**Implementação:**

```yaml
# docker/docker-compose.yml
version: '3.8'

services:
  rabbitmq:
    image: rabbitmq:3-management
    container_name: cutube-rabbitmq
    ports:
      - "5672:5672"
      - "15672:15672"
    environment:
      RABBITMQ_DEFAULT_USER: guest
      RABBITMQ_DEFAULT_PASS: guest
    healthcheck:
      test: ["CMD", "rabbitmq-diagnostics", "ping"]
      interval: 10s
      timeout: 5s
      retries: 5

  api:
    build:
      context: ..
      dockerfile: docker/Dockerfile.api
    container_name: cutube-api
    ports:
      - "5000:8080"
    depends_on:
      rabbitmq:
        condition: service_healthy
    environment:
      ASPNETCORE_URLS: http://+:8080
      ASPNETCORE_ENVIRONMENT: Development
      RABBITMQ_HOST: rabbitmq
    restart: unless-stopped

  worker:
    build:
      context: ..
      dockerfile: docker/Dockerfile.worker
    container_name: cutube-worker
    depends_on:
      rabbitmq:
        condition: service_healthy
    environment:
      RABBITMQ_HOST: rabbitmq
    restart: unless-stopped

  web:
    build:
      context: ..
      dockerfile: docker/Dockerfile.web
    container_name: cutube-web
    ports:
      - "4000:3000"
    depends_on:
      - api
    environment:
      NEXT_PUBLIC_API_URL: http://api:8080
    restart: unless-stopped
```

```bash
# Validar configuração
docker-compose -f docker/docker-compose.yml config

# Verificar services
docker-compose -f docker/docker-compose.yml ps
```

**Checklist:**
- [ ] Compose referencia paths corretos
- [ ] Todos os serviços incluídos (rabbitmq, api, worker, web)
- [ ] Variáveis de ambiente corretas
- [ ] Healthcheck no rabbitmq
- [ ] `docker-compose config` valida

**Critérios de aceite:**
- ✅ Compose atualizado

---

#### 6.6.2 Atualizar Dockerfiles

**Estimativa:** 20 min

**Arquivos:**
```
docker/Dockerfile.api
docker/Dockerfile.worker
docker/Dockerfile.web
```

**Implementação:**

```dockerfile
# docker/Dockerfile.api
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj files
COPY ["src/Cutube.Api/Cutube.Api.csproj", "Cutube.Api/"]
COPY ["src/Cutube.Core/Cutube.Core.csproj", "Cutube.Core/"]
COPY ["src/Cutube.Application/Cutube.Application.csproj", "Cutube.Application/"]
COPY ["src/Cutube.Domain/Cutube.Domain.csproj", "Cutube.Domain/"]
COPY ["src/Cutube.Contracts/Cutube.Contracts.csproj", "Cutube.Contracts/"]

# Restore
RUN dotnet restore "Cutube.Api/Cutube.Api.csproj"

# Copy everything
COPY . .

# Build
WORKDIR "/src/Cutube.Api"
RUN dotnet build "Cutube.Api.csproj" -c Release -o /app/build

# Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/build .
ENTRYPOINT ["dotnet", "Cutube.Api.dll"]
```

```dockerfile
# docker/Dockerfile.worker
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy csproj files
COPY ["src/Cutube.Worker/Cutube.Worker.csproj", "Cutube.Worker/"]
COPY ["src/Cutube.Core/Cutube.Core.csproj", "Cutube.Core/"]
COPY ["src/Cutube.Application/Cutube.Application.csproj", "Cutube.Application/"]
COPY ["src/Cutube.Contracts/Cutube.Contracts.csproj", "Cutube.Contracts/"]

# Restore
RUN dotnet restore "Cutube.Worker/Cutube.Worker.csproj"

# Copy everything
COPY . .

# Build
WORKDIR "/src/Cutube.Worker"
RUN dotnet build "Cutube.Worker.csproj" -c Release -o /app/build

# Runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/build .
ENTRYPOINT ["dotnet", "Cutube.Worker.dll"]
```

```dockerfile
# docker/Dockerfile.web
FROM node:20-alpine AS build
WORKDIR /app

# Copy package files
COPY ["src/Cutube.Web/package.json", "./"]
COPY ["src/Cutube.Web/pnpm-lock.yaml", "./"]

# Install
RUN npm install -g pnpm
RUN pnpm install

# Copy everything
COPY ["src/Cutube.Web/", "./"]

# Build
RUN pnpm build

# Runtime
FROM node:20-alpine AS runtime
WORKDIR /app

# Copy from build
COPY --from=build /app/.next/ ./.next/
COPY --from=build /app/node_modules/ ./node_modules/
COPY --from=build /app/package.json ./package.json
COPY --from=build /app/public/ ./public/

EXPOSE 3000

CMD ["pnpm", "start"]
```

```bash
# Testar builds
docker build -f docker/Dockerfile.api -t cutube-api .
docker build -f docker/Dockerfile.worker -t cutube-worker .
docker build -f docker/Dockerfile.web -t cutube-web .

# Verificar images
docker images | grep cutube
```

**Checklist:**
- [ ] Dockerfile API atualizado (paths src/Cutube.*)
- [ ] Dockerfile Worker atualizado (paths src/Cutube.*)
- [ ] Dockerfile Web atualizado (paths src/Cutube.Web)
- [ ] Builds de test passam
- [ ] Images criadas

**Critérios de aceite:**
- ✅ Dockerfiles atualizados

---

## Checklist de Fase

### Implementação
- [ ] docker-compose.yml atualizado
- [ ] Dockerfile.api atualizado
- [ ] Dockerfile.worker atualizado
- [ ] Dockerfile.web atualizado

### Validação
- [ ] `docker-compose config` valida
- [ ] `docker build -f docker/Dockerfile.api` passa
- [ ] `docker build -f docker/Dockerfile.worker` passa
- [ ] `docker build -f docker/Dockerfile.web` passa
- [ ] `docker-compose up -d rabbitmq` inicia rabbitmq
- [ ] `docker-compose up` inicia api, worker, web

---

## Notas

- **CLI sem Dockerfile**: CLI é development-only, não roda em container
- **Multi-stage Builds**: SDK image para build, runtime image para exec (menor image size)
- **Healthcheck**: RabbitMQ healthcheck garante que api/worker esperam rabbitmq estar pronto
- **Context**: `context: ..` permite Dockerfiles acessar `src/` e `tests/`

---

**Criado em:** 12/02/2026  
**Última atualização:** 12/02/2026  
**Tasks Beads:** Cutube-vxo.18 (T18), Cutube-vxo.17 (T19)

---

## Referências

- [Épico 6](./epico-6-restructure-monorepo.md)
- [Docker Compose](https://docs.docker.com/compose/)
- [Dockerfile Best Practices](https://docs.docker.com/develop/develop-images/dockerfile_best-practices/)
