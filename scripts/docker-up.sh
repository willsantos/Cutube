#!/bin/bash
set -e

GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

echo -e "${GREEN}Starting Cutube stack...${NC}"

if [ ! -f .env ]; then
  echo -e "${YELLOW}Creating .env from .env.example...${NC}"
  cp .env.example .env
fi

mkdir -p logs/api logs/worker downloads

if docker compose version >/dev/null 2>&1; then
  COMPOSE_CMD="docker compose"
else
  COMPOSE_CMD="docker-compose"
fi

echo -e "${GREEN}Pulling images...${NC}"
$COMPOSE_CMD pull

echo -e "${GREEN}Starting services...${NC}"
$COMPOSE_CMD up -d

echo
echo -e "${GREEN}Cutube is running.${NC}"
echo
echo "Available URLs:"
echo "  - Web:           http://localhost:4000"
echo "  - API:           http://localhost:5000"
echo "  - RabbitMQ UI:   http://localhost:15672"
echo
