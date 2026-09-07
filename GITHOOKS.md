# Git Hooks

This repository uses git hooks for branch protection.

## Installed Hooks

The following hook is automatically installed in `.git/hooks/`:

- **pre-commit**:
  - **Branch protection**: Blocks direct commits to `main` and `develop` branches

**Protected branches:** `main` and `develop`

**Workflow:**
1. Create feature branch: `git checkout -b feature/my-feature`
2. Make commits
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
   ```

### Branch protection errors

If you see "Commit direto na branch 'develop' não é permitido!", it means you're trying to commit directly to a protected branch.

**Solution:**
```bash
git checkout -b feature/my-feature
# Make your commits
git push origin feature/my-feature
# Create PR on GitHub
```
