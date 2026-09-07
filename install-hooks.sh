#!/bin/bash
# Install git hooks from .githooks directory to .git/hooks

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
HOOKS_DIR="$SCRIPT_DIR/.githooks"
GIT_HOOKS_DIR=".git/hooks"

echo "Installing git hooks from $HOOKS_DIR..."

# Check if hooks directory exists
if [ ! -d "$HOOKS_DIR" ]; then
    echo "Error: .githooks directory not found"
    exit 1
fi

# Copy all hooks
for hook in "$HOOKS_DIR"/*; do
    hook_name=$(basename "$hook")
    echo "Installing $hook_name..."
    cp "$hook" "$GIT_HOOKS_DIR/$hook_name"
    chmod +x "$GIT_HOOKS_DIR/$hook_name"
done

echo "✓ Git hooks installed successfully"
echo ""
echo "Installed hooks:"
ls -1 "$GIT_HOOKS_DIR" | grep -E "(pre-commit|pre-push|post-merge|post-checkout)" || true
