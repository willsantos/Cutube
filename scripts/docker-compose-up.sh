#!/bin/bash
# scripts/docker-compose-up.sh
set -e

echo "🚀 Starting RabbitMQ..."

# Check if .env exists, create if not
if [ ! -f .env ]; then
    cat > .env << EOF
RABBITMQ_USER=cutube
RABBITMQ_PASS=cutube123
EOF
    echo "✅ Created .env file with default credentials"
fi

# Start RabbitMQ
docker-compose up -d rabbitmq

echo "✅ RabbitMQ started!"
echo ""
echo "Management UI: http://localhost:15672"
echo "User: ${RABBITMQ_USER:-cutube}"
echo "Pass: ${RABBITMQ_PASS:-cutube123}"
echo ""
echo "Waiting for RabbitMQ to be ready..."
sleep 10

# Check if RabbitMQ is responding
docker-compose exec -T rabbitmq rabbitmq-diagnostics check_running

echo ""
echo "✅ RabbitMQ is ready!"
