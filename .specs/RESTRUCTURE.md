# Cutube - Restructuring Proposal

## Current Structure Issues

### Inconsistencies

1. **Testes divididos em dois locais**:
   ```
   Cutube.Tests/     # CLI tests (raiz)
   tests/             # API, Worker, Domain tests (raiz)
   ```

2. **Frontend não segue padrão .NET**:
   ```
   cutube-web/         # kebab-case
   cutube/             # CLI (lowercase)
   src/Cutube.Api/     # PascalCase
   ```

3. **CLI fora da estrutura organizada**:
   ```
   src/                # Projetos .NET organizados
   cutube/             # CLI fora (raiz)
   cutube-web/         # Web fora (raiz)
   ```

## Proposals

### Option 1: Full Monorepo (Recommended)

#### Philosophy
**Tudo que é código fonte em um único lugar**: `src/`

```
Cutube/
├── src/                          # ALL source code
│   ├── Cutube.Api/               # ASP.NET Core API
│   ├── Cutube.Worker/            # Background worker
│   ├── Cutube.Domain/             # Domain models
│   ├── Cutube.Contracts/          # Shared contracts
│   ├── Cutube.Core/              # Business logic (NEW)
│   ├── Cutube.Infrastructure/     # yt-dlp, FFmpeg (NEW)
│   ├── Cutube.Application/       # Use cases (NEW)
│   ├── Cutube.Cli/              # CLI application (MOVED from cutube/)
│   └── Cutube.Web/              # Next.js web (MOVED from cutube-web/)
│
├── tests/                        # ALL tests
│   ├── Cutube.Api.Tests/
│   ├── Cutube.Worker.Tests/
│   ├── Cutube.Domain.Tests/
│   ├── Cutube.Core.Tests/
│   ├── Cutube.Infrastructure.Tests/
│   ├── Cutube.Cli.Tests/        # MOVED from Cutube.Tests/
│   └── Cutube.Web.E2E/        # MOVED from cutube-web/e2e/
│
├── docs/                        # Documentation
├── scripts/                     # Utility scripts
├── docker/                      # Docker configs
├── .specs/                      # Spec-driven dev docs
├── .github/                     # GitHub workflows
├── cutube.sln                  # .NET solution
├── docker-compose.yml
└── README.md
```

#### Benefits
- ✅ **Consistência**: Todos os projetos em `src/`
- ✅ **Clareza**: Separação clara entre fonte e testes
- ✅ **Escalabilidade**: Fácil adicionar novos projetos
- ✅ **Padrão .NET**: Segue convenção da comunidade

#### Changes Required
1. **Mover CLI**: `cutube/` → `src/Cutube.Cli/`
2. **Mover Web**: `cutube-web/` → `src/Cutube.Web/`
3. **Mover Testes CLI**: `Cutube.Tests/` → `tests/Cutube.Cli.Tests/`
4. **Mover Testes Web**: `cutube-web/e2e/` → `tests/Cutube.Web.E2E/`
5. **Atualizar solution**: Adicionar novos projetos
6. **Atualizar paths**: Imports relativos, Dockerfile, etc.

### Alternative Options (Not Recommended)

#### Option 2: Hybrid (Frontend Separate)
**Not recommended** due to inconsistency - if CLI is in `src/`, why not Web?

#### Option 3: Keep Current
**Not recommended** - maintains current inconsistencies and unclear structure.

## Benefits of Recommended Approach

| Aspect | Benefit |
|---------|----------|
| **Consistência** | ✅ All projects use PascalCase in `src/` |
| **Clareza** | ✅ Clear separation: source in `src/`, tests in `tests/` |
| **Escalabilidade** | ✅ Easy to add `Cutube.Mobile`, `Cutube.Desktop`, etc. |
| **Convenção .NET** | ✅ Follows community standards |
| **Compartilhamento** | ✅ Easy to share DTOs/types between projects |
| **Monorepo** | ✅ Single place for all source code |

## Recommendation

### ✅ **Option 1: Full Monorepo** (src/ tests/)

**This is the RECOMMENDED approach.**

#### Justification

1. **Projetos .NET**:
   - Comunidade usa `src/` e `tests/`
   - Visual Studio espera essa estrutura
   - Solução (.sln) fica limpa

2. **Monorepo benefits**:
   - Compartilhar tipos (DTOs, contracts)
   - Mudanças atômicas (API + Web)
   - CI/CD simplificado
   - Code review único

3. **Nome padrão**:
   - `Cutube.Cli` (PascalCase)
   - `Cutube.Web` (PascalCase)
   - Consistente com `Cutube.Api`, `Cutube.Worker`

4. **Testes organizados**:
   - Todos em `tests/`
   - `tests/Cutube.[Project].Tests`
   - Fácil adicionar `tests/Cutube.[Project].Integration.Tests`

#### Migration Plan

#### Phase 1: Prepare (No file moves)
1. Create `RESTRUCTURE.md` checklist
2. Create `scripts/migrate-structure.sh`
3. Communicate to team (if any)

#### Phase 2: Backend (src/)
1. Move `cutube/` → `src/Cutube.Cli/`
2. Create `src/Cutube.Core/` (empty)
3. Create `src/Cutube.Infrastructure/` (empty)
4. Create `src/Cutube.Application/` (empty)
5. Update solution file
6. Update project references

#### Phase 3: Tests (tests/)
1. Move `Cutube.Tests/` → `tests/Cutube.Cli.Tests/`
2. Verify all test projects in `tests/`
3. Update solution file
4. Run tests

#### Phase 4: Frontend (src/)
1. Move `cutube-web/` → `src/Cutube.Web/`
2. Update `package.json` (name: "cutube-web")
3. Update imports/paths
4. Update Dockerfile
5. Update CI/CD

#### Phase 5: Cleanup
1. Update README.md paths
2. Update .gitignore
3. Update documentation (.specs/)
4. Delete old directories
5. Commit & push

## Name Changes

### Current → Proposed

| Current | Proposed | Reason |
|---------|-----------|--------|
| `cutube/` | `src/Cutube.Cli/` | PascalCase, in src/ |
| `cutube-web/` | `src/Cutube.Web/` | PascalCase, in src/ |
| `Cutube.Tests/` | `tests/Cutube.Cli.Tests/` | In tests/, consistent |
| `cutube-web/e2e/` | `tests/Cutube.Web.E2E/` | In tests/, consistent |

### Benefits

1. **PascalCase**:
   - Consistente com convenção .NET
   - Fácil reconhecer como projeto
   - Alinha com `Cutube.Api`, `Cutube.Worker`

2. **In src/**:
   - Separação clara: código vs configuração
   - Padrão comunidade .NET
   - Visual Studio友好

3. **In tests/**:
   - Separação clara: produção vs teste
   - Padrão xUnit/NUnit
   - Fácil excluir do build

## Next Steps

### Immediate

1. **Decision**: Choose Option 1, 2, or 3
2. **Planning**: Create detailed migration checklist
3. **Backup**: Ensure git is clean, create backup branch

### If Option 1 Chosen

1. **Create migration script**:
   ```bash
   #!/bin/bash
   # scripts/migrate-structure.sh

   # Move CLI
   git mv cutube src/Cutube.Cli

   # Move Web
   git mv cutube-web src/Cutube.Web

   # Move CLI tests
   git mv Cutube.Tests tests/Cutube.Cli.Tests

   # Move E2E tests
   git mv src/Cutube.Web/e2e ../tests/Cutube.Web.E2E

   # Update solution
   dotnet sln cutube.sln add src/Cutube.Cli/Cutube.Cli.csproj
   ```

2. **Update project references**:
   - Remove: `<ProjectReference Include="..\..\cutube\cutube.csproj" />`
   - Add: `<ProjectReference Include="..\Cutube.Core\Cutube.Core.csproj" />`

3. **Update paths**:
   - Dockerfile: `COPY src/Cutube.Api ./api`
   - docker-compose.yml: Volumes, paths
   - CI/CD: Work directories, build paths

4. **Test thoroughly**:
   - Build: `dotnet build`
   - Tests: `dotnet test`
   - Run: API, Worker, CLI, Web

### If Option 2 Chosen

Similar steps, but:
- Move `cutube-web/` → `web/`
- Keep `web/e2e/` (or move to `web-tests/`)

### If Option 3 Chosen

No changes needed, but accept technical debt.

## Risks

### Option 1 Risks

| Risk | Probability | Impact | Mitigation |
|------|-------------|---------|------------|
| Broken references | High | High | Use `git mv` to preserve history |
| Broken Docker | Medium | High | Test containers after migration |
| Broken CI/CD | Medium | High | Update workflows, test in PR |
| Team confusion | Low | Medium | Communicate changes clearly |
| Merge conflicts | Medium | Low | Do migration in dedicated branch |

### Option 2 Risks

Similar to Option 1, but lower risk for Web (stays separate).

### Option 3 Risks

| Risk | Probability | Impact | Mitigation |
|------|-------------|---------|------------|
| Continue inconsistency | High | High | None (accepted debt) |
| New contributor confusion | High | Medium | Better onboarding docs |
| Scalability issues | Medium | Medium | Refactor later |

## Monorepo vs Multi-Repo

### Current State

**Already a monorepo!** Just poorly organized.

### Monorepo Benefits

1. **Single source of truth**
   - One git repository
   - One CI/CD pipeline
   - One place to look

2. **Shared code**
   - DTOs in `Cutube.Contracts`
   - Types between Web and API
   - Common utilities

3. **Atomic changes**
   - Change API contract
   - Update Web and CLI
   - Single PR

4. **Simplified tooling**
   - Single git repo
   - Single docker-compose
   - Single development environment

### Multi-Repo Trade-offs

| Aspect | Monorepo | Multi-Repo |
|---------|-----------|-------------|
| **Repository count** | 1 | N+ |
| **CI/CD** | Single pipeline | Multiple pipelines |
| **Shared code** | Easy | Requires packages |
| **Atomic changes** | Yes | No |
| **Access control** | File-based | Repo-based |
| **Clone time** | Longer | Shorter |
| **Build time** | Longer | Shorter |
| **Complexity** | Higher | Lower |

### Tools for Monorepos

1. **pnpm workspaces** (already using!)
   - Efficient dependency sharing
   - Strict dependency handling
   - `pnpm-workspace.yaml` exists

2. **Turborepo** (optional)
   - Build caching
   - Task orchestration
   - Remote caching

3. **Nx** (optional)
   - Smart caching
   - Affected graph
   - Code generation

### Recommendation

**Stick with monorepo** (already have one), just organize better.

## Final Recommendation

### ✅ **Choose Option 1: Full Monorepo**

### Structure

```
Cutube/
├── src/
│   ├── Cutube.Api/
│   ├── Cutube.Worker/
│   ├── Cutube.Domain/
│   ├── Cutube.Contracts/
│   ├── Cutube.Core/              # NEW
│   ├── Cutube.Infrastructure/     # NEW
│   ├── Cutube.Application/       # NEW
│   ├── Cutube.Cli/              # MOVED from cutube/
│   └── Cutube.Web/              # MOVED from cutube-web/
│
├── tests/
│   ├── Cutube.Api.Tests/
│   ├── Cutube.Worker.Tests/
│   ├── Cutube.Domain.Tests/
│   ├── Cutube.Core.Tests/
│   ├── Cutube.Infrastructure.Tests/
│   ├── Cutube.Cli.Tests/        # MOVED from Cutube.Tests/
│   └── Cutube.Web.E2E/         # MOVED from cutube-web/e2e/
│
├── docs/
├── scripts/
├── docker/
├── .specs/
├── .github/
├── cutube.sln
├── docker-compose.yml
└── README.md
```

### Migration Branch

```bash
git checkout -b feature/restructure-monorepo
# Execute migration
# Test everything
# Create PR
```

### Timeline

- **Planning**: 1 day
- **Migration**: 1 day
- **Testing**: 1-2 days
- **Documentation**: 1 day
- **Total**: ~1 week

## References

### .NET Project Structure
- [.NET Application Architecture](https://learn.microsoft.com/en-us/dotnet/architecture/modern-web-apps-choose-architecture)
- [Standard .NET Project Structure](https://github.com/dotnet/aspnetcore-api-codepattern/)

### Monorepo Best Practices
- [Google's monorepo](https://www.youtube.com/watch?v=wBWDkRDYTaY)
- [Facebook's monorepo](https://code.facebook.com/posts/1468550970090376/a-small-engineer-s-guide-to-monorepos/)
- [Turborepo Guide](https://turbo.build/repo/docs)

### Migration Guides
- [Git mv docs](https://git-scm.com/docs/git-mv)
- [dotnet sln](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-sln)
