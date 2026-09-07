---
name: spec-driven-dev
description: Feature planning with 4 phases - Specify requirements, Design architecture, break into granular Tasks, Implement and Validate. Creates atomic tasks that agents can implement without errors. Triggers on "plan feature", "design", "new feature", "implement feature", "create spec".
---

# Spec-Driven Development

Plan and implement features with precision. Granular tasks. Clear dependencies. Right tools.

```
┌──────────┐   ┌──────────┐   ┌─────────┐   ┌───────────────────┐
│ SPECIFY  │ → │  DESIGN  │ → │  TASKS  │ → │ IMPLEMENT+VALIDATE│
└──────────┘   └──────────┘   └─────────┘   └───────────────────┘
```

## Phase Selection

| User wants to... | Load reference |
|------------------|----------------|
| Define what to build | [specify.md](references/specify.md) |
| Structure an epic/migration (macro) | [specify.md](references/specify.md) → "Epic/Macro Specs" |
| Design architecture | [design.md](references/design.md) |
| Break into tasks | [tasks.md](references/tasks.md) |
| Implement a task | [implement.md](references/implement.md) |
| Verify it works | [validate.md](references/validate.md) |

## Commands

| Command | Action |
|---------|--------|
| `specify [feature]` | Define requirements |
| `specify --epic [name]` | Define MACRO requirements (epic broken into features, not tasks) |
| `design [feature]` | Design architecture |
| `tasks [feature]` | Create task breakdown |
| `implement T1` | Implement task |
| `validate` | Verify implementation |

## Output

```
.specs/[feature-slug]/
├── spec.md
├── design.md
└── tasks.md
```

Epic/macro specs live at `.specs/[epic-slug]/spec.md` and each feature listed
there gets its own child directory later:

```
.specs/[epic-slug]/
├── spec.md                      # macro spec (tracks, features, decisions)
└── [feature-slug]/
    ├── spec.md
    ├── design.md
    └── tasks.md
```

## Project Conventions

- **Task tracking**: this repo uses `bd` (beads). Approved task breakdowns are
  mirrored to beads — see [tasks.md](references/tasks.md) → "Sync with Beads".
- **Quality gates**: `dotnet build` (zero warnings) and `dotnet test` (100%
  pass) are mandatory before any task closes — see
  [validate.md](references/validate.md).
- **Branching**: one branch per feature, conventional commits, PR to `develop`
  — see [implement.md](references/implement.md).
