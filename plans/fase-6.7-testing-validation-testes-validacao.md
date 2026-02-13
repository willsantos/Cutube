# Fase 6.7: Testing & Validation (Testes e Validação)

**Status:** 🎯 Planejamento  
**Épico:** Cutube-vxo (Épico 6: Restructure Monorepo)  
**Duração:** 1h10min  
**Responsável:** Backend / Fullstack  
**Prioridade:** 🔥 Alta  
**Dependência:** ✅ Fase 6.6 (Docker)

---

## Objetivo

Executar testes abrangentes para validar que reestruturação foi completa e funcional: builds, testes unitários, testes de integração (smoke test) e atualização de documentação.

**Benefícios:**
- ✅ **Confiança**: Reestruturação validada end-to-end
- ✅ **Qualidade**: Builds sem warnings, testes 100% passando
- ✅ **Documentação**: README atualizado reflete nova realidade

---

## Visão Arquitetural

### Testes em Camadas

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    TESTES EM CAMADAS                                 │
└─────────────────────────────────────────────────────────────────────────┘

1. Build Test (dotnet build, pnpm build)
   │
   ├─→ Compila: .csproj, .cs
   ├─→ Valida: referências, paths
   └─→ Saída: bin/, obj/, .next/
   │
   ▼
2. Unit Test (dotnet test, pnpm test)
   │
   ├─→ Roda: xUnit, Playwright
   ├─→ Valida: lógica, componentes
   └─→ Saída: TestResults/
   │
   ▼
3. Smoke Test (docker-compose up, curl)
   │
   ├─→ Inicia: infra (RabbitMQ)
   ├─→ Inicia: serviços (API, Worker, Web)
   ├─→ Verifica: health, endpoints
   └─→ Saída: 200 OK
   │
   ▼
4. Doc Test (README.md, .specs/)
   │
   └─→ Valida: documentação atualizada
```

### Smoke Test Flow

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    SMOKE TEST FLOW                                   │
└─────────────────────────────────────────────────────────────────────────┘

Terminal 1: ./scripts/dev.sh
   │
   ├─→ docker-compose up -d rabbitmq
   ├─→ pnpm dev
   │     ├─→ dotnet watch Cutube.Api (localhost:5000)
   │     ├─→ dotnet watch Cutube.Worker
   │     ├─→ dotnet watch Cutube.Cli
   │     └─→ next dev --turbo (localhost:4000)
   │
Terminal 2: curl health
   │
   ├─→ curl http://localhost:5000/health
   │     └─→ Expected: 200 OK
   │
   └─→ curl http://localhost:4000
         └─→ Expected: 200 OK (Web UI)
```

---

## Tarefas

---

### 6.7.1 Rodar Testes de Build

**Estimativa:** 15 min

**Arquivos:**
```
(saída: bin/, obj/, .next/)
```

**Implementação:**

```bash
# Testar .NET build
dotnet build

# Verificar exit code
echo "Exit code: $?"

# Deve ser 0 (sem erros, sem warnings)

# Testar Turborepo build
pnpm build

# Verificar exit code
echo "Exit code: $?"

# Deve ser 0
```

**Checklist:**
- [ ] `dotnet build` passa sem erros
- [ ] `dotnet build` passa sem warnings
- [ ] `pnpm build` completa com sucesso
- [ ] `bin/`, `obj/`, `.next/` criados
- [ ] Exit code é 0

**Critérios de aceite:**
- ✅ Build completo

---

#### 6.7.2 Rodar Testes Unitários

**Estimativa:** 15 min

**Arquivos:**
```
(saída: TestResults/)
```

**Implementação:**

```bash
# Testar .NET
dotnet test --no-build

# Verificar exit code
echo "Exit code: $?"

# Deve ser 0 (todos os testes passam)

# Testar via Turborepo
pnpm test

# Verificar exit code
echo "Exit code: $?"

# Deve ser 0
```

**Checklist:**
- [ ] `dotnet test` passa todos os testes
- [ ] `pnpm test` passa todos os testes
- [ ] Coverage não degradada (se measuring)
- [ ] TestResults/ criado
- [ ] Exit code é 0

**Critérios de aceite:**
- ✅ Todos os testes passam

---

#### 6.7.3 Rodar Smoke Test

**Estimativa:** 30 min

**Arquivos:**
```
(saída: containers rodando)
```

**Implementação:**

```bash
# Terminal 1: Iniciar tudo
./scripts/dev.sh

# Terminal 2: Verificar health
curl http://localhost:5000/health

# Expected:
# HTTP/1.1 200 OK
# {"status":"healthy",...

# Verificar Web
curl http://localhost:4000

# Expected:
# HTML response (Next.js)

# Verificar RabbitMQ
curl http://localhost:15672

# Expected:
# RabbitMQ Management UI
```

**Checklist:**
- [ ] RabbitMQ inicia
- [ ] API inicia
- [ ] Worker inicia
- [ ] Web inicia
- [ ] API responde health check (200 OK)
- [ ] Web carrega (200 OK)
- [ ] RabbitMQ Management UI disponível

**Critérios de aceite:**
- ✅ Sistema funcional

---

#### 6.7.4 Atualizar Documentação

**Estimativa:** 20 min

**Arquivos:**
```
README.md
.specs/codebase/RESTRUCTURE.md (atualizar)
.specs/STATE.md (atualizar)
```

**Implementação:**

```markdown
# README.md (atualizar)

## Quick Start

```bash
# Clone
git clone https://github.com/willsantos/Cutube
cd Cutube

# Install dependencies
pnpm install

# Start all services
./scripts/dev.sh

# Or start individual services
pnpm dev:api
pnpm dev:web
pnpm dev:cli

# Run tests
./scripts/test.sh

# Build all projects
./scripts/build.sh
```

## Structure

```
Cutube/
├── src/           # All source code
│   ├── Cutube.Api/          # REST API
│   ├── Cutube.Worker/       # Background worker
│   ├── Cutube.Domain/        # Domain models
│   ├── Cutube.Contracts/     # Shared contracts
│   ├── Cutube.Core/         # Business logic
│   ├── Cutube.Infrastructure/ # External adapters
│   ├── Cutube.Application/   # Use cases
│   ├── Cutube.Cli/          # CLI application
│   └── Cutube.Web/          # Next.js web
├── tests/         # All tests
│   ├── Cutube.Api.Tests/
│   ├── Cutube.Worker.Tests/
│   ├── Cutube.Domain.Tests/
│   ├── Cutube.Cli.Tests/
│   └── Cutube.Web.E2E/
├── scripts/       # Development scripts
│   ├── dev.sh
│   ├── build.sh
│   └── test.sh
└── docker/        # Docker configs
    ├── docker-compose.yml
    ├── Dockerfile.api
    ├── Dockerfile.worker
    └── Dockerfile.web
```

## Development

```bash
# Watch mode (hot reload)
pnpm dev

# Individual services
pnpm dev:api   # API on :5000
pnpm dev:web   # Web on :4000
pnpm dev:worker # Worker (background)
pnpm dev:cli   # CLI (local)
```

## Testing

```bash
# All tests
./scripts/test.sh

# .NET tests
dotnet test

# E2E tests
cd src/Cutube.Web
pnpm test
```

## Building

```bash
# All projects
./scripts/build.sh

# Individual projects
dotnet build src/Cutube.Api
next build src/Cutube.Web
```
```

**Checklist:**
- [ ] README.md reflete nova estrutura
- [ ] `.specs/codebase/RESTRUCTURE.md` atualizado
- [ ] `.specs/STATE.md` atualizado
- [ ] Quick start está correto
- [ ] Estrutura está correta

**Critérios de aceite:**
- ✅ Documentação atualizada

---

## Checklist de Fase

### Testes
- [ ] `dotnet build` passa sem warnings
- [ ] `dotnet test` 100% passando
- [ ] `docker-compose up` funciona
- [ ] `./scripts/dev.sh` inicia serviços
- [ ] Smoke test passa

### Documentação
- [ ] README.md atualizado
- [ ] .specs/ atualizado
- [ ] Quick start correto
- [ ] Estrutura documentada

---

## Notas

- **Smoke Test Opcional**: Se Docker não estiver disponível, smoke test pode ser skipado
- **Warnings**: `dotnet build` deve passar sem warnings - se houver warnings, corrija
- **Testes Falhando**: Se testes falharem, investigue - pode ser path issues
- **Web UI**: Smoke test verifica que Web responde (200 OK), não que UI está correta

---

**Criado em:** 12/02/2026  
**Última atualização:** 12/02/2026  
**Tasks Beads:** Cutube-vxo.21 (T20), Cutube-vxo.23 (T21), Cutube-vxo.20 (T22), Cutube-vxo.22 (T23)

---

## Referências

- [Épico 6](./epico-6-restructure-monorepo.md)
- [.NET Testing](https://learn.microsoft.com/en-us/dotnet/core/testing/)
- [Playwright](https://playwright.dev/)
