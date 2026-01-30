# Git Hooks

This repository uses **bd (beads)** for issue tracking and git hooks for automatic synchronization.

## Installed Hooks

The following hooks are automatically installed in `.git/hooks/`:

- **pre-commit**:
  - **Branch protection**: Blocks direct commits to `main` and `develop` branches
  - **BD sync**: Exports bd database to JSONL before commit (only on feature branches)
- **post-merge**: Imports JSONL to bd database after pull/merge
- **post-checkout**: Imports JSONL to bd database after branch checkout

**Protected branches:** `main` and `develop`

**Workflow:**
1. Create feature branch: `git checkout -b feature/my-feature`
2. Make commits (bd sync exports to JSONL automatically)
3. Push to feature branch: `git push origin feature/my-feature`
4. Create Pull Request
5. Merge via GitHub interface

## Hook Maintenance

Hooks are stored in `.githooks/` directory (versioned) and installed to `.git/hooks/` (local).

### Installing Hooks

After cloning the repository or when hooks need to be refreshed:

```bash
./install-hooks.sh
```

### Manual Installation

If the script doesn't work, copy hooks manually:

```bash
cp .githooks/* .git/hooks/
chmod +x .git/hooks/*
```

## Troubleshooting

### Hooks not executing

1. Check hook permissions:
   ```bash
   ls -la .git/hooks/
   ```

2. Ensure hooks are executable:
   ```bash
   chmod +x .git/hooks/pre-*
   chmod +x .git/hooks/post-*
   ```

3. Verify bd is installed:
   ```bash
   which bd
   ```

### Hook errors

If you see "Unknown hook" errors, it means a hook is calling an unsupported bd command. The supported hooks are:
- `bd hook pre-commit`
- `bd hook post-merge`
- `bd hook post-checkout`

### Branch protection errors

If you see "Commit direto na branch 'develop' não é permitido!", it means you're trying to commit directly to a protected branch.

**Solution:**
```bash
git checkout -b feature/my-feature
# Make your commits
git push origin feature/my-feature
# Create PR on GitHub
```

The `prepare-commit-msg` hook is **NOT supported** by bd and has been removed.

## BD Sync Status

Check current sync status:

```bash
bd sync --status
```

Manual sync:

```bash
bd sync
```
