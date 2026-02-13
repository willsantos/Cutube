# Épico 6: Restructure Monorepo

**Status:** 🎯 Planejamento  
**Duração:** 5-7 dias  
**Responsável:** Backend / Fullstack  
**Prioridade:** 🔥 Alta  
**Dependência:** ⏳ Nenhuma

---

## Índice

- [Objetivo](#objetivo)
- [Visão Arquitetural](#visão-arquitetural)
- [Fases e Tarefas](#fases-e-tarefas)
  - [Fase 1: Foundation (Projetos Base)](#fase-1-foundation-projetos-base)
  - [Fase 2: Project Moves (Mover Projetos)](#fase-2-project-moves-mover-projetos)
  - [Fase 3: Solution & References (Atualizar Solução)](#fase-3-solution--references-atualizar-solução)
  - [Fase 4: Package Management (Turborepo)](#fase-4-package-management-turborepo)
  - [Fase 5: Scripts (Scripts Unificados)](#fase-5-scripts-scripts-unificados)
  - [Fase 6: Docker (Contêineres)](#fase-6-docker-contêineres)
  - [Fase 7: Testing & Validation (Testes e Validação)](#fase-7-testing--validation-testes-e-validação)
- [Checklist Geral do Épico](#checklist-geral-do-épico)
- [Próximos Passos](#próximos-passos)
- [Notas](#notas)

---

## Objetivo

Reorganizar o codebase do Cutube para seguir padrões da comunidade .NET e melhorar a experiência de desenvolvimento (DX) com um monorepo bem gerenciado.

Este épico inclui:

1. **Reestruturação de Diretórios** - Mover todos os projetos para `src/` e `tests/`
2. **Padronização de Nomes** - Converter para PascalCase (Cutube.Cli, Cutube.Web)
3. **Turborepo** - Implementar orquestração de build, dev e test
4. **Scripts Unificados** - Criar comandos simples (pnpm dev, pnpm build, pnpm test)

**Benefícios:**
- ✅ **Consistência**: Todos os projetos seguem padrão .NET
- ✅ **Clareza**: Separação clara entre código fonte e testes
- ✅ **Escalabilidade**: Fácil adicionar Cutube.Mobile, Cutube.Desktop, etc.
- ✅ **Velocidade**: Builds incrementais com cache (<10s)
- ✅ **Simplicidade**: `pnpm dev` inicia tudo automaticamente

---

## Visão Arquitetural

### Estado Atual vs. Estado Alvo

```
┌─────────────────────────────────────────────────────────────────────────┐
│                       ESTRUTURA ATUAL (Problemas)                      │
└─────────────────────────────────────────────────────────────────────────┘

Cutube/
├── src/                          # Apenas .NET backend
│   ├── Cutube.Api/
│   ├── Cutube.Worker/
│   ├── Cutube.Domain/
│   └── Cutube.Contracts/
│
├── cutube/                       # ❌ Fora de src/ (inconsistente)
├── cutube-web/                   # ❌ Fora de src/ (inconsistente)
│
├── tests/                        # Apenas .NET tests
│   ├── Cutube.Api.Tests/
│   ├── Cutube.Worker.Tests/
│   └── Cutube.Domain.Tests/
│
└── Cutube.Tests/                # ❌ Fora de tests/ (inconsistente)

Problemas:
1. Alguns projetos em src/, outros na raiz
2. Nomes inconsistentes: cutube vs Cutube.Api vs cutube-web
3. Testes espalhados: tests/ + raiz
4. Sem comandos unificados para dev/build/test
```

```
┌─────────────────────────────────────────────────────────────────────────┐
│                       ESTRUTURA ALVO (Monorepo)                       │
└─────────────────────────────────────────────────────────────────────────┘

Cutube/
├── package.json                  # 🆕 Root package.json (scripts unificados)
├── pnpm-workspace.yaml          # 🆕 Workspace config
├── turbo.json                   # 🆕 Turborepo config
│
├── src/                          # ✅ TODOS os projetos
│   ├── Cutube.Api/
│   ├── Cutube.Worker/
│   ├── Cutube.Domain/
│   ├── Cutube.Contracts/
│   ├── Cutube.Core/             # 🆕 (empty)
│   ├── Cutube.Infrastructure/    # 🆕 (empty)
│   ├── Cutube.Application/      # 🆕 (empty)
│   ├── Cutube.Cli/              # ✅ MOVIDO de cutube/
│   └── Cutube.Web/              # ✅ MOVIDO de cutube-web/
│
├── tests/                        # ✅ TODOS os testes
│   ├── Cutube.Api.Tests/
│   ├── Cutube.Worker.Tests/
│   ├── Cutube.Domain.Tests/
│   ├── Cutube.Cli.Tests/        # ✅ MOVIDO de Cutube.Tests/
│   └── Cutube.Web.E2E/         # ✅ MOVIDO de cutube-web/e2e/
│
├── scripts/                      # 🆕 Scripts unificados
│   ├── dev.sh
│   ├── build.sh
│   └── test.sh
│
└── docker/                       # Atualizado para nova estrutura
    ├── docker-compose.yml
    ├── Dockerfile.api
    ├── Dockerfile.worker
    └── Dockerfile.web

Benefícios:
1. ✅ Tudo em src/ (padrão .NET)
2. ✅ Nomes PascalCase (Cutube.Cli, Cutube.Web)
3. ✅ Testes em tests/ (padrão xUnit)
4. ✅ pnpm dev/build/test (unificado)
```

### Fluxo de Desenvolvimento

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    FLUXO DESENVOLVIMENTO (Antes vs. Depois)           │
└─────────────────────────────────────────────────────────────────────────┘

ANTES (manual e propenso a erros):
┌──────────┐    ┌──────────┐    ┌──────────┐    ┌──────────┐
│ Terminal 1│    │ Terminal 2│    │ Terminal 3│    │ Terminal 4│
│ $ cd api  │    │ $ cd web  │    │ $ cd wk   │    │ $ docker  │
│ $ dotnet  │    │ $ pnpm dev│    │ $ dotnet  │    │ $ compose │
│   watch   │    │          │    │   watch   │    │   up -d   │
└──────────┘    └──────────┘    └──────────┘    └──────────┘
     │                │                │                │
     └────────────────┴────────────────┴────────────────┘
                      Ordem manual
                      Sem cache
                      Lento

DEPOIS (automático e rápido):
┌─────────────────────────────────────────────────────────────────┐
│                    ÚNICO TERMINAL                              │
│                                                                 │
│ $ pnpm dev                                                     │
│   → docker-compose up -d rabbitmq (infra)                     │
│   → dotnet watch Cutube.Api (API)                             │
│   → dotnet watch Cutube.Worker (Worker)                        │
│   → dotnet watch Cutube.Cli (CLI)                             │
│   → next dev --turbo (Web)                                    │
│                                                                 │
│   Turborepo orquestra:                                        │
│   - Detecte mudanças                                          │
│   - Cache outputs                                              │
│   - Paralleliza builds                                        │
│   - Restarta apenas afetados                                  │
└─────────────────────────────────────────────────────────────────┘
     │
     ▼
  Builds <10s (incremental)
```

---

## Fases e Tarefas

### Fase 1: Foundation (Projetos Base)

**Duração:** 1 hora  
**Tasks:** T1-T3

#### 1.1 Criar Projetos Foundation (T1)

**Estimativa:** 30 min

**Arquivos:**
```
src/
  ├── Cutube.Core/
  │   └── Cutube.Core.csproj
  ├── Cutube.Infrastructure/
  │   └── Cutube.Infrastructure.csproj
  └── Cutube.Application/
      └── Cutube.Application.csproj
```

**Implementação:**

```bash
dotnet new classlib -n Cutube.Core -o src/Cutube.Core
dotnet new classlib -n Cutube.Infrastructure -o src/Cutube.Infrastructure
dotnet new classlib -n Cutube.Application -o src/Cutube.Application

dotnet sln cutube.sln add src/Cutube.Core/Cutube.Core.csproj
dotnet sln cutube.sln add src/Cutube.Infrastructure/Cutube.Infrastructure.csproj
dotnet sln cutube.sln add src/Cutube.Application/Cutube.Application.csproj
```

**Checklist:**
- [ ] `src/Cutube.Core/Cutube.Core.csproj` criado
- [ ] `src/Cutube.Infrastructure/Cutube.Infrastructure.csproj` criado
- [ ] `src/Cutube.Application/Cutube.Application.csproj` criado
- [ ] Todos os projetos adicionados à solução
- [ ] `dotnet build` passa sem erros

**Critérios de aceite:**
- ✅ 3 projetos criados em `src/`
- ✅ Todos targeting .NET 10.0
- ✅ ImplicitUsings habilitado
- ✅ Nullable habilitado

---

#### 1.2 Criar Branch de Feature (T2)

**Estimativa:** 5 min

**Implementação:**

```bash
git checkout -b feature/restructure-monorepo
git status
```

**Checklist:**
- [ ] Branch `feature/restructure-monorepo` criada
- [ ] Branch baseada em `main`
- [ ] Diretório de trabalho limpo

**Critérios de aceite:**
- ✅ Branch criada e limpa

---

#### 1.3 Commitar Especificação (T3)

**Estimativa:** 10 min

**Implementação:**

```bash
git add .specs/features/restructure/
git commit -m "docs: add restructure specification and tasks"
git log --oneline -1
```

**Checklist:**
- [ ] `spec.md`, `design.md`, `tasks.md` existem
- [ ] Arquivos commitados
- [ ] Mensagem segue conventional commits

**Critérios de aceite:**
- ✅ Especificação versionada

---

### Fase 2: Project Moves (Mover Projetos)

**Duração:** 40 min  
**Tasks:** T4-T7

#### 2.1 Mover CLI (T4)

**Estimativa:** 10 min

**Implementação:**

```bash
# Preserva histórico com git mv
git mv cutube src/Cutube.Cli
git status
ls -la src/Cutube.Cli/
```

**Checklist:**
- [ ] `cutube/` → `src/Cutube.Cli/`
- [ ] Histórico preservado (git mv)
- [ ] Nenhum arquivo deixado para trás

**Critérios de aceite:**
- ✅ Projeto movido com histórico intacto

---

#### 2.2 Mover Web (T5)

**Estimativa:** 10 min

**Implementação:**

```bash
git mv cutube-web src/Cutube.Web
git status
ls -la src/Cutube.Web/
```

**Checklist:**
- [ ] `cutube-web/` → `src/Cutube.Web/`
- [ ] Histórico preservado

**Critérios de aceite:**
- ✅ Projeto movido

---

#### 2.3 Mover Testes CLI (T6)

**Estimativa:** 10 min

**Implementação:**

```bash
git mv Cutube.Tests tests/Cutube.Cli.Tests
git status
ls -la tests/Cutube.Cli.Tests/
```

**Checklist:**
- [ ] `Cutube.Tests/` → `tests/Cutube.Cli.Tests/`
- [ ] Histórico preservado

**Critérios de aceite:**
- ✅ Testes movidos

---

#### 2.4 Mover E2E Web (T7)

**Estimativa:** 10 min

**Implementação:**

```bash
git mv src/Cutube.Web/e2e tests/Cutube.Web.E2E
git status
ls -la tests/Cutube.Web.E2E/
```

**Checklist:**
- [ ] `src/Cutube.Web/e2e/` → `tests/Cutube.Web.E2E/`
- [ ] Histórico preservado

**Critérios de aceite:**
- ✅ E2E movido

---

### Fase 3: Solution & References (Atualizar Solução)

**Duração:** 55 min  
**Tasks:** T8-T10

#### 3.1 Atualizar Arquivo de Solução (T8)

**Estimativa:** 20 min

**Implementação:**

```bash
# Remover referências antigas
dotnet sln cutube.sln remove cutube/cutube.csproj
dotnet sln cutube.sln remove cutube-web/cutube-web.csproj
dotnet sln cutube.sln remove Cutube.Tests/Cutube.Tests.csproj

# Adicionar referências novas
dotnet sln cutube.sln add src/Cutube.Cli/Cutube.Cli.csproj
dotnet sln cutube.sln add src/Cutube.Web/Cutube.Web.csproj
dotnet sln cutube.sln add tests/Cutube.Cli.Tests/Cutube.Cli.Tests.csproj
dotnet sln cutube.sln add tests/Cutube.Web.E2E/Cutube.Web.E2E.csproj

# Verificar build
dotnet build cutube.sln
```

**Checklist:**
- [ ] Referências antigas removidas
- [ ] Referências novas adicionadas
- [ ] Solução builda sem erros

**Critérios de aceite:**
- ✅ Solução atualizada
- ✅ Build passa

---

#### 3.2 Remover Referência API→CLI (T9)

**Estimativa:** 15 min

**Implementação:**

```xml
<!-- src/Cutube.Api/Cutube.Api.csproj -->
<!-- ANTES -->
<ItemGroup>
  <ProjectReference Include="..\..\cutube\cutube.csproj" />
</ItemGroup>

<!-- DEPOIS -->
<ItemGroup>
  <!-- Removido -->
</ItemGroup>
```

**Checklist:**
- [ ] Referência a `cutube` removida
- [ ] Build sem erros

**Critérios de aceite:**
- ✅ API não referencia mais CLI

---

#### 3.3 Adicionar Referências Core (T10)

**Estimativa:** 20 min

**Implementação:**

```xml
<!-- src/Cutube.Api/Cutube.Api.csproj -->
<ItemGroup>
  <ProjectReference Include="..\Cutube.Core\Cutube.Core.csproj" />
  <ProjectReference Include="..\Cutube.Application\Cutube.Application.csproj" />
</ItemGroup>

<!-- src/Cutube.Worker/Cutube.Worker.csproj -->
<ItemGroup>
  <ProjectReference Include="..\Cutube.Core\Cutube.Core.csproj" />
</ItemGroup>

<!-- src/Cutube.Cli/Cutube.Cli.csproj -->
<ItemGroup>
  <ProjectReference Include="..\Cutube.Core\Cutube.Core.csproj" />
</ItemGroup>
```

**Checklist:**
- [ ] API referencia Core
- [ ] Worker referencia Core
- [ ] CLI referencia Core
- [ ] Todos buildam

**Critérios de aceite:**
- ✅ Camada Core referenciada

---

### Fase 4: Package Management (Turborepo)

**Duração:** 1h10min  
**Tasks:** T11-T14

#### 4.1 Criar Root package.json (T11)

**Estimativa:** 15 min

**Implementação:**

```json
// package.json (root)
{
  "name": "cutube-monorepo",
  "version": "0.1.0",
  "private": true,
  "scripts": {
    "dev": "turbo run dev",
    "dev:api": "turbo run dev --filter=Cutube.Api",
    "dev:worker": "turbo run dev --filter=Cutube.Worker",
    "dev:web": "turbo run dev --filter=Cutube.Web",
    "dev:cli": "turbo run dev --filter=Cutube.Cli",
    "build": "turbo run build",
    "test": "turbo run test",
    "lint": "turbo run lint",
    "clean": "turbo run clean"
  },
  "devDependencies": {
    "turbo": "^2.0.0"
  },
  "packageManager": "pnpm@9.0.0"
}
```

**Checklist:**
- [ ] `package.json` criado
- [ ] Scripts dev, build, test presentes
- [ ] Turborepo em devDependencies

**Critérios de aceite:**
- ✅ Root package configurado

---

#### 4.2 Mover Workspace Config (T12)

**Estimativa:** 10 min

**Implementação:**

```yaml
# pnpm-workspace.yaml (root)
packages:
  - 'src/*'
  - 'tests/*'
```

```bash
# Mover de src/Cutube.Web/ para root
git mv src/Cutube.Web/pnpm-workspace.yaml .
```

**Checklist:**
- [ ] `pnpm-workspace.yaml` na raiz
- [ ] Aponta para `src/*` e `tests/*`
- [ ] Removido de `src/Cutube.Web/`

**Critérios de aceite:**
- ✅ Workspace configurado

---

#### 4.3 Criar Turbo Config (T13)

**Estimativa:** 15 min

**Implementação:**

```json
// turbo.json (root)
{
  "pipeline": {
    "dev": {
      "dependsOn": ["^build"],
      "cache": false,
      "persistent": true
    },
    "build": {
      "dependsOn": ["^build"],
      "outputs": ["bin/**", "obj/**", ".next/**"]
    },
    "test": {
      "dependsOn": ["build"],
      "outputs": ["TestResults/**"]
    }
  }
}
```

**Checklist:**
- [ ] `turbo.json` criado
- [ ] Pipelines dev, build, test configurados
- [ ] Outputs definidos corretamente

**Critérios de aceite:**
- ✅ Turborepo configurado

---

#### 4.4 Adicionar package.json aos Projetos (T14)

**Estimativa:** 30 min

**Implementação:**

```json
// src/Cutube.Api/package.json
{
  "name": "Cutube.Api",
  "version": "0.1.0",
  "private": true,
  "scripts": {
    "dev": "dotnet watch --project Cutube.Api.csproj",
    "build": "dotnet build Cutube.Api.csproj",
    "test": "dotnet test ../../tests/Cutube.Api.Tests/Cutube.Api.Tests.csproj"
  }
}

// src/Cutube.Worker/package.json
{
  "name": "Cutube.Worker",
  "version": "0.1.0",
  "private": true,
  "scripts": {
    "dev": "dotnet watch --project Cutube.Worker.csproj",
    "build": "dotnet build Cutube.Worker.csproj",
    "test": "dotnet test ../../tests/Cutube.Worker.Tests/Cutube.Worker.Tests.csproj"
  }
}

// src/Cutube.Cli/package.json
{
  "name": "Cutube.Cli",
  "version": "0.1.0",
  "private": true,
  "scripts": {
    "dev": "dotnet watch --project Cutube.Cli.csproj",
    "build": "dotnet build Cutube.Cli.csproj",
    "test": "dotnet test ../../tests/Cutube.Cli.Tests/Cutube.Cli.Tests.csproj"
  }
}

// src/Cutube.Web/package.json
{
  "name": "Cutube.Web",
  "version": "0.1.0",
  "private": true,
  "scripts": {
    "dev": "next dev --turbo",
    "build": "next build",
    "test": "playwright test"
  }
}
```

**Checklist:**
- [ ] `src/Cutube.Api/package.json` existe
- [ ] `src/Cutube.Worker/package.json` existe
- [ ] `src/Cutube.Cli/package.json` existe
- [ ] `src/Cutube.Web/package.json` existe
- [ ] `pnpm install` executa sem erros

**Critérios de aceite:**
- ✅ Todos os projetos têm package.json

---

### Fase 5: Scripts (Scripts Unificados)

**Duração:** 1h  
**Tasks:** T15-T17

#### 5.1 Criar Scripts de Desenvolvimento (T15)

**Estimativa:** 30 min

**Implementação:**

```bash
#!/bin/bash
# scripts/dev.sh
set -e

echo "🚀 Starting Cutube development environment..."

# Start infrastructure
echo "📦 Starting infrastructure (RabbitMQ)..."
docker-compose -f docker/docker-compose.yml up -d rabbitmq

# Start all services via Turborepo
echo "🔧 Starting all services..."
pnpm dev
```

```bash
#!/bin/bash
# scripts/build.sh
set -e

echo "🔨 Building all projects..."

pnpm build

echo "✅ Build complete!"
```

```bash
#!/bin/bash
# scripts/test.sh
set -e

echo "🧪 Running all tests..."

pnpm test

echo "✅ All tests passed!"
```

```bash
chmod +x scripts/*.sh
```

**Checklist:**
- [ ] `scripts/dev.sh` existe e é executável
- [ ] `scripts/build.sh` existe e é executável
- [ ] `scripts/test.sh` existe e é executável
- [ ] Scripts usam comandos pnpm

**Critérios de aceite:**
- ✅ Scripts funcionais

---

#### 5.2 Atualizar .gitignore (T16)

**Estimativa:** 10 min

**Implementação:**

```gitignore
# .gitignore (adicionar)

# Specs (working documents)
.specs/

# Turborepo
.turbo

# pnpm
.pnpm-store

```

**Checklist:**
- [ ] `.specs/` ignorado
- [ ] Padrões antigos removidos (se houver)
- [ ] Novos padrões para monorepo adicionados

**Critérios de aceite:**
- ✅ .gitignore atualizado

---

#### 5.3 Commitar Mudanças (T17)

**Estimativa:** 20 min

**Implementação:**

```bash
git add .
git commit -m "refactor: reorganize monorepo structure

- Move projects to src/ and tests/
- Add Turborepo configuration
- Create unified development scripts
- Update solution file"

git status
```

**Checklist:**
- [ ] Arquivos movidos commitados
- [ ] Novos arquivos commitados
- [ ] Mensagem segue conventional commits
- [ ] Árvore de trabalho limpa

**Critérios de aceite:**
- ✅ Reestruturação commitada

---

### Fase 6: Docker (Contêineres)

**Duração:** 40 min  
**Tasks:** T18-T19

#### 6.1 Atualizar Docker Compose (T18)

**Estimativa:** 20 min

**Implementação:**

```yaml
# docker/docker-compose.yml
version: '3.8'

services:
  rabbitmq:
    image: rabbitmq:3-management
    ports:
      - "5672:5672"
      - "15672:15672"
    environment:
      RABBITMQ_DEFAULT_USER: guest
      RABBITMQ_DEFAULT_PASS: guest

  api:
    build:
      context: ..
      dockerfile: docker/Dockerfile.api
    ports:
      - "5000:8080"
    depends_on:
      - rabbitmq
    environment:
      ASPNETCORE_URLS: http://+:8080

  worker:
    build:
      context: ..
      dockerfile: docker/Dockerfile.worker
    depends_on:
      - rabbitmq
    environment:
      RABBITMQ_HOST: rabbitmq

  web:
    build:
      context: ..
      dockerfile: docker/Dockerfile.web
    ports:
      - "4000:3000"
```

**Checklist:**
- [ ] Compose referencia paths corretos
- [ ] Todos os serviços incluídos
- [ ] Variáveis de ambiente corretas
- [ ] `docker-compose config` valida

**Critérios de aceite:**
- ✅ Compose atualizado

---

#### 6.2 Atualizar Dockerfiles (T19)

**Estimativa:** 20 min

**Implementação:**

```dockerfile
# docker/Dockerfile.api
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/Cutube.Api/Cutube.Api.csproj", "Cutube.Api/"]
COPY ["src/Cutube.Core/Cutube.Core.csproj", "Cutube.Core/"]
COPY ["src/Cutube.Application/Cutube.Application.csproj", "Cutube.Application/"]
COPY ["src/Cutube.Domain/Cutube.Domain.csproj", "Cutube.Domain/"]
COPY ["src/Cutube.Contracts/Cutube.Contracts.csproj", "Cutube.Contracts/"]
RUN dotnet restore "Cutube.Api/Cutube.Api.csproj"
COPY . .
WORKDIR "/src/Cutube.Api"
RUN dotnet build "Cutube.Api.csproj" -c Release -o /app/build

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/build .
ENTRYPOINT ["dotnet", "Cutube.Api.dll"]
```

```dockerfile
# docker/Dockerfile.worker
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/Cutube.Worker/Cutube.Worker.csproj", "Cutube.Worker/"]
COPY ["src/Cutube.Core/Cutube.Core.csproj", "Cutube.Core/"]
COPY ["src/Cutube.Application/Cutube.Application.csproj", "Cutube.Application/"]
COPY ["src/Cutube.Contracts/Cutube.Contracts.csproj", "Cutube.Contracts/"]
RUN dotnet restore "Cutube.Worker/Cutube.Worker.csproj"
COPY . .
WORKDIR "/src/Cutube.Worker"
RUN dotnet build "Cutube.Worker.csproj" -c Release -o /app/build

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/build .
ENTRYPOINT ["dotnet", "Cutube.Worker.dll"]
```

```dockerfile
# docker/Dockerfile.web
FROM node:20-alpine AS build
WORKDIR /app
COPY ["src/Cutube.Web/package.json", "./"]
RUN npm install
COPY ["src/Cutube.Web/", "./"]
RUN npm run build

FROM node:20-alpine AS runtime
WORKDIR /app
COPY --from=build /app/.next/ ./.next/
COPY --from=build /app/node_modules/ ./node_modules/
COPY ["src/Cutube.Web/package.json", "./"]
EXPOSE 3000
CMD ["npm", "start"]
```

**Checklist:**
- [ ] Dockerfiles de API atualizado
- [ ] Dockerfiles de Worker atualizado
- [ ] Dockerfiles de Web atualizado
- [ ] Builds de test passam

**Critérios de aceite:**
- ✅ Dockerfiles atualizados

---

### Fase 7: Testing & Validation (Testes e Validação)

**Duração:** 1h10min  
**Tasks:** T20-T23

#### 7.1 Rodar Testes de Build (T20)

**Estimativa:** 15 min

**Implementação:**

```bash
# Testar .NET build
dotnet build

# Verificar exit code
echo "Exit code: $?"

# Testar Turborepo build
pnpm build

# Verificar exit code
echo "Exit code: $?"
```

**Checklist:**
- [ ] `dotnet build` passa sem warnings
- [ ] `pnpm build` completa com sucesso
- [ ] Todos os projetos têm artifacts

**Critérios de aceite:**
- ✅ Build completo

---

#### 7.2 Rodar Testes Unitários (T21)

**Estimativa:** 15 min

**Implementação:**

```bash
# Testar .NET
dotnet test --no-build

echo "Exit code: $?"

# Testar via Turborepo
pnpm test

echo "Exit code: $?"
```

**Checklist:**
- [ ] `dotnet test` passa todos os testes
- [ ] `pnpm test` passa todos os testes
- [ ] Coverage não degradada

**Critérios de aceite:**
- ✅ Todos os testes passam

---

#### 7.3 Rodar Smoke Test (T22)

**Estimativa:** 30 min

**Implementação:**

```bash
# Start infra
docker-compose -f docker/docker-compose.yml up -d rabbitmq

# Start services
pnpm dev

# Em outro terminal
curl http://localhost:5000/health
curl http://localhost:4000
```

**Checklist:**
- [ ] RabbitMQ inicia
- [ ] API inicia
- [ ] Worker inicia
- [ ] Web inicia
- [ ] API responde health check
- [ ] Web carrega no browser

**Critérios de aceite:**
- ✅ Sistema funcional

---

#### 7.4 Atualizar Documentação (T23)

**Estimativa:** 20 min

**Implementação:**

```markdown
# README.md (atualizar)

## Quick Start

```bash
# Install dependencies
pnpm install

# Start all services
pnpm dev

# Run tests
pnpm test

# Build all projects
pnpm build
```

## Structure

```
Cutube/
├── src/           # All source code
├── tests/         # All tests
├── scripts/       # Development scripts
└── docker/        # Docker configs
```
```

**Checklist:**
- [ ] `README.md` reflete nova estrutura
- [ ] `.specs/codebase/RESTRUCTURE.md` atualizado
- [ ] `.specs/STATE.md` atualizado

**Critérios de aceite:**
- ✅ Documentação atualizada

---

## Checklist Geral do Épico

### Estrutura
- [ ] Todos os projetos em `src/`
- [ ] Todos os testes em `tests/`
- [ ] Nomes PascalCase (Cutube.Cli, Cutube.Web)
- [ ] Solução atualizada

### Package Management
- [ ] `package.json` na raiz
- [ ] `pnpm-workspace.yaml` na raiz
- [ ] `turbo.json` na raiz
- [ ] Cada projeto tem `package.json`

### Scripts
- [ ] `pnpm dev` inicia tudo
- [ ] `pnpm dev:api` inicia API
- [ ] `pnpm dev:web` inicia Web
- [ ] `pnpm build` builda tudo
- [ ] `pnpm test` testa tudo

### Docker
- [ ] Docker Compose atualizado
- [ ] Dockerfiles atualizados
- [ ] Containers buildam

### Testes Finais
- [ ] `dotnet build` sem warnings
- [ ] `dotnet test` 100% passando
- [ ] `docker-compose up` funciona
- [ ] `pnpm dev` inicia serviços
- [ ] Smoke test passa

### Documentação
- [ ] README.md atualizado
- [ ] .specs/ atualizado
- [ ] Comentários em código crítico

---

## Próximos Passos

Após completar este épico:

1. **Épico 7**: Implementar lógica em Cutube.Core (extrair business logic)
2. **Épico 8**: Implementar adapters em Cutube.Infrastructure (yt-dlp, FFmpeg)
3. **Épico 9**: Implementar use cases em Cutube.Application
4. **Feature**: Observabilidade (OpenTelemetry, Seq)
5. **Feature**: CI/CD (GitHub Actions)

---

## Notas

- **Histórico de Git**: Use SEMPRE `git mv` para preservar histórico
- **Cache do Turborepo**: Primeiro build é lento (~2min), incrementais são rápidos (<10s)
- **Branch Strategy**: Trabalhe em `feature/restructure-monorepo`, PR para `main`
- **Rollback**: Se algo der errado, `git checkout main` e `git branch -D feature/restructure-monorepo`
- **Comunicação**: Comunique time sobre mudanças de estrutura (se houver)

---

**Criado em:** 12/02/2026  
**Status:** 🎯 Planejamento  
**Epico Beads:** Cutube-vxo

---

## Referências

- [Spec](../.specs/features/restructure/spec.md)
- [Design](../.specs/features/restructure/design.md)
- [Tasks](../.specs/features/restructure/tasks.md)
- [RESTRUCTURE.md](../.specs/codebase/RESTRUCTURE.md)
- [Turborepo Docs](https://turbo.build/repo/docs)
- [pnpm Workspaces](https://pnpm.io/workspaces)
