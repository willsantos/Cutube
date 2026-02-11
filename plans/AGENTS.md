# Instructions for Agents: Plan Creation

> **This document defines the standard for creating Epic and Phase plans in the Cutube project.**
> 
> **IMPORTANT:** These instructions are in English, but the actual plans you create should be in **Portuguese (pt-BR)** as the product is for Portuguese-speaking users.

---

## 📋 Overview

This directory (`plans/`) contains all project planning documentation. There are two types of plans:

1. **Epics** (`epico-N-nome.md`) - Macro, strategic objectives
2. **Phases** (`fase-N.M-nome.md`) - Specific, tactical implementations

---

## 📁 Directory Structure

```
plans/
├── PLAN-EPICO-TEMPLATE.md      # Template for creating epics
├── PLAN-FASE-TEMPLATE.md       # Template for creating phases
├── AGENTS.md                   # This file
├── epico-3-integracao-rabbitmq.md
├── epico-4-preparacao-deploy.md
├── fase-3.1-rabbitmq-setup.md
├── fase-3.5-retry-dead-letter-queue.md
└── ...
```

---

## 🎯 Fundamental Rules

### 1. Separation of Responsibilities

| Type | Scope | Content | Example |
|------|--------|----------|---------|
| **Epic** | Macro (weeks) | Architectural vision, multiple tasks | "Integração com RabbitMQ" |
| **Phase** | Micro (days) | Complete code, detailed implementation | "RabbitMQ Setup" |

### 2. Numeric Hierarchy

- **Epics**: `1`, `2`, `3`, `4`...
- **Phases**: `3.1`, `3.2`, `3.5`... (N.M where N = epic, M = phase)
- **Tasks**: `3.5.1`, `3.5.2`... (N.M.X)
- **Subtasks**: `3.5.1.1`, `3.5.1.2`... (N.M.X.Y)

### 3. File Naming

```bash
# Epics
plans/epico-{N}-{short-name}.md
ex: epico-3-integracao-rabbitmq.md

# Phases
plans/fase-{N.M}-{short-name}.md
ex: fase-3.5-retry-dead-letter-queue.md
```

**Naming rules:**
- Use kebab-case (lowercase with hyphens)
- Be descriptive but concise
- Avoid accents and special characters
- Maintain consistency with document title

---

## 📝 Creation Workflow

### To Create an Epic

1. **Consult the template**: Read `PLAN-EPICO-TEMPLATE.md`
2. **Define the number**: What is the next available number?
3. **Create the file**: `plans/epico-{N}-{name}.md`
4. **Follow the structure**:
   - Header with metadata
   - Index with anchor links
   - Clear objective
   - Architectural vision
   - Numbered tasks
   - General checklist
5. **Validate**: Is the epic complete but not overly detailed?
6. **Commit**: `docs: add Epic N plan for [objective]`

### To Create a Phase

1. **Consult the template**: Read `PLAN-FASE-TEMPLATE.md`
2. **Identify the parent epic**: Which epic does this phase belong to?
3. **Define the number**: What is the next available N.M?
4. **Create the file**: `plans/fase-{N.M}-{name}.md`
5. **Follow the structure**:
   - Header with epic reference
   - Specific objective
   - Detailed diagrams
   - Complete and functional code
   - Verifiable checklists
6. **Validate**: Can the code be copied and work?
7. **Commit**: `docs: add Phase N.M plan for [objective]`

---

## ✅ Quality Checklist

### Before Committing

- [ ] File name follows the standard
- [ ] Header complete with all metadata
- [ ] Index present (for epics) and working
- [ ] Clear and measurable objective
- [ ] Well-formatted ASCII diagrams
- [ ] Complete code (not pseudocode) for phases
- [ ] Checklists are verifiable (not subjective)
- [ ] Acceptance criteria are measurable
- [ ] References to other plans are correct
- [ ] No placeholder [brackets] left behind

### Content Validation

**Epics must have:**
- High-level vision
- Macro architecture
- Well-defined tasks but no detailed code
- Clear benefits listed

**Phases must have:**
- Complete and functional code
- Specific configurations
- Detailed flow diagrams
- Implementation checklists

---

## 🎨 Formatting Conventions

### Emojis and Status

| Usage | Emoji | Meaning |
|-------|-------|---------|
| Status | 🎯 | Planning |
| Status | 🚧 | In Progress |
| Status | ✅ | Completed |
| Priority | 🔥 | High |
| Priority | ⚡ | Medium |
| Priority | 💤 | Low |
| Dependency | ✅ | Complete |
| Dependency | ⏳ | Pending |
| Criteria | ✅ | Achieved |

### Epic Header

```markdown
# Épico [N]: [Title]

**Status:** 🎯 Planejamento  
**Duração:** [X-Y days/weeks]  
**Responsável:** [Role/Team]  
**Prioridade:** 🔥 [Alta/Média/Baixa]  
**Dependência:** [Status] [Reference]
```

### Phase Header

```markdown
# Fase [N.M]: [Title]

**Status:** 🎯 Planejamento  
**Épico:** [ID] (Épico [N]: [Name])  
**Duração:** [X-Y days]  
**Responsável:** [Role]  
**Prioridade:** 🔥 [Alta/Média/Baixa]  
**Dependência:** [Status] [Reference]
```

### Required Sections

#### Index (Epics Only)

```markdown
## Índice

- [Objetivo](#objetivo)
- [Visão Arquitetural](#visão-arquitetural)
- [Tarefas](#tarefas)
  - [4.1 Task Name](#41-task-name)
  - [4.2 Another Task](#42-another-task)
- [Checklist Geral](#checklist-geral)
- [Próximos Passos](#próximos-passos)
```

#### Checklists

```markdown
**Checklist:**
- [ ] Specific and verifiable item
- [ ] Another item with clear criteria

**Critérios de aceite:**
- ✅ [Measurable criterion 1]
- ✅ [Measurable criterion 2]
```

---

## 💻 Code Standards

### For Phases (Complete Code)

**Must include:**
- Complete file name
- Correct namespace
- All necessary using/imports
- Explanatory comments
- Complete implementation (not stubs)

**Correct example:**
```csharp
// src/Cutube.Worker/Configuration/RetryPolicyOptions.cs
namespace Cutube.Worker.Configuration;

/// <summary>
/// Configurações da política de retry.
/// </summary>
public class RetryPolicyOptions
{
    public const string SectionName = "RetryPolicy";

    public int MaxRetries { get; set; } = 3;
    public int InitialDelaySeconds { get; set; } = 5;
    
    public TimeSpan GetDelayForAttempt(int attemptNumber)
    {
        // Complete implementation here
    }
}
```

**DON'T do:**
```csharp
// Incomplete stub
public class RetryPolicyOptions 
{
    // TODO: implement
}
```

### ASCII Diagrams

**Use clear structures:**
```
┌─────────────────────────────────────┐
│           Container                 │
│  ┌──────────────┐  ┌─────────────┐  │
│  │ Component A  │──│ Component B │  │
│  └──────────────┘  └─────────────┘  │
└─────────────────────────────────────┘
```

---

## 🔗 References and Links

### Link to other plans

```markdown
**Dependência:** ✅ [Fase 3.4](fase-3.4-status-tracking.md) completa
**Épico:** Cutube-858 ([Épico 3](epico-3-integracao-rabbitmq.md))
```

### Internal references

```markdown
Veja [PLAN-EPICO-TEMPLATE.md](PLAN-EPICO-TEMPLATE.md) para mais detalhes.
Consulte o [AGENTS.md](AGENTS.md) para instruções.
```

---

## 🚫 Anti-Patterns

### What to AVOID

❌ **Inconsistent file names**
```
# Wrong
epico_3_rabbitmq.md
Epico3.md
fase35retry.md

# Correct
epico-3-integracao-rabbitmq.md
fase-3.5-retry-dead-letter-queue.md
```

❌ **Incomplete code in phases**
```csharp
// Wrong - stub
public void Process() {
    // TODO: implement logic
}

// Correct - complete implementation
public void Process() {
    var result = _service.Execute();
    if (result.IsSuccess) {
        _repository.Save(result.Data);
    }
}
```

❌ **Subjective checklists**
```markdown
# Wrong
- [ ] Works well
- [ ] Is optimized

# Correct
- [ ] Tests pass: `dotnet test`
- [ ] Build without warnings: `dotnet build`
- [ ] Response time < 100ms
```

❌ **Forgetting index in epics**
```markdown
# Wrong - starts directly at Objective
## Objetivo
...

# Correct - has index before
## Índice
- [Objetivo](#objetivo)
...

## Objetivo
```

---

## 📚 References

- [PLAN-EPICO-TEMPLATE.md](PLAN-EPICO-TEMPLATE.md) - Template for epics
- [PLAN-FASE-TEMPLATE.md](PLAN-FASE-TEMPLATE.md) - Template for phases
- Examples:
  - [epico-3-integracao-rabbitmq.md](epico-3-integracao-rabbitmq.md)
  - [fase-3.5-retry-dead-letter-queue.md](fase-3.5-retry-dead-letter-queue.md)

---

## 🔄 Maintenance

This document should be updated when:
- New standards are established
- Templates are modified
- New excellent examples are created

**Last updated:** 11/02/2026
