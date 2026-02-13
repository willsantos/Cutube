# Fase 6.5: Scripts (Scripts Unificados)

**Status:** 🎯 Planejamento  
**Épico:** Cutube-vxo (Épico 6: Restructure Monorepo)  
**Duração:** 1 hora  
**Responsável:** Backend / Fullstack  
**Prioridade:** 🔥 Alta  
**Dependência:** ✅ Fase 6.4 (Package Management)

---

## Objetivo

Criar scripts unificados para desenvimento (dev.sh), build (build.sh) e test (test.sh) que simplificam o fluxo de trabalho. Atualizar .gitignore para ignorar `.specs/` e outros arquivos de monorepo. Commitar todas as mudanças de reestruturação.

**Benefícios:**
- ✅ **Simplicidade**: `./scripts/dev.sh` inicia tudo
- ✅ **Consistência**: Scripts sempre funcionam igual
- ✅ **Onboarding**: Novos devs iniciam com `./scripts/dev.sh`
- ✅ **Histórico Limpo**: Commit atomiza todas as mudanças

---

## Visão Arquitetural

### Scripts de Alto Nível

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    SCRIPTS DE ALTO NÍVEL                           │
└─────────────────────────────────────────────────────────────────────────┘

scripts/dev.sh
   │
   ├─→ Inicia infra (RabbitMQ via docker-compose)
   │
   ├─→ Instala dependências (pnpm install)
   │
   └─→ Inicia serviços (pnpm dev)
       - API (localhost:5000)
       - Worker (background)
       - Web (localhost:4000)
       - CLI (disponível)

scripts/build.sh
   │
   └─→ Builda todos os projetos (pnpm build)
       - Cache outputs (Turborepo)
       - Paraleliza builds

scripts/test.sh
   │
   └─→ Roda todos os testes (pnpm test)
       - .NET tests (xUnit)
       - E2E tests (Playwright)
```

### Fluxo de Commit

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    FLUXO DE COMMIT                                   │
└─────────────────────────────────────────────────────────────────────────┘

git add .
  │
  ├── Adiciona: projetos movidos
  ├── Adiciona: package.json files
  ├── Adiciona: scripts/
  ├── Adiciona: .gitignore atualizado
  └── Adiciona: .specs/ (docs)

git commit -m "refactor: reorganize monorepo structure

- Move projects to src/ and tests/
- Add Turborepo configuration
- Create unified development scripts
- Update solution file"

  │
  └─→ Single commit atomiza toda reestruturação
```

---

## Tarefas

---

### 6.5.1 Criar Scripts de Desenvolvimento

**Estimativa:** 30 min

**Arquivos:**
```
scripts/dev.sh
scripts/build.sh
scripts/test.sh
```

**Implementação:**

```bash
#!/bin/bash
# scripts/dev.sh
set -e

echo "🚀 Starting Cutube development environment..."

# Start infrastructure
echo "📦 Starting infrastructure (RabbitMQ)..."
docker-compose -f docker/docker-compose.yml up -d rabbitmq

# Wait for infrastructure
echo "⏳ Waiting for infrastructure to be ready..."
sleep 5

# Install dependencies
echo "📦 Installing dependencies..."
pnpm install

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
# Tornar executáveis
chmod +x scripts/*.sh
```

**Checklist:**
- [ ] `scripts/dev.sh` existe e é executável
- [ ] `scripts/build.sh` existe e é executável
- [ ] `scripts/test.sh` existe e é executável
- [ ] Scripts usam comandos pnpm
- [ ] `./scripts/dev.sh` funciona

**Critérios de aceite:**
- ✅ Scripts funcionais

---

#### 6.5.2 Atualizar .gitignore

**Estimativa:** 10 min

**Arquivos:**
```
.gitignore (atualizar)
```

**Implementação:**

```gitignore
# .gitignore (adicionar)

# Specs (working documents)
.specs/

# Turborepo
.turbo

# pnpm
.pnpm-store

# Node (Turborepo installs in root)
node_modules/

# Next.js (Web)
.next/
out/

# Environment
.env*.local
.env

```

**Checklist:**
- [ ] `.specs/` ignorado
- [ ] `.turbo/` ignorado
- [ ] `.pnpm-store/` ignorado
- [ ] `node_modules/` (root) ignorado
- [ ] Padrões antigos removidos (se houver)

**Critérios de aceite:**
- ✅ .gitignore atualizado

---

#### 6.5.3 Commitar Mudanças

**Estimativa:** 20 min

**Arquivos:**
```
git add .
git commit
```

**Implementação:**

```bash
# Adicionar tudo
git add .

# Verificar o que será commitado
git status

# Commitar
git commit -m "refactor: reorganize monorepo structure

- Move projects to src/ and tests/
- Add Turborepo configuration
- Create unified development scripts
- Update solution file"

# Verificar commit
git log --oneline -1

# Verificar que working tree está limpo
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

## Checklist de Fase

### Implementação
- [ ] scripts/dev.sh criado
- [ ] scripts/build.sh criado
- [ ] scripts/test.sh criado
- [ ] .gitignore atualizado
- [ ] Mudanças commitadas

### Validação
- [ ] `./scripts/dev.sh` inicia infra e serviços
- [ ] `./scripts/build.sh` builda tudo
- [ ] `./scripts/test.sh` testa tudo
- [ ] `git log` mostra commit
- [ ] `git status` mostra working tree limpo

---

## Notas

- **Scripts Dev vs. pnpm dev**: `./scripts/dev.sh` = `docker-compose up -d rabbitmq` + `pnpm dev`
- **Abortar pnpm dev**: `Ctrl+C` aborta todos os serviços
- **Scripts em Bash**: Se no Windows, use WSL ou Git Bash
- **Commit Atomico**: Todas as mudanças de reestruturação em um commit facilita `revert` se necessário

---

**Criado em:** 12/02/2026  
**Última atualização:** 12/02/2026  
**Tasks Beads:** Cutube-vxo.16 (T15), Cutube-vxo.15 (T16), Cutube-vxo.19 (T17)

---

## Referências

- [Épico 6](./epico-6-restructure-monorepo.md)
- [Conventional Commits](https://www.conventionalcommits.org/)
