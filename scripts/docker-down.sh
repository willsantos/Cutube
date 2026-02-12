#!/bin/bash
set -e

if docker compose version >/dev/null 2>&1; then
  COMPOSE_CMD="docker compose"
else
  COMPOSE_CMD="docker-compose"
fi

echo "Stopping Cutube stack..."
$COMPOSE_CMD down
echo "Done."
