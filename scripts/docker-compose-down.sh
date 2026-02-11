#!/bin/bash
# scripts/docker-compose-down.sh
set -e

echo "🛑 Stopping RabbitMQ..."

docker-compose down

echo ""
echo "✅ RabbitMQ stopped!"
echo ""
echo "To remove volumes and clean up data, run:"
echo "  docker-compose down -v"
