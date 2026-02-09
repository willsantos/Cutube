# Plano de Implementação - Fase 1.1: Logging Estruturado

## Objetivo
Implementar sistema de logging estruturado com Serilog para capturar e persistir logs de aplicação com rotação automática de arquivos e formato JSON estruturado.

## Problema Atual
- ❌ Nenhum sistema de logging estruturado
- ❌ Erros não são rastreados entre execuções
- ❌ Impossível investigar problemas reportados por usuários
- ❌ Sem visibilidade de comportamento em produção

## Solução
Implementar logging estruturado usando Serilog com:
- Logs em JSON compacto
- Rotação automática de arquivos (10MB, 7 dias)
- Níveis de log (Debug, Info, Warning, Error, Critical)
- Contexto estruturado (key-value pairs)
- Path configurável via IEnvironmentService

## Branch
`feature/Cutube-dsn-fase-1.1-logging-estruturado`

## Tarefas Incluídas
- ✅ **Cutube-dsn.1**: Adicionar pacote Serilog
- ✅ **Cutube-dsn.3**: Integrar Logger no Program.cs
- ✅ **Cutube-dsn.4**: Criar interfaces e enums de logging
- ✅ **Cutube-dsn.5**: Criar ILoggerService interface
- ✅ **Cutube-dsn.6**: Implementar FileLoggerService
- ✅ **Cutube-dsn.7**: Testes unitários para Logging

---

## Estrutura Final

```
cutube/
├── Logging/
│   ├── ILoggerService.cs       # Interface pública de logging
│   ├── FileLoggerService.cs    # Implementação com Serilog
│   ├── LogLevel.cs             # Enum de níveis de log
│   └── LogEntry.cs             # Modelo de entrada de log (opcional)
└── Program.cs                  # Modificado (injeta logger)

Cutube.Tests/
└── Unit/
    └── Logging/
        └── FileLoggerServiceTests.cs
```

---

## Commits Planejados

### 1. `feat(logging): adicionar pacote Serilog` (Cutube-dsn.1)

**Arquivo:** `cutube.csproj` (MODIFICAR)

**Ações:**
- Adicionar pacotes NuGet via CLI
- Verificar compatibilidade com .NET 10
- Build sem warnings

**Comandos:**
```bash
dotnet add package Serilog
dotnet add package Serilog.Sinks.File
dotnet add package Serilog.Formatting.Compact
```

**Verificação:**
```bash
dotnet build
# Esperado: sucesso, sem warnings
```

**Critérios:**
- [ ] Pacotes Serilog adicionados ao projeto
- [ ] Build sem warnings
- [ ] Versões compatíveis com .NET 10

---

### 2. `feat(logging): criar interfaces e enums de logging` (Cutube-dsn.4)

**Arquivos NOVOS:**

#### `cutube/Logging/LogLevel.cs`
```csharp
namespace Cutube.Logging;

/// <summary>
/// Níveis de severidade de log
/// </summary>
public enum LogLevel
{
    Debug = 0,
    Info = 1,
    Warning = 2,
    Error = 3,
    Critical = 4
}
```

#### `cutube/Logging/ILogEntry.cs` (opcional, para validações futuras)
```csharp
namespace Cutube.Logging;

/// <summary>
/// Representa uma entrada de log estruturado
/// </summary>
public interface ILogEntry
{
    DateTime Timestamp { get; }
    LogLevel Level { get; }
    string Message { get; }
    Exception? Exception { get; }
    Dictionary<string, object> Context { get; }
}
```

#### `cutube/Logging/LogEntry.cs`
```csharp
namespace Cutube.Logging;

/// <summary>
/// Implementação concreta de entrada de log
/// </summary>
public class LogEntry : ILogEntry
{
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    public LogLevel Level { get; init; }
    public string Message { get; init; } = string.Empty;
    public Exception? Exception { get; init; }
    public Dictionary<string, object> Context { get; init; } = new();
}
```

**Critérios:**
- [ ] LogLevel enum criado
- [ ] ILogEntry interface criada com XML comments
- [ ] LogEntry classe criada
- [ ] Compila sem erros

---

### 3. `feat(logging): criar ILoggerService interface` (Cutube-dsn.5)

**Arquivo NOVO:** `cutube/Logging/ILoggerService.cs`

```csharp
namespace Cutube.Logging;

/// <summary>
/// Serviço de logging estruturado para aplicação Cutube
/// </summary>
public interface ILoggerService
{
    /// <summary>
    /// Registra mensagem de nível Debug (informação detalhada para desenvolvimento)
    /// </summary>
    void LogDebug(string message, params (string key, object value)[] context);

    /// <summary>
    /// Registra mensagem de nível Info (informação geral)
    /// </summary>
    void LogInfo(string message, params (string key, object value)[] context);

    /// <summary>
    /// Registra mensagem de nível Warning (alerta que não impede operação)
    /// </summary>
    void LogWarning(string message, params (string key, object value)[] context);

    /// <summary>
    /// Registra erro recuperável com exceção
    /// </summary>
    void LogError(Exception exception, string message, params (string key, object value)[] context);

    /// <summary>
    /// Registra mensagem de nível Error sem exceção
    /// </summary>
    void LogError(string message, params (string key, object value)[] context);

    /// <summary>
    /// Registra erro crítico que impede continuação da aplicação
    /// </summary>
    void LogCritical(Exception exception, string message, params (string key, object value)[] context);
}
```

**Design decisions:**
- Tuplas nomeadas para contexto: `LogInfo("Download started", ("url", url), ("output", path))`
- Sobrecarga de LogError para erros sem exceção
- Níveis alinhados com Serilog

**Critérios:**
- [ ] Interface ILoggerService criada
- [ ] Todos os métodos de log definidos
- [ ] XML comments em todos membros
- [ ] Tuplas nomeadas suportadas (C# 10+)

---

### 4. `feat(logging): implementar FileLoggerService` (Cutube-dsn.6)

**Arquivo NOVO:** `cutube/Logging/FileLoggerService.cs`

```csharp
using Cutube.Interfaces;
using Serilog;
using Serilog.Formatting.Compact;

namespace Cutube.Logging;

/// <summary>
/// Implementação de logger usando Serilog com output em arquivo JSON
/// </summary>
public class FileLoggerService : ILoggerService, IDisposable
{
    private readonly Serilog.Core.Logger _logger;
    private readonly string _logBasePath;
    private bool _disposed = false;

    public FileLoggerService(IEnvironmentService environmentService)
    {
        // Obter path base usando serviço existente
        _logBasePath = Path.Combine(
            environmentService.GetLocalSharePath(),
            "Cutube",
            "logs"
        );

        // Criar diretório se não existe
        if (!Directory.Exists(_logBasePath))
        {
            Directory.CreateDirectory(_logBasePath);
        }

        // Configurar Serilog
        _logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(
                path: Path.Combine(_logBasePath, "cutube-.log"),
                rollingInterval: RollingInterval.Day,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level}] {Message}{NewLine}{Exception}",
                fileSizeLimitBytes: 10_000_000, // 10MB
                retainedFileCountLimit: 7,      // 7 dias
                formatter: new CompactJsonFormatter(),
                shared: true,
                flushToDiskInterval: TimeSpan.FromSeconds(1)
            )
            .CreateLogger();
    }

    public void LogDebug(string message, params (string key, object value)[] context)
    {
        _logger.Debug("{Message} {@Context}", message, context.ToDictionary());
    }

    public void LogInfo(string message, params (string key, object value)[] context)
    {
        _logger.Information("{Message} {@Context}", message, context.ToDictionary());
    }

    public void LogWarning(string message, params (string key, object value)[] context)
    {
        _logger.Warning("{Message} {@Context}", message, context.ToDictionary());
    }

    public void LogError(Exception exception, string message, params (string key, object value)[] context)
    {
        _logger.Error(exception, "{Message} {@Context}", message, context.ToDictionary());
    }

    public void LogError(string message, params (string key, object value)[] context)
    {
        _logger.Error("{Message} {@Context}", message, context.ToDictionary());
    }

    public void LogCritical(Exception exception, string message, params (string key, object value)[] context)
    {
        _logger.Fatal(exception, "{Message} {@Context}", message, context.ToDictionary());
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _logger.Dispose();
            _disposed = true;
        }
    }
}

/// <summary>
/// Extension method para converter tuplas em Dictionary
/// </summary>
internal static class LoggingExtensions
{
    public static Dictionary<string, object> ToDictionary(this (string key, object value)[] tuples)
    {
        return tuples.ToDictionary(t => t.key, t => t.value);
    }
}
```

**Requisitos:**
- Path: `~/.local/share/Cutube/logs/cutube-{date}.log`
- Formato: JSON compacto (CompactJsonFormatter)
- Rotação: 10MB por arquivo ou diariamente
- Retenção: 7 dias
- Flush: 1 segundo

**Critérios:**
- [ ] Usa IEnvironmentService existente
- [ ] Serilog configurado corretamente
- [ ] JSON estruturado funcionando
- [ ] Dispose implementado
- [ ] Cria diretório se não existe
- [ ] Build sem warnings

---

### 5. `feat(logging): integrar logger no Program.cs` (Cutube-dsn.3)

**Arquivo MODIFICADO:** `cutube/Program.cs`

**Alterações:**
```csharp
// Adicionar using
using Cutube.Logging;

// No início do Main, criar logger
var loggerService = new FileLoggerService(environmentService);

// Adicionar logger ao ProgramWorkflow
var app = new ProgramWorkflow(
    menuService: menuService,
    ytdlpService: ytdlpService,
    consoleService: consoleService,
    fileService: fileService,
    environmentService: environmentService,
    loggerService: loggerService  // NOVO
);

// Envolvendo execução com try-catch para log crítico
try
{
    await app.RunAsync(cancellationToken);
}
catch (Exception ex)
{
    loggerService.LogCritical(ex, "Erro fatal na aplicação");
    throw;
}
finally
{
    if (loggerService is IDisposable disposableLogger)
    {
        disposableLogger.Dispose();
    }
}
```

**Critérios:**
- [ ] FileLoggerService instanciado no Main
- [ ] Injetado no ProgramWorkflow
- [ ] Try-catch para erros críticos
- [ ] Dispose no finally
- [ ] Build sem warnings

---

### 6. `test(logging): adicionar testes unitários para FileLoggerService` (Cutube-dsn.7)

**Arquivo NOVO:** `Cutube.Tests/Unit/Logging/FileLoggerServiceTests.cs`

```csharp
using Cutube.Logging;
using Xunit;
using Cutube.Interfaces;

namespace Cutube.Tests.Unit.Logging;

public class FileLoggerServiceTests : IDisposable
{
    private readonly FileLoggerService _logger;
    private readonly string _testLogPath;
    private readonly MockEnvironmentService _envService;

    public FileLoggerServiceTests()
    {
        // Setup temp directory
        _testLogPath = Path.Combine(Path.GetTempPath(), $"cutube-test-{Guid.NewGuid()}");
        Directory.CreateDirectory(_testLogPath);

        _envService = new MockEnvironmentService(_testLogPath);
        _logger = new FileLoggerService(_envService);
    }

    [Fact]
    public void LogInfo_ShouldCreateLogFile()
    {
        // Act
        _logger.LogInfo("Test message");

        // Assert
        var logFiles = Directory.GetFiles(_testLogPath, "cutube-*.log");
        Assert.Single(logFiles);
    }

    [Fact]
    public void LogInfo_WithContext_ShouldSerializeContext()
    {
        // Act
        _logger.LogInfo("Test", ("key1", "value1"), ("key2", 42));

        // Assert
        var logFile = Directory.GetFiles(_testLogPath, "cutube-*.log").First();
        var content = File.ReadAllText(logFile);
        Assert.Contains("key1", content);
        Assert.Contains("value1", content);
        Assert.Contains("key2", content);
        Assert.Contains("42", content);
    }

    [Fact]
    public void LogError_WithException_ShouldIncludeStackTrace()
    {
        // Arrange
        var exception = new Exception("Test exception");

        // Act
        _logger.LogError(exception, "Error occurred");

        // Assert
        var logFile = Directory.GetFiles(_testLogPath, "cutube-*.log").First();
        var content = File.ReadAllText(logFile);
        Assert.Contains("Test exception", content);
        Assert.Contains("Error occurred", content);
    }

    [Fact]
    public void Dispose_ShouldFlushLogger()
    {
        // Act
        _logger.LogInfo("Before dispose");
        _logger.Dispose();
        _logger.LogInfo("After dispose"); // Should not crash

        // Assert - se não lançar exceção, passou
    }

    public void Dispose()
    {
        _logger?.Dispose();
        if (Directory.Exists(_testLogPath))
        {
            Directory.Delete(_testLogPath, recursive: true);
        }
    }

    private class MockEnvironmentService : IEnvironmentService
    {
        private readonly string _basePath;

        public MockEnvironmentService(string basePath)
        {
            _basePath = basePath;
        }

        public string GetLocalSharePath() => _basePath;
        // Implementar outros métodos com throw new NotImplementedException()
    }
}
```

**Critérios:**
- [ ] Teste de criação de arquivo de log
- [ ] Teste de contexto estruturado
- [ ] Teste de exceção com stack trace
- [ ] Teste de dispose
- [ ] Mock de IEnvironmentService
- [ ] Todos os testes passam
- [ ] Cleanup no Dispose do teste

---

## Critérios de Aceite

### Funcional
- [ ] Logs são escritos em `~/.local/share/Cutube/logs/` em JSON
- [ ] Arquivos nomeados como `cutube-YYYYMMDD.log`
- [ ] Rotação automática de arquivos (10MB ou diariamente)
- [ ] Retenção de 7 dias
- [ ] Logs contém: timestamp, level, message, exception, context
- [ ] Contexto estruturado via tuplas nomeadas

### Técnico
- [ ] `dotnet build` - ZERO warnings
- [ ] `dotnet test` - 100% dos testes passando
- [ ] Interface ILoggerService segue padrão I-prefix
- [ ] Usa IEnvironmentService existente
- [ ] Dispose implementado corretamente

### Qualidade
- [ ] XML comments em interfaces e classes públicas
- [ ] Sem code duplication
- [ ] Nomes descritivos e seguindo convenções C#

---

## Testes Manuais Sugeridos

### Verificação de logs
```bash
# Executar aplicação
dotnet run --project cutube.csproj -- --help

# Verificar se log foi criado
ls -la ~/.local/share/Cutube/logs/

# Verificar conteúdo do log
cat ~/.local/share/Cutube/logs/cutube-*.log | jq .
```

### Teste de rotação
```bash
# Gerar log > 10MB
for i in {1..1000}; do
    echo "Log message $i" >> test-input.txt
    dotnet run --project cutube.csproj -- download "https://youtube.com/watch?v=test" --output test.mp4
done

# Verificar se rotação funcionou
ls -lh ~/.local/share/Cutube/logs/
```

### Teste de contexto estruturado
```csharp
_logger.LogInfo("Download started",
    ("url", "https://youtube.com/watch?v=abc"),
    ("output", "/tmp/video.mp4"),
    ("audioOnly", false)
);

// Verificar no JSON:
// {"Message":"Download started","Context":{"url":"...","output":"...","audioOnly":false}}
```

---

## Notas de Implementação

### Serilog Configuration
- `CompactJsonFormatter`: JSON minimalista, otimizado para parsing
- `rollingInterval: Day`: Cria novo arquivo por dia
- `fileSizeLimitBytes: 10MB`: Rotação também por tamanho
- `retainedFileCountLimit: 7`: Remove arquivos > 7 dias
- `flushToDiskInterval: 1s`: Garante logs em disco mesmo se crash

### Contexto Estruturado
- Tuplas nomeadas C#: `("key", value)` syntax
- Convertido para Dictionary via extension method
- Serializado como JSON no log

### IEnvironmentService
- Serviço já existe no projeto
- Retorna `~/.local/share` no Linux
- Windows/MAC retornam paths equivalentes

### Logging em produção
- Debug: Desenvolvimento apenas (verbose)
- Info: Operações normais (download start/finish)
- Warning: Recuperação automática (retry)
- Error: Erros recuperáveis (network timeout com sucesso após retry)
- Critical: Erros fatais (missing dependency)

---

## Breaking Changes

**Nenhum breaking change**
- Logger é novo código, paralelo ao existente
- CLI continua funcionando sem modificações
- Integração é via injeção de dependência

---

## Dependências

**Nenhuma**
- Todas as tarefas podem ser feitas independentemente
- Serilog é biblioteca externa, sem conflitos

---

## Próximos Passos (Fases seguintes)

Após esta fase, as seguintes fases dependem do Logging:

- **Fase 1.2**: Error Handler (usa ILoggerService para logar erros)
- **Fase 1.3**: Recovery (usa ILoggerService para logar estado)

---

## Estimativa

**Total:** ~15 horas (2 dias)
- Cutube-dsn.1 (Serilog setup): 1h
- Cutube-dsn.4 (interfaces/enums): 2h
- Cutube-dsn.5 (ILoggerService): 1h
- Cutube-dsn.6 (FileLoggerService): 4h
- Cutube-dsn.3 (integração Program.cs): 1h
- Cutube-dsn.7 (testes): 3h
- Qualidade gates + commits: 3h

---

## Commit Message Final

```bash
git add .
git commit -m "feat(logging): implementar logging estruturado com Serilog (Cutube-dsn.1,3-7)

- Adicionar pacotes Serilog, Serilog.Sinks.File, Serilog.Formatting.Compact
- Criar interfaces ILoggerService, ILogEntry e enums LogLevel
- Implementar FileLoggerService com rotação automática (10MB, 7 dias)
- Integrar logger no Program.cs com dispose correto
- Adicionar testes unitários para FileLoggerService
- Logs em JSON estruturado com contexto key-value

Branch: feature/Cutube-dsn-fase-1.1-logging-estruturado
Tarefas: Cutube-dsn.1, Cutube-dsn.3, Cutube-dsn.4, Cutube-dsn.5, Cutube-dsn.6, Cutube-dsn.7"
```
