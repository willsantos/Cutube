# Fase 6.2: Project Moves (Mover Projetos)

**Status:** 🎯 Planejamento  
**Épico:** Cutube-vxo (Épico 6: Restructure Monorepo)  
**Duração:** 40 minutos  
**Responsável:** Backend / Fullstack  
**Prioridade:** 🔥 Alta  
**Dependência:** ✅ Fase 6.1 (Foundation)

---

## Objetivo

Mover todos os projetos de localizações incorretas (cutube/, cutube-web/, Cutube.Tests/) para suas localizações corretas (src/Cutube.Cli/, src/Cutube.Web/, tests/Cutube.Cli.Tests/), preservando histórico do Git com `git mv`.

**Benefícios:**
- ✅ **Consistência**: Todos os projetos em src/ ou tests/
- ✅ **Padronização**: Nomes PascalCase (Cutube.Cli, Cutube.Web)
- ✅ **Histórico Preservado**: `git mv` mantém full history
- ✅ **Clareza**: Separação óbvia entre produção e testes

---

## Visão Arquitetural

### Movimentação de Arquivos

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    MOVIMENTAÇÃO (ANTES → DEPOIS)                     │
└─────────────────────────────────────────────────────────────────────────┘

CLI (cutube/):
ANTES:                    DEPOIS:
cutube/                    src/Cutube.Cli/
├── Program.cs      →       ├── Program.cs
├── cutube.csproj    →       ├── Cutube.Cli.csproj
└── ...                    └── ...

Web (cutube-web/):
ANTES:                    DEPOIS:
cutube-web/                src/Cutube.Web/
├── app/            →       ├── app/
├── package.json     →       ├── package.json
├── next.config.js  →       ├── next.config.ts
└── ...                    └── ...

CLI Tests (Cutube.Tests/):
ANTES:                    DEPOIS:
Cutube.Tests/             tests/Cutube.Cli.Tests/
├── ...            →       └── ...

Web E2E (cutube-web/e2e/):
ANTES:                    DEPOIS:
cutube-web/e2e/           tests/Cutube.Web.E2E/
├── ...            →       └── ...
```

### Fluxo de Movimentação

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    FLUXO GIT MV                                     │
└─────────────────────────────────────────────────────────────────────────┘

1. git mv cutube src/Cutube.Cli
   │
   ├── Preserva: histórico, permissões, modo
   ├── Atualiza: index, working tree
   └── NÃO: deleta, quebra diffs

2. git mv cutube-web src/Cutube.Web
   │
   └── Mesmos benefícios

3. git mv Cutube.Tests tests/Cutube.Cli.Tests
   │
   └── Mesmos benefícios

4. git mv src/Cutube.Web/e2e tests/Cutube.Web.E2E
   │
   └── Mesmos benefícios

5. git status
   │
   └── Verifica: renamed paths
```

---

## Tarefas

---

### 6.2.1 Mover Projeto CLI

**Estimativa:** 10 min

**Arquivos:**
```
cutube/ → src/Cutube.Cli/
```

**Implementação:**

```bash
# Preservar histórico com git mv
git mv cutube src/Cutube.Cli

# Verificar estado
git status
ls -la src/Cutube.Cli/

# Verificar que arquivos foram movidos
git diff --stat
```

**Checklist:**
- [ ] `cutube/` movido para `src/Cutube.Cli/`
- [ ] Histórico preservado (usou `git mv`)
- [ ] Nenhum arquivo deixado para trás
- [ ] `git status` mostra "renamed:"
- [ ] Todos os arquivos presentes

**Critérios de aceite:**
- ✅ CLI movido com histórico intacto

---

#### 6.2.2 Mover Projeto Web

**Estimativa:** 10 min

**Arquivos:**
```
cutube-web/ → src/Cutube.Web/
```

**Implementação:**

```bash
# Preservar histórico
git mv cutube-web src/Cutube.Web

# Verificar estado
git status
ls -la src/Cutube.Web/

# Verificar que arquivos foram movidos
git diff --stat
```

**Checklist:**
- [ ] `cutube-web/` movido para `src/Cutube.Web/`
- [ ] Histórico preservado
- [ ] Nenhum arquivo deixado para trás
- [ ] `git status` mostra "renamed:"

**Critérios de aceite:**
- ✅ Web movido com histórico intacto

---

#### 6.2.3 Mover Testes CLI

**Estimativa:** 10 min

**Arquivos:**
```
Cutube.Tests/ → tests/Cutube.Cli.Tests/
```

**Implementação:**

```bash
# Preservar histórico
git mv Cutube.Tests tests/Cutube.Cli.Tests

# Verificar estado
git status
ls -la tests/Cutube.Cli.Tests/

# Verificar que testes foram movidos
git diff --stat
```

**Checklist:**
- [ ] `Cutube.Tests/` movido para `tests/Cutube.Cli.Tests/`
- [ ] Histórico preservado
- [ ] Nenhum arquivo deixado para trás
- [ ] `git status` mostra "renamed:"

**Critérios de aceite:**
- ✅ Testes CLI movidos com histórico intacto

---

#### 6.2.4 Mover E2E Web

**Estimativa:** 10 min

**Arquivos:**
```
src/Cutube.Web/e2e/ → tests/Cutube.Web.E2E/
```

**Implementação:**

```bash
# NOTA: Web já foi movido em 6.2.2, então path é src/Cutube.Web/e2e
git mv src/Cutube.Web/e2e tests/Cutube.Web.E2E

# Verificar estado
git status
ls -la tests/Cutube.Web.E2E/

# Verificar que E2E foi movido
git diff --stat
```

**Checklist:**
- [ ] `src/Cutube.Web/e2e/` movido para `tests/Cutube.Web.E2E/`
- [ ] Histórico preservado
- [ ] Nenhum arquivo deixado para trás
- [ ] `git status` mostra "renamed:"

**Critérios de aceite:**
- ✅ E2E Web movido com histórico intacto

---

## Checklist de Fase

### Implementação
- [ ] CLI movido: `cutube/` → `src/Cutube.Cli/`
- [ ] Web movido: `cutube-web/` → `src/Cutube.Web/`
- [ ] Testes CLI movidos: `Cutube.Tests/` → `tests/Cutube.Cli.Tests/`
- [ ] E2E Web movido: `src/Cutube.Web/e2e/` → `tests/Cutube.Web.E2E/`

### Validação
- [ ] Todos os movimentos usaram `git mv`
- [ ] `git status` mostra "renamed:" para todos
- [ ] `git log --follow` mostra histórico preservado
- [ ] Nenhum arquivo órfão
- [ ] Diretórios antigos não existem mais

---

## Notas

- **Ordem Importa**: Mover Web antes de E2E (6.2.2 antes de 6.2.4)
- **Sem cp + rm**: `git mv` preserva histórico, `cp` + `rm` quebra
- **Verificar com git log**: `git log --follow src/Cutube.Cli/Program.cs` deve mostrar histórico de `cutube/Program.cs`
- **Commit Separado**: Movimentação será commitada na Fase 6.5, mas pode ser commitada antes se desejado

---

**Criado em:** 12/02/2026  
**Última atualização:** 12/02/2026  
**Tasks Beads:** Cutube-vxo.9 (T4), Cutube-vxo.5 (T5), Cutube-vxo.4 (T6), Cutube-vxo.8 (T7)

---

## Referências

- [Épico 6](./epico-6-restructure-monorepo.md)
- [Git mv Documentation](https://git-scm.com/docs/git-mv)
