# RabbitMQ Setup - Cutube

## Requisitos

- Docker e Docker Compose instalados
- .NET 10 SDK

## Início Rápido

### 1. Iniciar RabbitMQ

```bash
# Usar o script helper
./scripts/docker-compose-up.sh

# Ou manualmente
docker-compose up -d rabbitmq
```

### 2. Verificar Status

```bash
# Ver logs
docker-compose logs -f rabbitmq

# Ver health check
docker-compose exec rabbitmq rabbitmq-diagnostics check_running
```

### 3. Acessar Management UI

Abra o navegador em: http://localhost:15672

- **User:** cutube
- **Pass:** cutube123

### 4. Parar RabbitMQ

```bash
./scripts/docker-compose-down.sh

# Ou manualmente
docker-compose down
```

## Troubleshooting

### Portas já em uso

```bash
# Verificar se as portas estão ocupadas
lsof -i :5672
lsof -i :15672

# Mudar as portas no docker-compose.yml se necessário
```

### Container não inicia

```bash
# Ver logs do container
docker-compose logs rabbitmq

# Remover e recriar
docker-compose down -v
docker-compose up -d
```

### Reset completo

```bash
# Parar e remover volumes
docker-compose down -v

# Limpar dados do RabbitMQ
docker volume rm cutube_rabbitmq_data

# Reiniciar
docker-compose up -d
```

## Configuração Avançada

### Alterar credenciais

Edite o arquivo `.env`:

```bash
RABBITMQ_USER=seu_usuario
RABBITMQ_PASS=sua_senha_segura
```

### Persistência de dados

Os dados são persistidos no volume `rabbitmq_data`. Para fazer backup:

```bash
docker run --rm -v cutube_rabbitmq_data:/data -v $(pwd):/backup \
  ubuntu tar czf /backup/rabbitmq-backup.tar.gz /data
```

## Health Check Endpoint

```bash
curl http://localhost:15672/api/overview
# User: cutube
# Pass: cutube123
```

## Topologia RabbitMQ

### Exchanges e Queues

| Nome | Tipo | Propósito |
|------|------|-----------|
| **cutube.direct** | direct | Exchange principal para downloads |
| **cutube.dlq** | direct | Dead Letter Exchange para erros |
| **cutube.downloads** | queue | Fila principal de downloads |
| **cutube.downloads.dlq** | queue | Fila de mensagens com erro |

### Bindings

```
cutube.direct (routing key: "download") → cutube.downloads
cutube.direct (routing key: "download.dlq") → cutube.downloads.dlq
```

## Worker Service

O projeto `Cutube.Worker` é um Worker Service .NET que:

- Consome mensagens da fila `cutube.downloads`
- Processa downloads de forma assíncrona
- Envia status de progresso via WebSocket
- Publica eventos de conclusão

### Executar Worker

```bash
cd src/Cutube.Worker
dotnet run
```

### Configuração

Edite `appsettings.json`:

```json
{
  "RabbitMq": {
    "Host": "localhost",
    "Port": 5672,
    "VirtualHost": "/",
    "UserName": "cutube",
    "Password": "cutube123"
  }
}
```

## Próximos Passos

Após configurar RabbitMQ:
1. Implementar Producer na API (Fase 3.2)
2. Implementar Consumer no Worker (Fase 3.3)
3. Configurar Status Tracking (Fase 3.4)
