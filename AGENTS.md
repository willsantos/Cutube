# Agentes (IA) - Diretrizes para Especificação

## ⚠️ CRÍTICO: .specs NÃO deve ser commitado

**Status:** `.specs/` está listado no `.gitignore` (linha 494)

**Motivo:** 
- Arquivos `.specs/` são **documentação de planejamento e análise**
- Devem ser usados para **tomada de decisão** e **contexto**
- **NÃO** são código ou documentação oficial do projeto
- Mantém o repositório limpo focado em implementação

## 📁 O que é .specs/

- **Especificações funcionais**: `.specs/features/` - specs, design, tasks de features
- **Documentação técnica**: `.specs/codebase/` - arquitetura, convenções, stack
- **Documentação de projeto**: `.specs/project/` - roadmap, estado do projeto

## 🚫 O que AGENTES NÃO devem fazer

❌ **NÃO** criar tarefas para adicionar .specs ao git
❌ **NÃO** commitar arquivos .specs/ (eles estão no .gitignore por motivo)
❌ **NÃO** mover documentação oficial para .specs/ (use docs/ ou README.md)

## ✅ O que AGENTES devem fazer

✅ **USAR** .specs/ como contexto para entender decisões e arquitetura
✅ **LER** specs ao planejar features (entender requisitos, design, trade-offs)
✅ **CRIAR** specs ao planejar features grandes (seguir template .oroborus-docs)
✅ **ATUALIZAR** specs quando decisões técnicas mudam (via ADR)
✅ **DOCUMENTAR** código em docs/ ou README.md (não em .specs/)

## 📋 Quando criar .specs/

Use `.oroborus-docs` skill para criar:
- **PRD** (Product Requirements Document) - requisitos de feature
- **SDD** (Software Design Document) - arquitetura e design técnico
- **ADR** (Architecture Decision Record) - decisões técnicas
- **EPIC** - quebras de épico em tarefas
- **Status** - relatórios de progresso

**Template:** Veja `.opencode/skills/oroborus-docs/`

## 🎯 Diretrizes por Tipo de Documento

### PRD (Product Requirements)
- **Onde:** `.specs/features/{nome}/spec.md`
- **Quando:** Planejar nova feature ou epic
- **Conteúdo:** Requisitos funcionais, critérios de aceitação, user stories

### SDD (Software Design)
- **Onde:** `.specs/features/{nome}/design.md`
- **Quando:** Feature requer decisões arquiteturais significativas
- **Conteúdo:** Arquitetura, padrões, trade-offs, diagramas

### ADR (Architecture Decision)
- **Onde:** `.specs/codebase/decisions/` ou `.specs/features/{nome}/decisions.md`
- **Quando:** Mudança em decisão técnica existente
- **Conteúdo:** Contexto, decisão, consequências, alternativas

### EPIC
- **Onde:** `.specs/features/{epic}/epic.md`
- **Quando:** Quebrar epic em tarefas
- **Conteúdo:** Visão geral, dependências, riscos, milestones

## 🔍 Integração com Beads (bd)

**Planejamento:**
1. Criar spec/design em `.specs/features/{nome}/`
2. Criar tarefa no Beads: `bd create`
3. Vincular spec à tarefa: campo "Spec" da tarefa
4. Implementar seguindo spec

**Decisões em código:**
1. Criar ADR para decisão técnica
2. Atualizar código
3. Documentar consequências no ADR

## 📌 Exemplo de Fluxo Correto

```bash
# 1. Planejar feature (opcional: usar skill oroborus-docs)
mkdir .specs/features/nova-feature
# Criar spec.md, design.md

# 2. Criar tarefa no Beads
bd create "Implement nova feature"

# 3. Implementar
git checkout -b feat/nova-feature
# ... código ...

# 4. Commitar código (NÃO os .specs)
git add src/ tests/
git commit -m "feat: implement nova feature"

# 5. Push e PR
git push
gh pr create
```

## ⚠️ Erros Comuns a Evitar

### ❌ Errado: Commitar .specs
```bash
git add .specs/  # ERRADO - está no .gitignore
```

### ✅ Correto: Usar .specs como contexto
```bash
# Ler .specs/features/nova-feature/spec.md para entender requisitos
# Implementar seguindo spec
# Commitar apenas código
```

### ❌ Errado: Criar tarefa para "adicionar .specs ao git"
```bash
bd create "Add .specs to git"  # NÃO FAZER
```

### ✅ Correto: Criar tarefa para feature
```bash
bd create "Implement user authentication"  # Spec está em .specs/features/auth/
```

## 📚 Recursos Adicionais

- **Skill oroborus-docs:** Criar specs seguindo template padrão
- **Beads:** Gerenciar tarefas e vincular a specs
- **Git:** Versionar código e documentação oficial (README.md, docs/)
- **.specs:** Contexto e planejamento (não versionado)

## 🎓 Resumo para Agentes

| Pergunta | Resposta |
|-----------|----------|
| Devo commitar .specs/? | **NÃO** - está no .gitignore |
| Onde criar specs? | `.specs/features/{nome}/` |
| Onde documentar código? | `README.md`, `docs/`, comentários |
| Quando usar .specs? | Planejamento, decisões, contexto |
| Quando versionar docs? | Documentação oficial (guias, manuais) |

---

**Última atualização:** 2025-02-13
**Propósito:** Evitar que agentes commitem .specs/ acidentalmente
