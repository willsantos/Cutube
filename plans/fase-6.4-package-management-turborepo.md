# Fase 6.4: Package Management (Turborepo)

**Status:** 🎯 Planejamento  
**Épico:** Cutube-vxo (Épico 6: Restructure Monorepo)  
**Duração:** 1h10min  
**Responsável:** Backend / Fullstack  
**Prioridade:** 🔥 Alta  
**Dependência:** ✅ Fase 6.3 (Solution & References)

---

## Objetivo

Configurar Turborepo para orquestrar builds, desenvolvimento e testes em todo o monorepo. Criar package.json na raiz, configurar pnpm workspaces, configurar turbopipelines e adicionar package.json para cada projeto.

**Benefícios:**
- ✅ **Builds Incrementais**: Cache inteligente (<10s em mudanças pequenas)
- ✅ **Orquestração**: `pnpm dev` inicia tudo em ordem correta
- ✅ **Paralelização**: Builds de projetos independentes rodam em paralelo
- ✅ **Comandos Unificados**: `pnpm build`, `pnpm test`, `pnpm dev`

---

## Visão Arquitetural

### Árvore de Dependências

```
┌─────────────────────────────────────────────────────────────────────────┐
│                  ÁRVORE DE DEPENDÊNCIAS TURBO                       │
└─────────────────────────────────────────────────────────────────────────┘

pnpm dev
   │
   ├─→ rabbitmq (infra, deve estar rodando)
   │
   └─→ Turborepo orquestra:
       │
       ├─→ API build (API, Domain, Contracts, Application, Core)
       │     └─→ API dev
       │
       ├─→ Worker build (Worker, Contracts, Application, Core)
       │     └─→ Worker dev
       │
       ├─→ Web build (Web)
       │     └─→ Web dev
       │
       └─→ CLI build (CLI, Application, Core)
             └─→ CLI dev
```

### Pipeline do Turborepo

```
┌─────────────────────────────────────────────────────────────────────────┐
│                   TURBO PIPELINE CONFIGURAÇÃO                       │
└─────────────────────────────────────────────────────────────────────────┘

dev:
  - dependsOn: ["^build"] (espera build de dependências)
  - cache: false (processos persistentes não cacheiam)
  - persistent: true (dev servers ficam rodando)

build:
  - dependsOn: ["^build"] (build em ordem topológica)
  - outputs: ["bin/**", "obj/**", ".next/**", "node_modules/.cache/**"]
  - cache: true (cache outputs)

test:
  - dependsOn: ["build"] (espera build completar)
  - outputs: ["TestResults/**"]
  - cache: true (cache test results)
```

### Workspace pnpm

```
┌─────────────────────────────────────────────────────────────────────────┐
│                  PNPM WORKSPACE CONFIGURAÇÃO                        │
└─────────────────────────────────────────────────────────────────────────┘

packages:
  - 'src/*'      # Todos os projetos de produção
  - 'tests/*'     # Todos os projetos de teste

Resulta em:
- src/Cutube.Api/package.json
- src/Cutube.Worker/package.json
- src/Cutube.Cli/package.json
- src/Cutube.Web/package.json
- tests/Cutube.Api.Tests/package.json
- tests/Cutube.Worker.Tests/package.json
- tests/Cutube.Cli.Tests/package.json
- tests/Cutube.Web.E2E/package.json
```

---

## Tarefas

---

### 6.4.1 Criar Root package.json

**Estimativa:** 15 min

**Arquivos:**
```
package.json (root)
```

**Implementação:**

```json
// package.json (root)
{
  "name": "cutube-monorepo",
  "version": "0.1.0",
  "private": true,
  "description": "Cutube - Monorepo reestruturado com Turborepo",
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
- [ ] `package.json` criado na raiz
- [ ] Scripts dev, build, test presentes
- [ ] Turborepo em devDependencies
- [ ] Nome é "cutube-monorepo"
- [ ] Private: true

**Critérios de aceite:**
- ✅ Root package.json criado

---

#### 6.4.2 Mover Workspace Config

**Estimativa:** 10 min

**Arquivos:**
```
pnpm-workspace.yaml (move de src/Cutube.Web/ para root)
```

**Implementação:**

```bash
# Mover de src/Cutube.Web/ para root
git mv src/Cutube.Web/pnpm-workspace.yaml .
```

```yaml
# pnpm-workspace.yaml (root)
packages:
  - 'src/*'
  - 'tests/*'
```

**Checklist:**
- [ ] `pnpm-workspace.yaml` movido para root
- [ ] Aponta para `src/*` e `tests/*`
- [ ] Removido de `src/Cutube.Web/`
- [ ] `pnpm list` mostra todos os pacotes

**Critérios de aceite:**
- ✅ Workspace configurado

---

#### 6.4.3 Criar Turbo Config

**Estimativa:** 15 min

**Arquivos:**
```
turbo.json (root)
```

**Implementação:**

```json
// turbo.json (root)
{
  "$schema": "https://turbo.build/schema.json",
  "globalDependencies": ["**/.env.*local"],
  "pipeline": {
    "dev": {
      "dependsOn": ["^build"],
      "cache": false,
      "persistent": true
    },
    "build": {
      "dependsOn": ["^build"],
      "outputs": ["bin/**", "obj/**", ".next/**", "dist/**"]
    },
    "test": {
      "dependsOn": ["build"],
      "outputs": ["TestResults/**"]
    },
    "lint": {
      "outputs": []
    },
    "clean": {
      "cache": false
    }
  }
}
```

**Checklist:**
- [ ] `turbo.json` criado na raiz
- [ ] Pipelines dev, build, test configurados
- [ ] Outputs definidos corretamente
- [ ] `^` dependency (topológica)

**Critérios de aceite:**
- ✅ Turborepo configurado

---

#### 6.4.4 Adicionar package.json aos Projetos

**Estimativa:** 30 min

**Arquivos:**
```
src/Cutube.Api/package.json
src/Cutube.Worker/package.json
src/Cutube.Cli/package.json
src/Cutube.Web/package.json
tests/Cutube.Api.Tests/package.json
tests/Cutube.Worker.Tests/package.json
tests/Cutube.Cli.Tests/package.json
tests/Cutube.Web.E2E/package.json
```

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

// tests/Cutube.Api.Tests/package.json
{
  "name": "Cutube.Api.Tests",
  "version": "0.1.0",
  "private": true,
  "scripts": {
    "test": "dotnet test Cutube.Api.Tests.csproj"
  }
}

// tests/Cutube.Worker.Tests/package.json
{
  "name": "Cutube.Worker.Tests",
  "version": "0.1.0",
  "private": true,
  "scripts": {
    "test": "dotnet test Cutube.Worker.Tests.csproj"
  }
}

// tests/Cutube.Cli.Tests/package.json
{
  "name": "Cutube.Cli.Tests",
  "version": "0.1.0",
  "private": true,
  "scripts": {
    "test": "dotnet test Cutube.Cli.Tests.csproj"
  }
}

// tests/Cutube.Web.E2E/package.json
{
  "name": "Cutube.Web.E2E",
  "version": "0.1.0",
  "private": true,
  "scripts": {
    "test": "playwright test"
  }
}
```

```bash
# Instalar dependências
pnpm install

# Verificar instalação
pnpm list
```

**Checklist:**
- [ ] `src/Cutube.Api/package.json` existe
- [ ] `src/Cutube.Worker/package.json` existe
- [ ] `src/Cutube.Cli/package.json` existe
- [ ] `src/Cutube.Web/package.json` existe
- [ ] `tests/Cutube.Api.Tests/package.json` existe
- [ ] `tests/Cutube.Worker.Tests/package.json` existe
- [ ] `tests/Cutube.Cli.Tests/package.json` existe
- [ ] `tests/Cutube.Web.E2E/package.json` existe
- [ ] `pnpm install` executa sem erros

**Critérios de aceite:**
- ✅ Todos os projetos têm package.json

---

## Checklist de Fase

### Implementação
- [ ] Root package.json criado
- [ ] pnpm-workspace.yaml configurado
- [ ] turbo.json configurado
- [ ] package.json criado para cada projeto

### Validação
- [ ] `pnpm install` executa sem erros
- [ ] `pnpm list` mostra todos os pacotes
- [ ] `pnpm build` builda todos os projetos
- [ ] Turborepo cache funciona (rodar `pnpm build` 2x, 2x deve ser mais rápido)

---

## Notas

- **Primeiro Build Lento**: Primeira vez que `pnpm build` roda, Turborepo cacheia outputs. Segunda vez será muito mais rápido
- **dev:api vs dev:web**: `pnpm dev:api` inicia apenas API (útil para debugar API isoladamente)
- **Paralelização**: Turborepo automaticamente paraleliza builds de projetos independentes
- **Outputs**: Certifique-se de que `outputs` está correto - se não, Turborepo não detecta mudanças

---

**Criado em:** 12/02/2026  
**Última atualização:** 12/02/2026  
**Tasks Beads:** Cutube-vxo.11 (T11), Cutube-vxo.12 (T12), Cutube-vxo.13 (T13), Cutube-vxo.14 (T14)

---

## Referências

- [Épico 6](./epico-6-restructure-monorepo.md)
- [Turborepo Docs](https://turbo.build/repo/docs)
- [pnpm Workspaces](https://pnpm.io/workspaces)
