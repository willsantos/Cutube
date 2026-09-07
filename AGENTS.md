# Agent Instructions

This project does **not** use beads (bd) or any local task-tracker. Task/issue
management:

- **Issues (Linear):** user-reported bugs and explicitly requested work —
  see "Linear Issue Management" below
- **Planning:** feature specs and plans live in the repository
  (`.specs/`, `plans/`) and follow the `spec-driven-dev` workflow when used

## Linear Issue Management

**⚠️ IMPORTANT:** Issues are tracked in **Linear**.

### Creating Issues

**Issues should ONLY be created when the user explicitly requests it.**

Do NOT automatically create issues for:
- Minor bugs found during development
- Edge cases discovered during testing
- Nice-to-have improvements
- Documentation updates

ONLY create issues when:
- User explicitly asks: "create an issue for this"
- User asks to track a non-impediment bug/improvement
- User requests work to be deferred/saved for later

### Linear Issue Guidelines

When creating issues in Linear for this project:

1. **ALWAYS associate with the "Cutube" project**
   - Use `project: "Cutube"` parameter
   - The Cutube project already exists in Linear workspace "Oroborus"
   - Project ID: `90c0cdae-e411-4fcd-80ce-eef703622671`
   - **⚠️ MANDATORY:** ALL Linear issues for this project MUST be associated with the Cutube project

2. **Team Assignment:**
   - Use team ID: `2e51306a-fedf-4c10-8f46-f65c808dff79` (Oroborus team)
   - Or use `team: "Oroborus"` parameter

3. **Issue structure:**
   - Clear, descriptive title
   - Detailed problem description
   - Proposed solution options (when applicable)
   - Impact analysis
   - Estimated effort

4. **Labels:**
   - Use appropriate labels (Bug, Feature, Improvement)
   - Set appropriate priority

Example:
```
linear_create_issue
  title="Clear title"
  description="Detailed description..."
  project="Cutube"
  team="2e51306a-fedf-4c10-8f46-f65c808dff79"
  priority=3
  labels=["Improvement"]
```

## Branch Strategy

This project uses **one branch per feature** with Conventional Commits.

**Primary branch:** `main`. Cutube is a standalone CLI — feature branches are
created from `main` and PRs target `main`. (Note: the `develop` branch still
exists only as a temporary historical reference during the Orotube migration
and will be deleted once the migration completes; do not base new work on it.
The pre-split distributed history also lives in `legacy/cutube-develop`.)

### Workflow

1. **Create feature branch:**
   ```bash
   git checkout -b feature/<name> main
   ```

2. **Implement feature:**
   - Write code
   - Write tests
   - Run tests: `dotnet test` ⚠️ **ALL tests MUST pass**
   - Run build: `dotnet build` ⚠️ **NO warnings allowed**

3. **Commit with conventional commit:**
   ```bash
   git add .
   git commit -m "feat: description"
   ```

4. **Push and create PR:**
   ```bash
   git push origin feature/<name>
   # Create PR on GitHub targeting main
   ```

5. **Merge and cleanup:**
   ```bash
   git checkout main
   git pull
   git branch -d feature/<name>
   ```

### Commit Types

- `feat:` - New feature
- `fix:` - Bug fix
- `refactor:` - Code refactoring
- `test:` - Tests
- `docs:` - Documentation
- `chore:` - Build/deps

## Quality Gates (Mandatory)

**Before marking a task as complete, ALL quality gates MUST pass:**

1. **Tests pass:** `dotnet test` ⚠️ **100% of tests MUST pass**
   - No failing tests allowed
   - No skipped tests without justification
   - Run before every commit

2. **Build succeeds:** `dotnet build` ⚠️ **NO warnings allowed**
   - Zero compiler warnings
   - Zero build errors
   - Clean build output

3. **Code review:**
   - Code follows project conventions
   - Tests cover new functionality
   - No hardcoded values or magic numbers

**⚠️ CRITICAL: A task is ONLY complete when:**
- All tests pass: `dotnet test` ✓
- Build succeeds with no warnings: `dotnet build` ✓
- Code committed with conventional commit ✓
- Changes pushed to remote: `git push` ✓

### Quality Gate Examples

```bash
# ❌ WRONG - Don't call work done without running tests
git commit -m "feat: done" && git push

# ✅ CORRECT - Run quality gates first
dotnet test                    # ALL tests must pass
dotnet build                   # NO warnings allowed
git add .
git commit -m "feat: add flexible time parsing"
git push
```

## Landing the Plane (Session Completion)

**When ending a work session**, you MUST complete ALL steps below. Work is NOT complete until `git push` succeeds.

**MANDATORY WORKFLOW:**

1. **File issues for remaining work** - Create issues for anything that needs follow-up
2. **Run quality gates** (if code changed) - Tests, linters, builds ⚠️ **REQUIRED**
   - `dotnet test` - ALL tests MUST pass (100%)
   - `dotnet build` - NO warnings allowed
3. **PUSH TO REMOTE** - This is MANDATORY:
   ```bash
   git pull --rebase
   git push
   git status  # MUST show "up to date with origin"
   ```
4. **Clean up** - Clear stashes, prune remote branches
5. **Verify** - All changes committed AND pushed
6. **Hand off** - Provide context for next session

**CRITICAL RULES:**
- Work is NOT complete until `git push` succeeds
- NEVER stop before pushing - that leaves work stranded locally
- NEVER say "ready to push when you are" - YOU must push
- If push fails, resolve and retry until it succeeds
