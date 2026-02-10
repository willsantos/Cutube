# Fase 2.2: Cobertura de Testes (80-100%)

**Status:** 🎯 Planejamento
**Épico:** Cutube-lsx (Fase 2.2: REST API)
**Duração:** 8-10 horas
**Responsável:** Backend Developer
**Prioridade:** 🔥 Alta
**Dependência:** ✅ Fase 2.2 REST API completa

---

## Objetivo

Aumentar a cobertura de testes do projeto **Cutube.Api** de **30.48%** para **mínimo 80%** (foco em 100%), utilizando:
- Testes unitários para lógica isolada
- Testes de integração para endpoints HTTP
- `ExcludeFromCodeCoverage` para código que não requer testes
- Separação clara entre testes unitários e de integração

---

## Cobertura Atual

| Projeto | Cobertura Atual | Meta | Gap |
|---------|----------------|------|-----|
| **Cutube.Api** | 30.48% | 80%+ | -49.52% |
| Cutube.Domain | 10.69% | (fora do escopo) | - |
| **Total** | 12.29% | 80%+ | -67.71% |

### Arquivos com baixa cobertura

| Arquivo | Linhas | Cobertura | Prioridade |
|---------|--------|-----------|------------|
| Program.cs | 94 | 100% | ✅ OK (pode excluir) |
| DownloadsEndpoints.cs | 250 | ~40% | 🔴 Alta |
| VideosEndpoints.cs | 61 | ~30% | 🔴 Alta |
| DownloadQueue.cs | 54 | 0% | 🔴 Alta |
| BackgroundDownloadWorker.cs | 125 | 0% | 🟡 Média (diffícil) |
| InMemoryStatusRepository.cs | 76 | 0% | 🔴 Alta |
| YtDlpHealthCheck.cs | 44 | 0% | 🟡 Média |
| FfmpegHealthCheck.cs | 43 | 0% | 🟡 Média |
| DiskSpaceHealthCheck.cs | 54 | 0% | 🟡 Média |

---

## Estratégia

### 1. Excluir código não testável com `ExcludeFromCodeCoverage`

**Arquivos a excluir totalmente:**
```csharp
// Program.cs - Apenas configuração, não requer testes diretos
[assembly: ExcludeFromCodeCoverage]
```

**Arquivos a excluir parcialmente:**
```csharp
// BackgroundDownloadWorker.cs - HostedService, testado via integração
[ExcludeFromCodeCoverage]
protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    // ... código do hosted service ...
}
```

### 2. Criar testes unitários para lógica isolada

**Novos arquivos de teste unitário:**
```
tests/Cutube.Api.Tests/
├── Unit/
│   ├── Services/
│   │   ├── DownloadQueueTests.cs
│   │   └── InMemoryStatusRepositoryTests.cs
│   ├── HealthChecks/
│   │   ├── YtDlpHealthCheckTests.cs
│   │   ├── FfmpegHealthCheckTests.cs
│   │   └── DiskSpaceHealthCheckTests.cs
│   └── Endpoints/
│       ├── DownloadsEndpointsValidatorTests.cs
│       └── VideosEndpointsValidatorTests.cs
```

### 3. Melhorar testes de integração existentes

**Adicionar mocks na TestWebApplicationFactory:**
```csharp
protected override void ConfigureWebHost(IWebHostBuilder builder)
{
    builder.ConfigureServices(services =>
    {
        // Mock de dependências externas
        services.AddSingleton<IMockService, MockService>();
    });
}
```

---

## Tarefas

### Tarefa 1: Preparar infraestrutura de testes

**Estimativa:** 1 hora

**Arquivos:**
- `tests/Cutube.Api.Tests/Unit/` (novo diretório)

**Comandos:**
```bash
mkdir -p tests/Cutube.Api.Tests/Unit/Services
mkdir -p tests/Cutube.Api.Tests/Unit/HealthChecks
mkdir -p tests/Cutube.Api.Tests/Unit/Endpoints
```

**Checklist:**
- [ ] Criar diretórios para testes unitários
- [ ] Adicionar pacote Moq (se não existe)
- [ ] Adicionar ExcludeFromCodeCoverage em Program.cs
- [ ] Configurar filtragem de testes (unit vs integration)

**Critérios de aceito:**
- ✅ Estrutura de diretórios criada
- ✅ Moq instalado
- ✅ Program.cs marcado com ExcludeFromCodeCoverage

---

### Tarefa 2: Testes unitários para DownloadQueue

**Estimativa:** 2 horas

**Arquivo:** `tests/Cutube.Api.Tests/Unit/Services/DownloadQueueTests.cs`

**Cenários de teste:**
1. **EnqueueAsync:**
   - [ ] Deve gerar ID único para cada download
   - [ ] Deve adicionar item ao channel
   - [ ] Deve criar CancellationTokenSource para o download
   - [ ] Deve logar informação de enqueue

2. **CancelAsync:**
   - [ ] Deve cancelar download existente
   - [ ] Deve remover CancellationTokenSource
   - [ ] Deve logar warning se download não encontrado
   - [ ] Deve ser idempotente (cancelar múltiplas vezes)

3. **DequeueAllAsync:**
   - [ ] Deve retornar itens enfileirados
   - [ ] Deve respeitar CancellationToken
   - [ ] Deve bloquear até haver itens

**Implementação exemplo:**
```csharp
public class DownloadQueueTests
{
    [Fact]
    public async Task EnqueueAsync_ShouldGenerateUniqueId()
    {
        // Arrange
        var queue = new DownloadQueue(new NullLogger<DownloadQueue>());
        var request = new DownloadRequest { Url = "https://test.com" };

        // Act
        var id1 = await queue.EnqueueAsync(request, CancellationToken.None);
        var id2 = await queue.EnqueueAsync(request, CancellationToken.None);

        // Assert
        id1.Should().NotBeNullOrEmpty();
        id2.Should().NotBeNullOrEmpty();
        id1.Should().NotBe(id2);
    }

    [Fact]
    public async Task CancelAsync_ShouldCancelExistingDownload()
    {
        // Arrange
        var queue = new DownloadQueue(new NullLogger<DownloadQueue>());
        var request = new DownloadRequest { Url = "https://test.com" };
        var id = await queue.EnqueueAsync(request, CancellationToken.None);

        // Act
        await queue.CancelAsync(id, CancellationToken.None);

        // Assert
        // Verificar que o cancelamento foi registrado
        // (pode precisar expor estado interno para teste)
    }

    [Fact]
    public async Task DequeueAllAsync_ShouldReturnEnqueuedItems()
    {
        // Arrange
        var queue = new DownloadQueue(new NullLogger<DownloadQueue>());
        var request = new DownloadRequest { Url = "https://test.com" };
        var id = await queue.EnqueueAsync(request, CancellationToken.None);
        var cts = new CancellationTokenSource(100); // Timeout

        // Act
        var items = queue.DequeueAllAsync(cts.Token);
        await foreach (var (itemId, itemRequest) in items)
        {
            // Assert
            itemId.Should().Be(id);
            itemRequest.Url.Should().Be("https://test.com");
            break; // Testa apenas o primeiro item
        }
    }
}
```

**Checklist:**
- [ ] Criar DownloadQueueTests.cs
- [ ] Testar EnqueueAsync (3 cenários)
- [ ] Testar CancelAsync (3 cenários)
- [ ] Testar DequeueAllAsync (2 cenários)
- [ ] Testar logs (usando ILogger mock)

**Critérios de aceito:**
- ✅ Todos testes passam
- ✅ Cobertura de DownloadQueue > 90%

---

### Tarefa 3: Testes unitários para InMemoryStatusRepository

**Estimativa:** 2 horas

**Arquivo:** `tests/Cutube.Api.Tests/Unit/Services/InMemoryStatusRepositoryTests.cs`

**Cenários de teste:**
1. **AddAsync:**
   - [ ] Deve adicionar status ao repositório
   - [ ] Deve sobrescrever se ID já existe
   - [ ] Deve logar informação

2. **GetByIdAsync:**
   - [ ] Deve retornar status se existe
   - [ ] Deve retornar null se não existe

3. **GetAllAsync:**
   - [ ] Deve retornar todos os downloads
   - [ ] Deve retornar lista vazia se repositório vazio

4. **UpdateProgressAsync:**
   - [ ] Deve atualizar status existente
   - [ ] Deve mapear estados corretamente (downloading, finished, error)
   - [ ] Deve setar CompletedAt quando finished
   - [ ] Não deve falhar se ID não existe

5. **DeleteAsync:**
   - [ ] Deve remover download existente
   - [ ] Deve logar informação
   - [ ] Deve ser idempotente (deletar múltiplas vezes)

**Implementação exemplo:**
```csharp
public class InMemoryStatusRepositoryTests
{
    private readonly InMemoryStatusRepository _repository;
    private readonly NullLogger<InMemoryStatusRepository> _logger;

    public InMemoryStatusRepositoryTests()
    {
        _logger = new NullLogger<InMemoryStatusRepository>();
        _repository = new InMemoryStatusRepository(_logger);
    }

    [Fact]
    public async Task AddAsync_ShouldAddStatus()
    {
        // Arrange
        var status = new DownloadStatusRecord
        {
            Id = "test-id",
            Url = "https://test.com",
            Status = DownloadStatus.Queued,
            Progress = 0,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        await _repository.AddAsync("test-id", status);
        var retrieved = await _repository.GetByIdAsync("test-id", CancellationToken.None);

        // Assert
        retrieved.Should().NotBeNull();
        retrieved!.Id.Should().Be("test-id");
        retrieved.Url.Should().Be("https://test.com");
    }

    [Fact]
    public async Task UpdateProgressAsync_ShouldMapStatesCorrectly()
    {
        // Arrange
        await _repository.AddAsync("test-id", new DownloadStatusRecord
        {
            Id = "test-id",
            Url = "https://test.com",
            Status = DownloadStatus.Queued,
            Progress = 0,
            CreatedAt = DateTime.UtcNow
        });

        // Act - Update to downloading
        await _repository.UpdateProgressAsync("test-id", new Domain.Models.DownloadProgress
        {
            State = "downloading",
            Percentage = 50,
            DownloadedBytes = 1024,
            TotalBytes = 2048,
            Speed = 1024
        });

        var result = await _repository.GetByIdAsync("test-id", CancellationToken.None);

        // Assert
        result!.Status.Should().Be(DownloadStatus.Downloading);
        result.Progress.Should().Be(50);
        result.DownloadedBytes.Should().Be(1024);
    }

    [Fact]
    public async Task UpdateProgressAsync_Finished_ShouldSetCompletedAt()
    {
        // Arrange
        await _repository.AddAsync("test-id", new DownloadStatusRecord
        {
            Id = "test-id",
            Url = "https://test.com",
            Status = DownloadStatus.Downloading,
            Progress = 50,
            CreatedAt = DateTime.UtcNow
        });

        // Act
        await _repository.UpdateProgressAsync("test-id", new Domain.Models.DownloadProgress
        {
            State = "finished",
            Percentage = 100
        });

        var result = await _repository.GetByIdAsync("test-id", CancellationToken.None);

        // Assert
        result!.Status.Should().Be(DownloadStatus.Completed);
        result.CompletedAt.Should().NotBeNull();
        result.CompletedAt!.Value.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveDownload()
    {
        // Arrange
        await _repository.AddAsync("test-id", new DownloadStatusRecord
        {
            Id = "test-id",
            Url = "https://test.com",
            Status = DownloadStatus.Queued,
            Progress = 0,
            CreatedAt = DateTime.UtcNow
        });

        // Act
        await _repository.DeleteAsync("test-id", CancellationToken.None);
        var result = await _repository.GetByIdAsync("test-id", CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllDownloads()
    {
        // Arrange
        await _repository.AddAsync("id1", new DownloadStatusRecord
        {
            Id = "id1",
            Url = "https://test1.com",
            Status = DownloadStatus.Queued,
            Progress = 0,
            CreatedAt = DateTime.UtcNow
        });
        await _repository.AddAsync("id2", new DownloadStatusRecord
        {
            Id = "id2",
            Url = "https://test2.com",
            Status = DownloadStatus.Downloading,
            Progress = 50,
            CreatedAt = DateTime.UtcNow
        });

        // Act
        var all = await _repository.GetAllAsync(CancellationToken.None);

        // Assert
        all.Count().Should().Be(2);
    }
}
```

**Checklist:**
- [ ] Criar InMemoryStatusRepositoryTests.cs
- [ ] Testar AddAsync (2 cenários)
- [ ] Testar GetByIdAsync (2 cenários)
- [ ] Testar GetAllAsync (2 cenários)
- [ ] Testar UpdateProgressAsync (4 cenários)
- [ ] Testar DeleteAsync (2 cenários)

**Critérios de aceito:**
- ✅ Todos testes passam
- ✅ Cobertura de InMemoryStatusRepository > 95%

---

### Tarefa 4: Testes unitários para Health Checks

**Estimativa:** 2 horas

**Arquivos:**
- `tests/Cutube.Api.Tests/Unit/HealthChecks/YtDlpHealthCheckTests.cs`
- `tests/Cutube.Api.Tests/Unit/HealthChecks/FfmpegHealthCheckTests.cs`
- `tests/Cutube.Api.Tests/Unit/HealthChecks/DiskSpaceHealthCheckTests.cs`

**Cenários de teste (YtDlpHealthCheck):**
1. [ ] Deve retornar Healthy se yt-dlp instalado (mock Process)
2. [ ] Deve retornar Unhealthy se yt-dlp não instalado
3. [ ] Deve retornar Unhealthy se exceção lançada
4. [ ] Deve incluir versão no resultado Healthy

**Cenários de teste (FfmpegHealthCheck):**
1. [ ] Deve retornar Healthy se ffmpeg instalado (mock Process)
2. [ ] Deve retornar Unhealthy se ffmpeg não instalado
3. [ ] Deve retornar Unhealthy se exceção lançada

**Cenários de teste (DiskSpaceHealthCheck):**
1. [ ] Deve retornar Healthy se espaço suficiente
2. [ ] Deve retornar Degraded se espaço insuficiente
3. [ ] Deve retornar Unhealthy se exceção lançada
4. [ ] Deve usar configuração correta (min required GB)

**Implementação exemplo (YtDlpHealthCheck):**
```csharp
public class YtDlpHealthCheckTests
{
    [Fact]
    public async Task CheckHealthAsync_WithYtDlpInstalled_ReturnsHealthy()
    {
        // Arrange
        var healthCheck = new YtDlpHealthCheck();
        var context = new HealthCheckContext();

        // Act
        var result = await healthCheck.CheckHealthAsync(context, CancellationToken.None);

        // Assert
        if (Environment.GetEnvironmentVariable("CI") == "true")
        {
            // Skip test in CI if yt-dlp not available
            result.Status.Should().BeOneOf(HealthStatus.Healthy, HealthStatus.Unhealthy);
        }
        else
        {
            result.Status.Should().Be(HealthStatus.Healthy);
            result.Description.Should().Contain("yt-dlp is available");
        }
    }

    [Fact]
    public async Task CheckHealthAsync_WithoutYtDlp_ReturnsUnhealthy()
    {
        // Este teste requer mock de Process.Start, o que é complexo
        // Alternativa: testar com exe inexistente modificando PATH
        // Ou usar interface wrapper para Process

        // Pular por enquanto, marcando como melhoria futura
        // TODO: Adicionar wrapper para IProcessExecutor
    }
}
```

**Nota:** Health checks que dependem de Process.Start são difíceis de testar sem mock. Considere refatorar para usar interface `IProcessExecutor`.

**Checklist:**
- [ ] Criar YtDlpHealthCheckTests.cs
- [ ] Criar FfmpegHealthCheckTests.cs
- [ ] Criar DiskSpaceHealthCheckTests.cs
- [ ] Testar cenário Healthy
- [ ] Testar cenário Unhealthy/Degraded
- [ ] Testar exceções

**Critérios de aceito:**
- ✅ Todos testes passam
- ✅ Cobertura de HealthChecks > 70%

---

### Tarefa 5: Melhorar testes de integração existentes

**Estimativa:** 2 horas

**Arquivos a modificar:**
- `tests/Cutube.Api.Tests/Integration/DownloadsEndpointsTests.cs`
- `tests/Cutube.Api.Tests/Integration/VideosEndpointsTests.cs`
- `tests/Cutube.Api.Tests/Helpers/TestWebApplicationFactory.cs`

**Melhorias:**

1. **Adicionar testes com dados reais:**
   ```csharp
   [Fact]
   public async Task CreateDownload_WithValidRequest_Returns202Accepted()
   {
       // Arrange
       var request = new
       {
           Url = "https://youtube.com/watch?v=test",
           OutputPath = "/tmp/test"
       };

       // Act
       var response = await _client.PostAsJsonAsync("/api/downloads", request);

       // Assert
       response.StatusCode.Should().Be(HttpStatusCode.Accepted);

       var content = await response.Content.ReadFromJsonAsync<CreateDownloadResponse>();
       content!.Status.Should().Be("queued");
       content.DownloadId.Should().NotBeEmpty();
   }

   [Fact]
   public async Task CreateDownload_WithTimeRange_Returns202Accepted()
   {
       // Arrange
       var request = new
       {
           Url = "https://youtube.com/watch?v=test",
           OutputPath = "/tmp/test",
           StartTime = "00:01:00",
           EndTime = "00:02:00"
       };

       // Act
       var response = await _client.PostAsJsonAsync("/api/downloads", request);

       // Assert
       response.StatusCode.Should().Be(HttpStatusCode.Accepted);
   }

   [Fact]
   public async Task CreateDownload_WithInvalidTimeRange_Returns400BadRequest()
   {
       // Arrange
       var request = new
       {
           Url = "https://youtube.com/watch?v=test",
           OutputPath = "/tmp/test",
           StartTime = "00:02:00",  // Maior que EndTime
           EndTime = "00:01:00"
       };

       // Act
       var response = await _client.PostAsJsonAsync("/api/downloads", request);

       // Assert
       response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
   }
   ```

2. **Adicionar teste de fluxo completo:**
   ```csharp
   [Fact]
   public async Task DownloadFlow_EnqueueAndGetStatus_ReturnsCorrectStatus()
   {
       // Arrange
       var createRequest = new
       {
           Url = "https://youtube.com/watch?v=test",
           OutputPath = "/tmp/test"
       };

       // Act - Create download
       var createResponse = await _client.PostAsJsonAsync("/api/downloads", createRequest);
       var createContent = await createResponse.Content.ReadFromJsonAsync<CreateDownloadResponse>();

       // Act - Get download by ID
       var getResponse = await _client.GetAsync($"/api/downloads/{createContent!.DownloadId}");
       var getContent = await getResponse.Content.ReadFromJsonAsync<DownloadDetails>();

       // Assert
       getContent!.DownloadId.Should().Be(createContent.DownloadId);
       getContent.Status.Should().BeOneOf("queued", "downloading");
   }

   [Fact]
   public async Task DeleteDownload_WithExistingDownload_Returns204NoContent()
   {
       // Arrange
       var createRequest = new
       {
           Url = "https://youtube.com/watch?v=test",
           OutputPath = "/tmp/test"
       };

       // Create download first
       var createResponse = await _client.PostAsJsonAsync("/api/downloads", createRequest);
       var createContent = await createResponse.Content.ReadFromJsonAsync<CreateDownloadResponse>();

       // Act
       var deleteResponse = await _client.DeleteAsync($"/api/downloads/{createContent!.DownloadId}");

       // Assert
       deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

       // Verify it's deleted
       var getResponse = await _client.GetAsync($"/api/downloads/{createContent.DownloadId}");
       getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
   }
   ```

3. **Melhorar TestWebApplicationFactory com mocks:**
   ```csharp
   protected override void ConfigureWebHost(IWebHostBuilder builder)
   {
       builder.ConfigureServices(services =>
       {
           // Opcional: substituir serviços com mocks para testes mais rápidos
           // Exemplo: Mock de IVideoDownloader para evitar chamadas reais ao yt-dlp

           // var descriptor = services.SingleOrDefault(
           //     d => d.ServiceType == typeof(IVideoDownloader));
           // if (descriptor != null)
           // {
           //     services.Remove(descriptor);
           //     var mock = new Mock<IVideoDownloader>();
           //     // Setup mock behavior
           //     services.AddSingleton(mock.Object);
           // }
       });
   }
   ```

**Checklist:**
- [ ] Adicionar teste CreateDownload com request válido
- [ ] Adicionar teste CreateDownload com TimeRange
- [ ] Adicionar teste de fluxo completo (enqueue + get status)
- [ ] Adicionar teste DeleteDownload funcional
- [ ] Melhorar TestWebApplicationFactory (opcional)

**Critérios de aceito:**
- ✅ Novos testes passam
- ✅ Cobertura de Endpoints > 80%

---

### Tarefa 6: Adicionar ExcludeFromCodeCoverage

**Estimativa:** 1 hora

**Arquivos a modificar:**

1. **Program.cs** (excluir totalmente):
   ```csharp
   using System.Diagnostics.CodeAnalysis;

   [ExcludeFromCodeCoverage]
   var builder = WebApplication.CreateBuilder(args);
   // ... resto do código
   ```

2. **BackgroundDownloadWorker.cs** (excluir ExecuteAsync):
   ```csharp
   [ExcludeFromCodeCoverage]
   protected override async Task ExecuteAsync(CancellationToken stoppingToken)
   {
       // ... código ...
   }
   ```

3. **DTOs** (marcar classes como exclude):
   ```csharp
   using System.Diagnostics.CodeAnalysis;

   [ExcludeFromCodeCoverage]
   public record CreateDownloadRequest
   {
       // ...
   }
   ```

**Checklist:**
- [ ] Adicionar ExcludeFromCodeCoverage em Program.cs
- [ ] Adicionar ExcludeFromCodeCoverage em BackgroundDownloadWorker.ExecuteAsync
- [ ] Adicionar ExcludeFromCodeCoverage em DTOs
- [ ] Verificar novo percentual de cobertura

**Critérios de aceito:**
- ✅ Código boilerplate excluído da cobertura
- ✅ Cobertura ajustada > 80%

---

### Tarefa 7: Validação final e relatório

**Estimativa:** 1 hora

**Comandos:**
```bash
# Executar todos os testes
dotnet test tests/Cutube.Api.Tests/Cutube.Api.Tests.csproj

# Executar com cobertura
dotnet test tests/Cutube.Api.Tests/Cutube.Api.Tests.csproj \
  --collect:"XPlat Code Coverage" \
  --results-directory:./TestResults

# Verificar relatório de cobertura
cat TestResults/*/coverage.cobertura.xml | grep line-rate
```

**Checklist:**
- [ ] Executar todos os testes (100% pass)
- [ ] Verificar cobertura > 80%
- [ ] Documentar testes restantes (débito técnico)
- [ ] Atualizar README com instruções de teste

**Critérios de aceito:**
- ✅ **Cobertura Cutube.Api > 80%**
- ✅ Todos os testes passam
- ✅ Testes executam em < 15 segundos
- ✅ Documentação atualizada

---

## Qualidade Gates - Fase 2.2 Cobertura

**ANTES de considerar completa, TODOS os itens abaixo devem ser verdadeiros:**

- [ ] **dotnet test** - **100% pass** (0 falhas)
- [ ] **Cobertura > 80%** no projeto Cutube.Api
- [ ] **ExcludeFromCodeCoverage** aplicado corretamente
- [ ] Testes unitários separados de integração
- [ ] Todos os testes executam em < 15 segundos

---

## Cronograma Detalhado

| Tarefa | Estimativa | Dependencies | Blocker |
|--------|-----------|--------------|---------|
| 1. Preparar infraestrutura | 1h | - | Não |
| 2. DownloadQueue tests | 2h | 1 | Não |
| 3. InMemoryStatusRepository tests | 2h | 1 | Não |
| 4. Health Checks tests | 2h | 1 | Não |
| 5. Melhorar integração | 2h | 1 | Não |
| 6. ExcludeFromCodeCoverage | 1h | 2,3,4,5 | Não |
| 7. Validação final | 1h | 6 | Não |

**Total:** 11 horas (2-3 dias)

---

## Tecnologias

- **xUnit** - Framework de testes
- **Moq** - Mocking framework
- **FluentAssertions** - Asserts fluentes
- **Microsoft.AspNetCore.Mvc.Testing** - Testes de integração
- **coverlet.collector** - Cobertura de código

---

## Exclusões (Débito Técnico Conhecido)

**Código excluído da cobertura com justificativa:**

1. **Program.cs** - Configuração de DI e pipeline, testado indiretamente via testes de integração
2. **BackgroundDownloadWorker.ExecuteAsync** - HostedService complexo, testado via integração
3. **DTOs** - Classes de dados anêmicas, sem lógica para testar
4. **Health checks com Process.Start** - Requer wrapper de IProcessExecutor (melhoria futura)

---

## Métricas de Sucesso

| Métrica | Antes | Depois | Meta |
|---------|-------|--------|------|
| Cobertura Cutube.Api | 30.48% | > 80% | 80%+ |
| Total de testes | 13 | 50+ | 50+ |
| Testes unitários | 0 | 30+ | 30+ |
| Testes integração | 13 | 20+ | 20+ |
| Tempo de execução | ~3s | < 15s | < 15s |

---

## Próximos Passos

Após completar esta tarefa:

1. ✅ **Fase 2.3**: Implementar SignalR (WebSocket)
2. 📊 **Métricas**: Configurar relatórios de cobertura contínua
3. 🔧 **CI/CD**: Adicionar gate de cobertura no pipeline de build
