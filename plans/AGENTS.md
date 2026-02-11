# Agent Instructions: Plan Creation

> Create plans in **Portuguese (pt-BR)** following these standards.

## Quick Reference

| Type | File Pattern | Scope | Content |
|------|-------------|-------|---------|
| **Epic** | `epico-N-name.md` | Macro (weeks) | Architecture, multiple tasks |
| **Phase** | `fase-N.M-name.md` | Micro (days) | Complete code, implementation |

## Naming Convention

- **Epics**: `epico-3-integracao-rabbitmq.md`
- **Phases**: `fase-3.5-retry-dead-letter-queue.md`
- Use kebab-case, no accents

## Hierarchy

```
Epic N → Phase N.M → Task N.M.X → Subtask N.M.X.Y
```

## Required Structure

### Epic Header
```markdown
# Épico [N]: [Título]

**Status:** 🎯 Planejamento  
**Duração:** [X-Y dias/semanas]  
**Responsável:** [Role]  
**Prioridade:** 🔥 [Alta/Média/Baixa]  
**Dependência:** [✅/⏳] [Reference]
```

### Phase Header
```markdown
# Fase [N.M]: [Título]

**Status:** 🎯 Planejamento  
**Épico:** [ID] (Épico [N]: [Nome])  
**Duração:** [X-Y dias]  
**Responsável:** [Role]  
**Prioridade:** 🔥 [Alta/Média/Baixa]  
**Dependência:** [✅/⏳] [Reference]
```

## Key Rules

### Epics Must Have
- [ ] Index with anchor links
- [ ] High-level architecture (ASCII diagrams)
- [ ] Numbered tasks (N.1, N.2)
- [ ] Benefits listed
- [ ] NO detailed code

### Phases Must Have
- [ ] Reference to parent epic
- [ ] Complete, working code
- [ ] Detailed diagrams
- [ ] Verifiable checklists
- [ ] Acceptance criteria

## Emojis

| Context | Emoji | Meaning |
|---------|-------|---------|
| Status | 🎯 🚧 ✅ | Planning / In Progress / Done |
| Priority | 🔥 ⚡ 💤 | High / Medium / Low |
| Dependency | ✅ ⏳ | Complete / Pending |

## Quality Checklist

Before committing:
- [ ] Filename follows pattern
- [ ] Header complete
- [ ] Index present (epics only)
- [ ] Code complete (phases only)
- [ ] No placeholder [brackets]
- [ ] References correct

## Anti-Patterns

❌ **Wrong:**
```
epico_3_rabbitmq.md
fase35retry.md
public void Process() { /* TODO */ }
- [ ] Works well
```

✅ **Correct:**
```
epico-3-integracao-rabbitmq.md
fase-3.5-retry-dead-letter-queue.md
public void Process() { /* full implementation */ }
- [ ] Tests pass: dotnet test
```

## References

- [PLAN-EPICO-TEMPLATE.md](PLAN-EPICO-TEMPLATE.md)
- [PLAN-FASE-TEMPLATE.md](PLAN-FASE-TEMPLATE.md)
- Example Epic: [epico-3-integracao-rabbitmq.md](epico-3-integracao-rabbitmq.md)
- Example Phase: [fase-3.5-retry-dead-letter-queue.md](fase-3.5-retry-dead-letter-queue.md)
