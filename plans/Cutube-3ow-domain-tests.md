# Plano de Implementação: Cutube.Domain.Tests

**Tarefa:** Cutube-3ow
**Data:** 2026-02-05
**Estimativa:** 4 horas
**Responsável:** Wilson Santos

## Contexto

Criar testes unitários para a camada de domínio do Cutube, garantindo cobertura > 80% e testes abrangentes para todos os serviços e modelos críticos.

## Estrutura Atual do Domain

```
src/Cutube.Domain/
├── Interfaces/
│   ├── IDownloadValidator.cs
│   ├── IVideoDownloader.cs
│   ├── IVideoProcessor.cs
│   ├── IVideoMetadataProvider.cs
│   ├── IProgressReporter.cs
│   └── IFileSystem.cs
├── Models/
│   ├── DownloadRequest.cs
│   ├── VideoMetadata.cs
│   ├── TimeRange.cs
│   ├── ProcessingProgress.cs
│   ├── ValidationResult.cs
│   ├── DownloadProgress.cs
│   ├── DownloadResult.cs
│   ├── ProcessingRequest.cs
│   └── ProcessingResult.cs
└── Services/
    ├── ValidationService.cs
    ├── DownloadService.cs
    ├── MetadataService.cs
    └── ProcessingService.cs
```

## Objetivos

### 1. Criar Projeto de Testes
- Criar projeto xUnit: `tests/Cutube.Domain.Tests/Cutube.Domain.Tests.csproj`
- Adicionar dependências:
  - xUnit (já existe versão 2.9.3)
  - Moq (já existe versão 4.20.72)
  - FluentAssertions (já existe versão 8.8.0)
  - Coverlet (já configurado)

### 2. Estrutura de Testes

```
tests/Cutube.Domain.Tests/
├── Services/
│   ├── ValidationServiceTests.cs
│   ├── DownloadServiceTests.cs
│   ├── MetadataServiceTests.cs
│   └── ProcessingServiceTests.cs
└── Models/
    ├── DownloadRequestTests.cs
    ├── ValidationResultTests.cs
    └── TimeRangeTests.cs
```

## Casos de Testo

### ValidationServiceTests.cs

#### 1. ValidateUrl Tests
```csharp
// Casos de Sucesso
- URL válida youtube.com
- URL válida www.youtube.com
- URL válida m.youtube.com
- URL válida youtu.be
- URL com HTTPS

// Casos de Falha
- URL vazia ou null
- URL malformada
- URL sem http/https
- URL de outro domínio
- URL com subdomínio inválido
```

#### 2. ValidateFileName Tests
```csharp
// Casos de Sucesso
- Nome vazio (permitido)
- Nome válido simples
- Nome com espaços
- Nome com extensão

// Casos de Falha
- Caracteres inválidos: <, >, :, ", |, ?, *, /
- Barra (/)
- Barra invertida (\)
```

#### 3. ValidateTimeRange Tests
```csharp
// Casos de Sucesso
- Tempo em formato MM:SS
- Tempo em formato HH:MM:SS
- Tempo em segundos (90s)
- Início < Fim válido

// Casos de Falha
- Início <= 0
- Fim <= Início
- Formato inválido
- Formato parcialmente correto
```

#### 4. ValidateDirectory Tests
```csharp
// Casos de Sucesso
- Diretório existente com permissão
- Diretório atual (.)

// Casos de Falha
- Diretório inexistente
- Sem permissão de escrita
- Caminho inválido
```

### DownloadServiceTests.cs

#### 1. DownloadAsync Tests (Sem TimeRange)
```csharp
// Setup
- Mock IVideoDownloader
- Mock IVideoProcessor
- Mock IDownloadValidator

// Casos de Sucesso
- Download simples com sucesso
- Download com AudioOnly

// Casos de Falha
- URL inválida
- Diretório de saída inválido
- Download falha no downloader subjacente
```

#### 2. DownloadAsync Tests (Com TimeRange)
```csharp
// Casos de Sucesso
- Download com time range completo
- Download com time range + audio only
- Limpeza do arquivo temporário

// Casos de Falha
- Falha no download inicial
- Falha no processamento
- Cancelamento durante download
- Cancelamento durante processamento
```

### MetadataServiceTests.cs

#### 1. GetMetadataAsync Tests
```csharp
// Setup
- Mock IVideoMetadataProvider
- Mock IDownloadValidator

// Casos de Sucesso
- Metadata recuperada com sucesso
- Título sanitizado corretamente
- Preserva informações originais

// Casos de Falha
- URL inválida
- Metadata não disponível
```

#### 2. SanitizeTitle (Método Privado)
```csharp
// Testes via GetMetadataAsync
- Remove caracteres inválidos: <, >, :, ", |, ?, *
- Espaços múltiplos tornam-se espaço único
- Trim no início e fim
```

### Models Tests

#### 1. ValidationResultTests
```csharp
- Success() cria IsValid = true
- Failure(msg) cria IsValid = false
- ErrorMessage corretamente definido
```

#### 2. DownloadRequestTests
```csharp
- Record imutável
- Required properties funcionam
- with expression para cópia
```

## Estratégia de Mocks

### Depender (Collaborators)
```csharp
// IVideoDownloader
- Setup: DownloadAsync returns DownloadResult
- Verify: DownloadAsync called with correct parameters

// IVideoProcessor
- Setup: ProcessAsync returns ProcessingResult
- Verify: ProcessAsync called with correct parameters

// IDownloadValidator
- Setup: ValidateUrl returns ValidationResult
- Setup: ValidateDirectory returns ValidationResult

// IVideoMetadataProvider
- Setup: GetMetadataAsync returns VideoMetadata
```

## Implementação

### Passo 1: Criar Projeto
```bash
dotnet new xunit -o tests/Cutube.Domain.Tests -n Cutube.Domain.Tests
cd tests/Cutube.Domain.Tests
dotnet add reference ../../src/Cutube.Domain/Cutube.Domain.csproj
```

### Passo 2: Adicionar Packages
```bash
dotnet add package Moq
dotnet add package FluentAssertions
```

### Passo 3: Criar Estrutura de Diretórios
```bash
mkdir -p Services
mkdir -p Models
```

### Passo 4: Implementar Testes
Ordem sugerida:
1. ValidationResultTests (mais simples)
2. ValidationServiceTests (sem dependências externas)
3. MetadataServiceTests (1 dependência mockada)
4. DownloadServiceTests (3 dependências mockadas)

### Passo 5: Executar Testes
```bash
dotnet test
```

### Passo 6: Verificar Cobertura
```bash
dotnet test --collect:"XPlat Code Coverage"
```

## Critérios de Qualidade

### Funcionais
- [ ] Todos os testes passam: `dotnet test`
- [ ] Cobertura > 80%: `dotnet test --collect:"XPlat Code Coverage"`
- [ ] Zero warnings no build: `dotnet build`

### Código de Teste
- [ ] Nomes descritivos: `MethodName_State_ExpectedOutcome`
- [ ] Arrange/Act/Assert claro
- [ ] Mocks apropriados (não over-mock)
- [ ] Testes independentes (não compartilham estado)

### Boas Práticas
- [ ] Um assert por teste (ou related asserts)
- [ ] Testes rápidos (unit tests, não integration)
- [ ] Testes determinísticos (mesmo resultado sempre)

## Deliverables

### Arquivos Criados
1. `tests/Cutube.Domain.Tests/Cutube.Domain.Tests.csproj`
2. `tests/Cutube.Domain.Tests/Services/ValidationServiceTests.cs`
3. `tests/Cutube.Domain.Tests/Services/DownloadServiceTests.cs`
4. `tests/Cutube.Domain.Tests/Services/MetadataServiceTests.cs`
5. `tests/Cutube.Domain.Tests/Models/DownloadRequestTests.cs`
6. `tests/Cutube.Domain.Tests/Models/ValidationResultTests.cs`

### Métricas de Sucesso
- **Cobertura:** > 80%
- **Testes:** ~40-50 testes
- **Tempo de execução:** < 5 segundos
- **Build:** Zero warnings

## Exemplo de Teste

```csharp
public class ValidationServiceTests
{
    private readonly ValidationService _validator;

    public ValidationServiceTests()
    {
        _validator = new ValidationService();
    }

    [Fact]
    public void ValidateUrl_ValidYouTubeUrl_ReturnsSuccess()
    {
        // Arrange
        var url = "https://www.youtube.com/watch?v=dQw4w9WgXcQ";

        // Act
        var result = _validator.ValidateUrl(url);

        // Assert
        result.IsValid.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ValidateUrl_EmptyUrl_ReturnsFailure(string? url)
    {
        // Act
        var result = _validator.ValidateUrl(url!);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Be("URL não pode ser vazia");
    }
}
```

## Próximos Passos

Após completar este plano:
1. Rodar `dotnet test` para garantir todos passam
2. Verificar cobertura com Coverlet
3. Commit com mensagem: `feat: add unit tests for Domain layer`
4. Atualizar Cutube-3ow com status "in_progress"
5. Fechar tarefa quando completar

## Observações

- Testes unitários devem ser rápidos (< 100ms por teste)
- Usar Theory/InlineData para testes parametrizados
- Mock apenas dependências externas (filesystem, network, etc)
- Validações de arquivo/diretório podem precisar de mock de IFileSystem
- Testes de integração são separados (projeto Cutube.Tests já existe)
