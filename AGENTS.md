# Agent Instructions

This project uses **bd** (beads) for issue tracking. Run `bd onboard` to get started.

## Quick Reference

```bash
bd ready              # Find available work
bd show <id>          # View issue details
bd update <id> --status in_progress  # Claim work
bd close <id>         # Complete work
bd sync               # Sync with git
```

## Branch Strategy

This project uses **one branch per feature** with Conventional Commits.

### Workflow

1. **Create feature branch:**
   ```bash
   git checkout -b feature/<name>
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
   # Create PR on GitHub
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

### Examples

```bash
feat: add flexible time input parsing (1h30m, 90s, etc)
feat: add custom filename option
feat: add audio-only download with MP3 support
feat: add CTRL+C cancellation support
feat: add input validations
docs: update README with new features
test: add unit tests for TimeHelper
```

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
# ❌ WRONG - Don't close issue without running tests
bd close Cutube-gcb

# ✅ CORRECT - Run quality gates first
dotnet test                    # ALL tests must pass
dotnet build                   # NO warnings allowed
git add .
git commit -m "feat: add flexible time parsing"
git push
bd close Cutube-gcb            # NOW you can close
```

## Landing the Plane (Session Completion)

**When ending a work session**, you MUST complete ALL steps below. Work is NOT complete until `git push` succeeds.

**MANDATORY WORKFLOW:**

1. **File issues for remaining work** - Create issues for anything that needs follow-up
2. **Run quality gates** (if code changed) - Tests, linters, builds ⚠️ **REQUIRED**
   - `dotnet test` - ALL tests MUST pass (100%)
   - `dotnet build` - NO warnings allowed
3. **Update issue status** - Close finished work, update in-progress items ⚠️ **ONLY after tests pass**
4. **PUSH TO REMOTE** - This is MANDATORY:
   ```bash
   git pull --rebase
   bd sync
   git push
   git status  # MUST show "up to date with origin"
   ```
5. **Clean up** - Clear stashes, prune remote branches
6. **Verify** - All changes committed AND pushed
7. **Hand off** - Provide context for next session

**CRITICAL RULES:**
- Work is NOT complete until `git push` succeeds
- NEVER stop before pushing - that leaves work stranded locally
- NEVER say "ready to push when you are" - YOU must push
- If push fails, resolve and retry until it succeeds

