#!/bin/bash
# scripts/dev.sh
set -e

echo "🚀 Starting Cutube development environment..."

# Start infrastructure
echo "📦 Starting infrastructure (RabbitMQ)..."
docker-compose -f docker-compose.yml up -d rabbitmq

# Wait for infrastructure
echo "⏳ Waiting for infrastructure to be ready..."
sleep 5

# Install dependencies
echo "📦 Installing dependencies..."
pnpm install

# Start all services via Turborepo
echo "🔧 Starting all services..."
pnpm dev
