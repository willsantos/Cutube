# Fase 3.1: RabbitMQ Setup & Configuration

**Status:** 🎯 Planejamento
**Épico:** Cutube-858 (Épico 3: Integração com RabbitMQ)
**Duração:** 2 dias
**Responsável:** Backend Developer
**Prioridade:** 🔥 Alta
**Dependência:** ✅ Épico 2 completo

---

## Objetivo

Configurar RabbitMQ como message broker para o sistema distribuído do Cutube. Esta fase estabelece a infraestrutura de filas necessária para o processamento assíncrono de downloads.

**Benefícios:**
- Desacoplamento entre API (producer) e processamento (consumer)
- Escalabilidade horizontal (múltiplos workers)
- Tolerância a falhas com DLQ (Dead Letter Queue)
- Retry automático com backoff exponencial

---

## Visão Arquitetural

### Topologia RabbitMQ

```
┌─────────────────────────────────────────────────────────────────┐
│                         RabbitMQ                                │
│                                                                  │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │              Exchange: cutube.direct                      │  │
│  │         (direct exchange - routing key exact match)      │  │
│  └────────────────────┬─────────────────────────────────────┘  │
│                       │                                          │
│       ┌───────────────┼───────────────┐                       │
│       │               │               │                       │
│       ▼               ▼               ▼                       │
│  ┌─────────┐    ┌──────────┐   ┌──────────┐                  │
│  │ Queue:  │    │ Queue:   │   │ Queue:   │                  │
│  │cutube.  │    │ cutube.  │   │ cutube.  │                  │
│  │downloads│    │downloads │   │downloads │                  │
│  │         │    │.dlq      │   │.priority │                  │
│  └────┬────┘    └────┬─────┘   └────┬─────┘                  │
│       │              │              │                          │
│       │              │              │                          │
└───────┼──────────────┼──────────────┼──────────────────────────┘
        │              │              │
        ▼              ▼              ▼
   ┌─────────┐    ┌──────────┐   ┌──────────┐
   │ Worker  │    │ DLQ      │   │ Future:  │
   │consume  │    │ Handler  │   │ Priority │
   └─────────┘    └──────────┘   └──────────┘
```

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

DLQ Configuration:
- x-dead-letter-exchange: cutube.direct
- x-dead-letter-routing-key: download
```

---

## Tarefas

### 3.1.1 Criar Docker Compose para RabbitMQ

**Estimativa:** 3 horas
**Arquivos:**
```
docker-compose.yml
scripts/
  ├── docker-compose-up.sh
  └── docker-compose-down.sh
```

**Implementação:**

```yaml
# docker-compose.yml
version: '3.8'

services:
  rabbitmq:
    image: rabbitmq:4-management
    container_name: cutube-rabbitmq
    hostname: rabbitmq
    ports:
      - "5672:5672"   # AMQP protocol
      - "15672:15672" # Management UI
    environment:
      RABBITMQ_DEFAULT_USER: ${RABBITMQ_USER:-cutube}
      RABBITMQ_DEFAULT_PASS: ${RABBITMQ_PASS:-cutube123}
      RABBITMQ_DEFAULT_VHOST: "/"
    volumes:
      - rabbitmq_data:/var/lib/rabbitmq
      - ./docker/rabbitmq/enabled_plugins:/etc/rabbitmq/enabled_plugins
    healthcheck:
      test: ["CMD", "rabbitmq-diagnostics", "check_running"]
      interval: 30s
      timeout: 10s
      retries: 3
    networks:
      - cutube-network

volumes:
  rabbitmq_data:
    driver: local

networks:
  cutube-network:
    driver: bridge
```

**Scripts:**

```bash
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
fi

# Start RabbitMQ
docker-compose up -d rabbitmq

echo "✅ RabbitMQ started!"
echo ""
echo "Management UI: http://localhost:15672"
echo "User: cutube"
echo "Pass: cutube123"
echo ""
echo "Waiting for RabbitMQ to be ready..."
sleep 10

# Check if RabbitMQ is responding
docker-compose exec -T rabbitmq rabbitmq-diagnostics check_running

echo "✅ RabbitMQ is ready!"
```

**Checklist:**
- [ ] Criar docker-compose.yml na raiz
- [ ] Configurar ports 5672 (AMQP) e 15672 (Management UI)
- [ ] Adicionar variáveis de ambiente para user/password
- [ ] Configurar volume para persistência
- [ ] Adicionar health check
- [ ] Criar scripts docker-compose-up.sh e docker-compose-down.sh
- [ ] Criar arquivo .env.example com credentials
- [ ] Adicionar .env ao .gitignore

**Critérios de aceito:**
- ✅ `docker-compose up -d` inicia RabbitMQ
- ✅ Management UI acessível em http://localhost:15672
- ✅ Dados persistem após container restart
- ✅ Health check funciona corretamente
- ✅ User/password configuráveis via .env

---

### 3.1.2 Configurar Exchanges e Queues

**Estimativa:** 5 horas
**Arquivos:**
```
src/Cutube.Worker/  (novo projeto)
  Configuration/
    ├── RabbitMqOptions.cs
    ├── RabbitMqConfig.cs
    └── QueueDeclarationService.cs
  Extensions/
    └── ServiceCollectionExtensions.cs
```

**Pacotes NuGet:**
```xml
<PackageReference Include="MassTransit.RabbitMQ" Version="8.3.0" />
<PackageReference Include="Microsoft.Extensions.Diagnostics.HealthChecks" Version="10.0.0" />
<PackageReference Include="Microsoft.Extensions.Options.ConfigurationExtensions" Version="10.0.0" />
```

**Implementação:**

```csharp
// src/Cutube.Worker/Configuration/RabbitMqOptions.cs
namespace Cutube.Worker.Configuration;

public class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    /// <summary>
    /// RabbitMQ host (default: localhost)
    /// </summary>
    public string Host { get; set; } = "localhost";

    /// <summary>
    /// AMQP port (default: 5672)
    /// </summary>
    public int Port { get; set; } = 5672;

    /// <summary>
    /// Virtual host (default: /)
    /// </summary>
    public string VirtualHost { get; set; } = "/";

    /// <summary>
    /// Username for authentication
    /// </summary>
    public string UserName { get; set; } = "cutube";

    /// <summary>
    /// Password for authentication
    /// </summary>
    public string Password { get; set; } = "cutube123";

    /// <summary>
    /// Retry count for connection attempts
    /// </summary>
    public int RetryCount { get; set; } = 5;

    /// <summary>
    /// Connection timeout in seconds
    /// </summary>
    public int Timeout { get; set; } = 30;
}
```

```csharp
// src/Cutube.Worker/Configuration/RabbitMqConfig.cs
namespace Cutube.Worker.Configuration;

public static class RabbitMqConfig
{
    public const string MainExchange = "cutube.direct";
    public const string DlqExchange = "cutube.dlq";

    public const string DownloadsQueue = "cutube.downloads";
    public const string DownloadsDlqQueue = "cutube.downloads.dlq";

    public const string DownloadRoutingKey = "download";
    public const string DlqRoutingKey = "download.dlq";

    /// <summary>
    /// Queue TTL para mensagens na DLQ (7 dias)
    /// </summary>
    public const long DlqMessageTtlMs = 7 * 24 * 60 * 60 * 1000;

    /// <summary>
    /// Prefetch count (max unacked messages per consumer)
    /// </summary>
    public const ushort PrefetchCount = 3;
}
```

```csharp
// src/Cutube.Worker/Configuration/QueueDeclarationService.cs
using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cutube.Worker.Configuration;

public class QueueDeclarationService
{
    private readonly RabbitMqOptions _options;
    private readonly ILogger<QueueDeclarationService> _logger;

    public QueueDeclarationService(
        IOptions<RabbitMqOptions> options,
        ILogger<QueueDeclarationService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// Configura MassTransit com exchanges, queues e bindings
    /// </summary>
    public void ConfigureMassTransit(IBusRegistrationConfigurator configurator)
    {
        configurator.UsingRabbitMq((context, cfg) =>
        {
            cfg.Host(_options.Host, _options.Port, _options.VirtualHost, h =>
            {
                h.Username(_options.UserName);
                h.Password(_options.Password);
            });

            // Configure retry for connection issues
            cfg.UseMessageRetry(r =>
            {
                r.Incremental(3, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(30));
            });

            // Configure prefetch count
            cfg.PrefetchCount = RabbitMqConfig.PrefetchCount;

            // Configure exchanges and queues
            cfg.ConfigureEndpoints(context);
        });
    }

    /// <summary>
    /// Declara a fila principal com DLQ configuration
    /// </summary>
    public static void ConfigureDownloadsQueue(IReceiveEndpointConfigurator endpoint)
    {
        endpoint.ConfigureConsumeTopology = false;

        // Configurar DLQ
        endpoint.SetQuorumQueue();

        // Dead letter configuration
        endpoint.DeadLetterExchange = RabbitMqConfig.MainExchange;
        endpoint.DeadLetterRoutingKey = RabbitMqConfig.DlqRoutingKey;
    }
}
```

**Configuração no Program.cs (Worker):**

```csharp
// src/Cutube.Worker/Program.cs
using Cutube.Worker.Configuration;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

var builder = Host.CreateApplicationBuilder(args);

// Configuration
builder.Services.Configure<RabbitMqOptions>(
    builder.Configuration.GetSection(RabbitMqOptions.SectionName)
);

// MassTransit
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<DownloadConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        var options = context.GetRequiredService<IOptions<RabbitMqOptions>>().Value;

        cfg.Host(options.Host, options.Port, options.VirtualHost, h =>
        {
            h.Username(options.UserName);
            h.Password(options.Password);
        });

        cfg.ConfigureEndpoints(context);
    });
});

// Health checks
builder.Services.AddHealthChecks()
    .AddRabbitMQ(checkConnectionString: true);

var host = builder.Build();
host.Run();
```

**appsettings.json:**

```json
{
  "RabbitMq": {
    "Host": "localhost",
    "Port": 5672,
    "VirtualHost": "/",
    "UserName": "cutube",
    "Password": "cutube123",
    "RetryCount": 5,
    "Timeout": 30
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "MassTransit": "Debug"
    }
  }
}
```

**Checklist:**
- [ ] Criar projeto Cutube.Worker (Worker Service)
- [ ] Adicionar pacote MassTransit.RabbitMQ
- [ ] Criar RabbitMqOptions com configurações
- [ ] Criar RabbitMqConfig com constantes
- [ ] Implementar QueueDeclarationService
- [ ] Configurar exchanges: cutube.direct, cutube.dlq
- [ ] Configurar queues: cutube.downloads, cutube.downloads.dlq
- [ ] Configurar bindings entre exchanges e queues
- [ ] Implementar health check para RabbitMQ
- [ ] Configurar DLQ com TTL de 7 dias

**Critérios de aceito:**
- ✅ Exchanges e queues são criados automaticamente no startup
- ✅ Health check retorna healthy se RabbitMQ está conectado
- ✅ DLQ configurada corretamente
- ✅ Prefetch count configurado (max 3 mensagens simultâneas)

---

### 3.1.3 Documentar Setup Local

**Estimativa:** 2 horas
**Arquivos:**
```
docs/
  └── setup-rabbitmq.md
README.md (atualizar)
```

**Conteúdo do documento:**

```markdown
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

## Próximos Passos

Após configurar RabbitMQ:
1. Implementar Producer na API (Fase 3.2)
2. Implementar Consumer no Worker (Fase 3.3)
3. Configurar Status Tracking (Fase 3.4)
```

**Checklist:**
- [ ] Criar docs/setup-rabbitmq.md
- [ ] Documentar como iniciar RabbitMQ via Docker
- [ ] Documentar como acessar Management UI
- [ ] Adicionar troubleshooting comum
- [ ] Atualizar README principal com link para documentação
- [ ] Adicionar diagramas da arquitetura

**Critérios de aceito:**
- ✅ README claro e completo
- ✅ Troubleshooting cobre cenários comuns
- ✅ Diagramas ilustram a topologia
- ✅ Comandos de exemplo funcionais

---

## Qualidade Gates - Fase 3.1

**ANTES de considerar esta fase completa, TODOS os itens abaixo devem ser concluídos:**

- [ ] **docker-compose up -d** inicia RabbitMQ sem erros
- [ ] Management UI acessível em localhost:15672
- [ ] Health check (`/health`) retorna healthy
- [ ] Exchanges e queues criados automaticamente
- [ ] DLQ configurada corretamente
- [ ] Documentação completa em docs/setup-rabbitmq.md
- [ ] README atualizado
- [ ] Testes manuais executados

---

## Cronograma Detalhado

| Tarefa | Estimativa | Dependencies | Blocker |
|--------|-----------|--------------|---------|
| 3.1.1 Docker Compose | 3h | - | Não |
| 3.1.2 Exchanges/Queues | 5h | 3.1.1 | **Sim** |
| 3.1.3 Documentação | 2h | 3.1.1, 3.1.2 | **Sim** |

**Total:** 10 horas (~2 dias)

---

## Tecnologias

- **RabbitMQ 4** - Message broker
- **MassTransit 8** - Abstração para RabbitMQ
- **Docker Compose** - Orquestração de containers
- **.NET 10** - Worker Service
- **Serilog** - Logging estruturado

---

## Riscos e Mitigações

| Risco | Impacto | Mitigação |
|-------|---------|-----------|
| Docker não instalado | Alto | Documentar instalação para Windows/Mac/Linux |
| Portas conflitando | Médio | Permitir customização via .env |
| RabbitMQ crash perdendo dados | Alto | Volume persistente configurado |
| Dificuldade de debug | Baixo | Management UI exposta |

---

## Próximos Passos

Após completar Fase 3.1:

1. **Fase 3.2: Producer** - Implementar publicação de mensagens
2. Criar projeto Cutube.Worker se ainda não existe
3. Implementar RabbitMqProducer na API
4. Integrar Producer com endpoint POST /api/downloads

---

## Deliverables

- [x] Docker Compose configurado
- [ ] Worker Service criado
- [ ] Exchanges/Queues configurados
- [ ] Health check implementado
- [ ] Documentação completa
- [ ] README atualizado

---

**Fim do Plano - Fase 3.1**
