# Fase 2.5: Integração CLI ↔ API

**Status:** 🎯 Planejamento
**Épico:** Cutube-2i6 (Épico 2: Arquitetura Híbrida CLI + API)
**Duração:** 2-3 dias
**Responsável:** Backend Developer
**Prioridade:** 🔥 Alta
**Dependência:** ✅ Fase 2.4 completa (Frontend Next.js)

---

## Objetivo

Permitir que o CLI funcione tanto em modo **standalone** (local) quanto usando **API remota**, com configuração flexível e fallback automático.

**Benefícios:**
- CLI se torna "thin client" que pode usar API remota
- Mantém compatibilidade com modo local (standalone)
- Configuração simples via arquivo ou flag
- Prepara para sistema distribuído (Épico 3)
- Possibilita debug/teste remoto

---

## Visão Arquitetural

### Modos de Operação

```
┌─────────────────────────────────────────────────────────────┐
│                    CLI (cutube)                              │
│                                                             │
│  Modo Local              Modo API (Remoto)                   │
│  ┌──────────────┐      ┌─────────────────────────────────┐  │
│  │ Config:      │      │ Config:                         │  │
│  │ ApiUrl: null │      │ ApiUrl: "http://localhost:5000" │  │
│  └──────────────┘      └─────────────────────────────────┘  │
│         │                        │                           │
│         ▼                        ▼                           │
│  ┌──────────────┐      ┌─────────────────────────────────┐  │
│  │DownloadService│      │    ApiClient                    │  │
│  │  (Local)     │      │  (HTTP/REST)                    │  │
│  └──────────────┘      └─────────────────────────────────┘  │
│         │                        │                           │
│         ▼                        ▼                           │
│  ┌──────────────┐      ┌─────────────────────────────────┐  │
│  │ YtDlp        │      │   API HTTP                      │  │
│  │ Ffmpeg       │      │   /api/downloads                │  │
│  │ FileSystem   │      │   (Background Processing)       │  │
│  └──────────────┘      └─────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────┘
```

### Configuração

**Arquivo:** `~/.config/cutube/config.json`

```json
{
  "apiUrl": "http://localhost:5000",
  "defaultOutputPath": "~/Downloads",
  "maxConcurrentDownloads": 3,
  "timeoutSeconds": 300
}
```

**Override via flag:**

```bash
# Usa config do arquivo
cutube download https://youtube.com/watch?v=example

# Override para modo API
cutube download --api-url http://192.168.1.100:5000 https://youtube.com/watch?v=example

# Force modo local (mesmo com apiUrl configurado)
cutube download --local https://youtube.com/watch?v=example
```

---

## Tarefas

### 2.5.1 Criar sistema de configuração

**Estimativa:** 3 horas
**Arquivos:**
```
cutube/Configuration/
  ├── AppConfig.cs                    - Modelo de configuração
  ├── ConfigService.cs                 - Load/Save config
  └── ConfigDefaults.cs                - Valores padrão
```

**AppConfig Model:**

```csharp
// cutube/Configuration/AppConfig.cs
namespace Cutube.Configuration;

using System.Text.Json.Serialization;

public class AppConfig
{
    /// <summary>
    /// URL da API remota (ex: http://localhost:5000)
    /// Se null ou vazio, usa modo local (standalone)
    /// </summary>
    [JsonPropertyName("apiUrl")]
    public string? ApiUrl { get; set; }

    /// <summary>
    /// Indica se deve usar API remota
    /// </summary>
    [JsonIgnore]
    public bool UseApi => !string.IsNullOrWhiteSpace(ApiUrl);

    /// <summary>
    /// Caminho padrão para salvar downloads
    /// Padrão: ~/Downloads
    /// </summary>
    [JsonPropertyName("defaultOutputPath")]
    public string DefaultOutputPath { get; set; } = "~/Downloads";

    /// <summary>
    /// Número máximo de downloads simultâneos (apenas modo API)
    /// Padrão: 3
    /// </summary>
    [JsonPropertyName("maxConcurrentDownloads")]
    public int MaxConcurrentDownloads { get; set; } = 3;

    /// <summary>
    /// Timeout em segundos para operações da API
    /// Padrão: 300 (5 minutos)
    /// </summary>
    [JsonPropertyName("timeoutSeconds")]
    public int TimeoutSeconds { get; set; } = 300;

    /// <summary>
    /// Habilita logs de debug verbose
    /// Padrão: false
    /// </summary>
    [JsonPropertyName("verboseLogging")]
    public bool VerboseLogging { get; set; } = false;
}
```

**ConfigService:**

```csharp
// cutube/Configuration/ConfigService.cs
using System.Text.Json;

namespace Cutube.Configuration;

public class ConfigService
{
    private static readonly string ConfigDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserConfig),
        "cutube"
    );

    private static readonly string ConfigPath = Path.Combine(
        ConfigDirectory,
        "config.json"
    );

    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Carrega configuração do arquivo
    /// Se arquivo não existe, retorna config padrão
    /// </summary>
    public async Task<AppConfig> LoadAsync(CancellationToken ct = default)
    {
        if (!File.Exists(ConfigPath))
        {
            return CreateDefaultConfig();
        }

        try
        {
            var json = await File.ReadAllTextAsync(ConfigPath, ct);
            var config = JsonSerializer.Deserialize<AppConfig>(json, _jsonOptions);

            return config ?? CreateDefaultConfig();
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                $"Erro ao ler configuração de {ConfigPath}. " +
                $"Arquivo pode estar corrompido. Delete o arquivo para usar config padrão.",
                ex
            );
        }
    }

    /// <summary>
    /// Salva configuração no arquivo
    /// Cria diretório se não existe
    /// </summary>
    public async Task SaveAsync(AppConfig config, CancellationToken ct = default)
    {
        // Criar diretório se não existe
        Directory.CreateDirectory(ConfigDirectory);

        var json = JsonSerializer.Serialize(config, _jsonOptions);
        await File.WriteAllTextAsync(ConfigPath, json, ct);
    }

    /// <summary>
    /// Retorna configuração padrão
    /// </summary>
    private static AppConfig CreateDefaultConfig()
    {
        return new AppConfig
        {
            ApiUrl = null,
            DefaultOutputPath = "~/Downloads",
            MaxConcurrentDownloads = 3,
            TimeoutSeconds = 300,
            VerboseLogging = false
        };
    }

    /// <summary>
    /// Retorna caminho completo do arquivo de config (para debugging)
    /// </summary>
    public string GetConfigPath() => ConfigPath;

    /// <summary>
    /// Verifica se arquivo de config existe
    /// </summary>
    public bool ConfigExists() => File.Exists(ConfigPath);
}
```

**Comandos de debug:**

```bash
# Ver config atual
cutube config show

# Reset config para padrão
cutube config reset

# Editar config (abre no editor padrão)
cutube config edit
```

**Checklist:**
- [ ] Criar AppConfig com todas propriedades
- [ ] Criar ConfigService com LoadAsync/SaveAsync
- [ ] Salvar em ~/.config/cutube/config.json (Linux/Mac) ou %APPDATA%/cutube/config.json (Windows)
- [ ] Criar diretório automaticamente se não existe
- [ ] Tratar erros de JSON corrompido
- [ ] Testar load/save
- [ ] Adicionar comandos de debug (config show/reset/edit)

**Critérios de aceito:**
- ✅ Config salva/carrega corretamente
- ✅ Caminho de config correto para cada OS
- ✅ Tratamento de erro adequado (JSON corrompido)
- ✅ Valores padrão aplicados quando config não existe

---

### 2.5.2 Criar ApiClient para CLI

**Estimativa:** 4 horas
**Arquivo:** `cutube/Services/ApiClient.cs`

**Objetivo:** Implementar `IDownloadService` que faz chamadas HTTP para a API remota, permitindo que o CLI funcione como thin client.

**Implementação:**

```csharp
// cutube/Services/ApiClient.cs
using System.Net.Http.Json;
using Cutube.Configuration;
using Cutube.Domain.Models;
using Cutube.Domain.Services;
using FluentResults;

namespace Cutube.Services;

public class ApiClient : IDownloadService
{
    private readonly HttpClient _httpClient;
    private readonly AppConfig _config;
    private readonly ILogger<ApiClient> _logger;

    public ApiClient(AppConfig config, ILogger<ApiClient> logger)
    {
        _config = config;
        _logger = logger;

        // Configurar HttpClient
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(config.ApiUrl!),
            Timeout = TimeSpan.FromSeconds(config.TimeoutSeconds)
        };

        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Cutube-CLI/2.0");
    }

    public async Task<Result<DownloadResult>> DownloadAsync(
        DownloadRequest request,
        IProgress<DownloadProgress> progress,
        CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Enviando download para API: {Url}", request.Url);

            // 1. Criar download via API
            var createResponse = await _httpClient.PostAsJsonAsync(
                "/api/downloads",
                new
                {
                    url = request.Url,
                    outputPath = request.OutputPath,
                    startTime = request.TimeRange?.StartSeconds.ToString(),
                    endTime = request.TimeRange?.EndSeconds.ToString(),
                    audioOnly = request.AudioOnly
                },
                ct
            );

            if (!createResponse.IsSuccessStatusCode)
            {
                var errorContent = await createResponse.Content.ReadAsStringAsync(ct);
                return Result.Fail($"API retornou erro {createResponse.StatusCode}: {errorContent}");
            }

            var createData = await createResponse.Content.ReadFromJsonAsync<CreateDownloadResponse>(ct);
            var downloadId = createData!.DownloadId;

            _logger.LogInformation("Download criado na API: {DownloadId}", downloadId);

            // 2. Poll progress
            var lastProgress = 0.0;

            while (!ct.IsCancellationRequested)
            {
                await Task.Delay(1000, ct); // Poll a cada 1s

                var statusResponse = await _httpClient.GetAsync($"/api/downloads/{downloadId}", ct);

                if (!statusResponse.IsSuccessStatusCode)
                {
                    return Result.Fail($"Erro ao buscar status: {statusResponse.StatusCode}");
                }

                var status = await statusResponse.Content.ReadFromJsonAsync<DownloadDetails>(ct);

                // Report progress
                if (status!.Progress > lastProgress)
                {
                    progress.Report(new DownloadProgress
                    {
                        DownloadId = downloadId,
                        Percentage = status.Progress,
                        Speed = status.Speed,
                        Eta = string.IsNullOrEmpty(status.Eta) ? null : TimeSpan.Parse(status.Eta),
                        DownloadedBytes = status.DownloadedBytes ?? 0,
                        TotalBytes = status.TotalBytes ?? 0,
                        Status = Enum.Parse<DownloadStatus>(status.Status, true)
                    });

                    lastProgress = status.Progress;
                }

                // Check if completed
                if (status.Status.Equals("completed", StringComparison.OrdinalIgnoreCase) ||
                    status.Status.Equals("failed", StringComparison.OrdinalIgnoreCase) ||
                    status.Status.Equals("cancelled", StringComparison.OrdinalIgnoreCase))
                {
                    if (status.Status.Equals("completed", StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogInformation("Download completado: {DownloadId}", downloadId);

                        return Result.Success(new DownloadResult
                        {
                            FilePath = status.FilePath ?? "unknown",
                            Size = status.TotalBytes ?? 0,
                            Duration = TimeSpan.Zero // API não retorna duration no status
                        });
                    }
                    else if (status.Status.Equals("failed", StringComparison.OrdinalIgnoreCase))
                    {
                        return Result.Fail($"Download falhou: {status.ErrorMessage}");
                    }
                    else
                    {
                        return Result.Fail("Download foi cancelado");
                    }
                }
            }

            return Result.Fail("Download cancelado pelo usuário");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Erro de conexão com API");
            return Result.Fail($"Erro de conexão com API: {ex.Message}");
        }
        catch (TaskCanceledException)
        {
            return Result.Fail("Download cancelado");
        }
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}

// DTOs para API
public record CreateDownloadResponse(
    string DownloadId,
    string Status,
    string? Message
);

public record DownloadDetails(
    string DownloadId,
    string Url,
    string Status,
    double Progress,
    double Speed,
    string? Eta,
    long? DownloadedBytes,
    long? TotalBytes,
    string? FilePath,
    string? ErrorMessage
);
```

**Checklist:**
- [ ] Criar ApiClient implementando IDownloadService
- [ ] Implementar DownloadAsync com polling de progresso
- [ ] Configurar HttpClient com timeout da config
- [ ] Error handling (HTTP errors, timeout, connection refused)
- [ ] Polling a cada 1s (configurável?)
- [ ] Report progress via IProgress<DownloadProgress>
- [ ] Tratar cancellation token

**Critérios de aceito:**
- ✅ CLI pode usar API remota como se fosse local
- ✅ Progress reportado corretamente durante download
- ✅ Error handling adequado (API offline, timeout, etc)
- ✅ Funciona com downloads longos (> 10 minutos)

---

### 2.5.3 Modificar Program.cs para suportar modos

**Estimativa:** 3 horas
**Arquivo:** `cutube/Program.cs`

**Objetivo:** Adicionar flags `--api-url` e `--local` para permitir escolher modo de operação, além de detectar modo automaticamente via config.

**Implementação:**

```csharp
// cutube/Program.cs
using System.CommandLine;
using Cutube.Configuration;
using Cutube.Domain.Services;
using Cutube.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cutube;

class Program
{
    static async Task<int> Main(string[] args)
    {
        // Criar root command
        var rootCommand = new RootCommand("Cutube - YouTube video downloader");

        // Opções globais
        var apiOption = new Option<string?>(
            aliases: new[] { "--api-url", "-a" },
            description: "URL da API remota (ex: http://localhost:5000). Se não especificado, usa config do arquivo ou modo local.",
            getDefaultValue: () => null
        );

        var localOption = new Option<bool>(
            aliases: new[] { "--local", "-l" },
            description: "Força modo local (ignora apiUrl da config)",
            getDefaultValue: () => false
        );

        var verboseOption = new Option<bool>(
            aliases: new[] { "--verbose", "-v" },
            description: "Logs verbose",
            getDefaultValue: () => false
        );

        rootCommand.AddGlobalOption(apiOption);
        rootCommand.AddGlobalOption(localOption);
        rootCommand.AddGlobalOption(verboseOption);

        // Comando: download
        var downloadCommand = new Command("download", "Download vídeo do YouTube");
        var urlArgument = new Argument<string>("url", "URL do vídeo");
        var outputOption = new Option<string?>("--output", "-o", "Caminho de saída");
        var startOption = new Option<string?>("--start", "-s", "Tempo inicial (HH:MM:SS)");
        var endOption = new Option<string?>("--end", "-e", "Tempo final (HH:MM:SS)");
        var audioOption = new Option<bool>("--audio", "-a", "Download apenas áudio (MP3)");

        downloadCommand.AddArgument(urlArgument);
        downloadCommand.AddOption(outputOption);
        downloadCommand.AddOption(startOption);
        downloadCommand.AddOption(endOption);
        downloadCommand.AddOption(audioOption);

        downloadCommand.SetHandler(async (
            string url,
            string? output,
            string? start,
            string? end,
            bool audio,
            string? apiUrl,
            bool forceLocal,
            bool verbose) =>
        {
            await HandleDownloadAsync(url, output, start, end, audio, apiUrl, forceLocal, verbose);
        },
        urlArgument, outputOption, startOption, endOption, audioOption,
        apiOption, localOption, verboseOption);

        rootCommand.AddCommand(downloadCommand);

        // Comando: config (debug)
        var configCommand = new Command("config", "Gerenciar configuração");

        var showCommand = new Command("show", "Mostra configuração atual");
        showCommand.SetHandler(async (string? apiUrl, bool forceLocal, bool verbose) =>
        {
            await HandleConfigShowAsync(apiUrl, forceLocal, verbose);
        }, apiOption, localOption, verboseOption);

        var resetCommand = new Command("reset", "Reseta configuração para padrão");
        resetCommand.SetHandler(async () =>
        {
            var configService = new ConfigService();
            await configService.SaveAsync(new AppConfig());
            Console.WriteLine("Configuração resetada para padrão.");
            Console.WriteLine($"Local: {configService.GetConfigPath()}");
        });

        configCommand.AddCommand(showCommand);
        configCommand.AddCommand(resetCommand);
        rootCommand.AddCommand(configCommand);

        // Executar
        return await rootCommand.InvokeAsync(args);
    }

    private static async Task<int> HandleDownloadAsync(
        string url,
        string? output,
        string? start,
        string? end,
        bool audio,
        string? apiUrlFlag,
        bool forceLocal,
        bool verbose)
    {
        // 1. Carregar configuração
        var configService = new ConfigService();
        var config = await configService.LoadAsync();

        // 2. Determinar modo de operação
        var useApi = DetermineMode(apiUrlFlag, forceLocal, config);

        // 3. Criar serviços
        var serviceProvider = BuildServiceProvider(useApi ? config.ApiUrl! : null, config, verbose);

        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
        var downloadService = serviceProvider.GetRequiredService<IDownloadService>();

        // 4. Log do modo
        if (useApi)
        {
            logger.LogInformation("Modo: API Remota ({Url})", config.ApiUrl);
        }
        else
        {
            logger.LogInformation("Modo: Local (Standalone)");
        }

        // 5. Criar request
        var request = CreateDownloadRequest(url, output, start, end, audio, config);

        // 6. Executar download
        var progress = new Progress<Domain.Models.DownloadProgress>(p =>
        {
            Console.Write($"\rProgresso: {p.Percentage:F1}% | " +
                         $"Velocidade: {FormatSpeed(p.Speed)} | " +
                         $"ETA: {p.Eta?.ToString(@"hh\:mm\:ss") ?? "--:--:--"}");
        });

        try
        {
            var result = await downloadService.DownloadAsync(request, progress);

            if (result.IsFailed)
            {
                Console.WriteLine($"\n❌ Erro: {result.Errors.First().Message}");
                return 1;
            }

            Console.WriteLine($"\n✅ Download completado: {result.Value.FilePath}");
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ Erro inesperado: {ex.Message}");
            return 1;
        }
    }

    private static bool DetermineMode(string? apiUrlFlag, bool forceLocal, AppConfig config)
    {
        // Flag --local tem precedência
        if (forceLocal)
        {
            return false;
        }

        // Flag --api-url tem precedência sobre config do arquivo
        if (!string.IsNullOrWhiteSpace(apiUrlFlag))
        {
            return true;
        }

        // Usar config do arquivo
        return config.UseApi;
    }

    private static ServiceProvider BuildServiceProvider(string? apiUrl, AppConfig config, bool verbose)
    {
        var services = new ServiceCollection();

        // Logging
        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(verbose ? LogLevel.Debug : LogLevel.Information);
        });

        // Services
        if (apiUrl is not null)
        {
            // Modo API
            var apiConfig = new AppConfig { ApiUrl = apiUrl };
            services.AddSingleton<IDownloadService>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<ApiClient>>();
                return new ApiClient(apiConfig, logger);
            });
        }
        else
        {
            // Modo local
            services.AddSingleton<IDownloadService, DownloadService>();
            services.AddSingleton<IVideoDownloader, YtDlpDownloader>();
            services.AddSingleton<IVideoProcessor, FfmpegProcessor>();
            services.AddSingleton<IValidationService, ValidationService>();
            services.AddSingleton<IFileSystem, FileSystem>();
        }

        return services.BuildServiceProvider();
    }

    private static Domain.Models.DownloadRequest CreateDownloadRequest(
        string url,
        string? output,
        string? start,
        string? end,
        bool audio,
        AppConfig config)
    {
        Domain.Models.TimeRange? timeRange = null;

        if (!string.IsNullOrEmpty(start) && !string.IsNullOrEmpty(end))
        {
            timeRange = new Domain.Models.TimeRange
            {
                StartSeconds = (int)TimeSpan.Parse(start).TotalSeconds,
                EndSeconds = (int)TimeSpan.Parse(end).TotalSeconds
            };
        }

        return new Domain.Models.DownloadRequest
        {
            Url = url,
            OutputPath = output ?? config.DefaultOutputPath,
            TimeRange = timeRange,
            AudioOnly = audio
        };
    }

    private static async Task HandleConfigShowAsync(string? apiUrlFlag, bool forceLocal, bool verbose)
    {
        var configService = new ConfigService();
        var config = await configService.LoadAsync();

        Console.WriteLine("=== Cutube Configuration ===");
        Console.WriteLine($"Config File: {configService.GetConfigPath()}");
        Console.WriteLine($"Exists: {configService.ConfigExists()}");
        Console.WriteLine();
        Console.WriteLine($"API URL: {config.ApiUrl ?? "(not set - local mode)"}");
        Console.WriteLine($"Default Output: {config.DefaultOutputPath}");
        Console.WriteLine($"Max Concurrent: {config.MaxConcurrentDownloads}");
        Console.WriteLine($"Timeout: {config.TimeoutSeconds}s");
        Console.WriteLine($"Verbose Logging: {config.VerboseLogging}");
        Console.WriteLine();

        // Show effective mode
        var useApi = DetermineMode(apiUrlFlag, forceLocal, config);
        Console.WriteLine($"Effective Mode: {(useApi ? $"API ({config.ApiUrl})" : "Local")}");
    }

    private static string FormatSpeed(double bytesPerSecond)
    {
        if (bytesPerSecond < 1024)
            return $"{bytesPerSecond:F1} B/s";
        if (bytesPerSecond < 1024 * 1024)
            return $"{bytesPerSecond / 1024:F1} KB/s";
        if (bytesPerSecond < 1024 * 1024 * 1024)
            return $"{bytesPerSecond / (1024 * 1024):F1} MB/s";
        return $"{bytesPerSecond / (1024 * 1024 * 1024):F1} GB/s";
    }
}
```

**Checklist:**
- [ ] Adicionar flag --api-url
- [ ] Adicionar flag --local (force local mode)
- [ ] Carregar config do arquivo
- [ ] Determinar modo (API vs Local) com precedência correta
- [ ] Injetar DownloadService ou ApiClient dependendo do modo
- [ ] Adicionar comando "config show" para debug
- [ ] Adicionar comando "config reset"
- [ ] Log do modo sendo usado
- [ ] Testar ambos modos

**Critérios de aceito:**
- ✅ `cutube download <url>` funciona local (sem config)
- ✅ `cutube download --api-url http://localhost:5000 <url>` usa API
- ✅ Config do arquivo é respeitada
- ✅ Flag --local força modo local (ignora config)
- ✅ Comandos de config funcionam

---

### 2.5.4 Testar integração CLI ↔ API

**Estimativa:** 3 horas
**Arquivos:**
```
Cutube.Tests/Integration/
  └── CliApiIntegrationTests.cs
```

**Cenários de teste:**

```csharp
// Cutube.Tests/Integration/CliApiIntegrationTests.cs
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;
using Cutube.Configuration;
using Cutube.Services;
using System.Diagnostics;

namespace Cutube.Tests.Integration;

public class CliApiIntegrationTests : IClassFixture<WebApplicationFactoryFixture>
{
    private readonly ITestOutputHelper _output;
    private readonly WebApplicationFactoryFixture _fixture;
    private readonly string _apiUrl;

    public CliApiIntegrationTests(ITestOutputHelper output, WebApplicationFactoryFixture fixture)
    {
        _output = output;
        _fixture = fixture;
        _apiUrl = fixture.ApiUrl;
    }

    [Fact(Skip = "Requer API rodando")]
    public async Task Cli_ShouldDownloadVideo_Via_Api()
    {
        // Arrange
        var testUrl = "https://www.youtube.com/watch?v=test";
        var config = new AppConfig
        {
            ApiUrl = _apiUrl,
            TimeoutSeconds = 60
        };

        var apiClient = new ApiClient(config, _output.ToLogger<ApiClient>());

        // Act
        var progress = new Progress<Domain.Models.DownloadProgress>();
        var result = await apiClient.DownloadAsync(new Domain.Models.DownloadRequest
        {
            Url = testUrl,
            OutputPath = Path.GetTempPath()
        }, progress);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ConfigService_ShouldSaveAndLoad()
    {
        // Arrange
        var tempConfigPath = Path.Combine(Path.GetTempPath(), "cutube-test-config.json");
        var configService = new ConfigService(tempConfigPath);

        var originalConfig = new AppConfig
        {
            ApiUrl = "http://localhost:5000",
            DefaultOutputPath = "~/Videos",
            MaxConcurrentDownloads = 5
        };

        // Act
        await configService.SaveAsync(originalConfig);
        var loadedConfig = await configService.LoadAsync();

        // Assert
        loadedConfig.ApiUrl.Should().Be(originalConfig.ApiUrl);
        loadedConfig.DefaultOutputPath.Should().Be(originalConfig.DefaultOutputPath);
        loadedConfig.MaxConcurrentDownloads.Should().Be(originalConfig.MaxConcurrentDownloads);

        // Cleanup
        File.Delete(tempConfigPath);
    }

    [Fact]
    public void DetermineMode_ShouldRespectPrecedence()
    {
        // Config com apiUrl
        var config = new AppConfig { ApiUrl = "http://localhost:5000" };

        // Flag --local força modo local
        Program.DetermineMode(null, true, config).Should().BeFalse();

        // Flag --api-url override config
        Program.DetermineMode("http://other:5000", false, config)
            .Should().BeTrue();

        // Sem flags, usa config
        Program.DetermineMode(null, false, config).Should().BeTrue();

        // Config sem apiUrl = modo local
        var localConfig = new AppConfig { ApiUrl = null };
        Program.DetermineMode(null, false, localConfig).Should().BeFalse();
    }
}
```

**Testes manuais (E2E):**

```bash
# Test 1: CLI local (sem API)
cutube download https://youtube.com/watch?v=test

# Test 2: CLI com API
cutube download --api-url http://localhost:5000 https://youtube.com/watch?v=test

# Test 3: CLI com config file
cat > ~/.config/cutube/config.json << EOF
{
  "apiUrl": "http://localhost:5000",
  "defaultOutputPath": "~/Downloads"
}
EOF
cutube download https://youtube.com/watch?v=test

# Test 4: CLI com --local (ignora config)
cutube download --local https://youtube.com/watch?v=test

# Test 5: Config show
cutube config show

# Test 6: Config reset
cutube config reset

# Test 7: API offline (error handling)
cutube download --api-url http://localhost:9999 https://youtube.com/watch?v=test
# Expected: Erro de conexão
```

**Checklist:**
- [ ] Criar testes de integração
- [ ] Testar Cli local (sem API)
- [ ] Testar Cli com API (API rodando)
- [ ] Testar config file (load/save)
- [ ] Testar precedência de flags
- [ ] Testar error handling (API offline, timeout)
- [ ] Testar comando config show/reset
- [ ] Testar cancelamento (CTRL+C)

**Critérios de aceito:**
- ✅ Todos testes passam
- ✅ Ambos modos funcionam (local e API)
- ✅ Config file funciona corretamente
- ✅ Error handling adequado
- ✅ CLI se comporta igual em ambos modos

---

## Qualidade Gates - Fase 2.5

**ANTES de considerar esta fase completa, TODOS os itens abaixo devem ser concluídos:**

- [ ] **dotnet build** - **ZERO warnings** em todos projetos
- [ ] **dotnet test** - **100% pass** em todos projetos
- [ ] CLI local funciona (sem modificação de comportamento)
- [ ] CLI com API funciona (código igual ao local)
- [ ] Config file carrega/salva corretamente
- [ ] Error handling adequado (API offline, timeout, network errors)
- [ ] Documentação atualizada (README com exemplos)
- [ ] Code review aprovado

---

## Cronograma Detalhado

| Tarefa | Estimativa | Dependencies | Blocker |
|--------|-----------|--------------|---------|
| 2.5.1 Sistema de Configuração | 3h | Fase 2.4 | Não |
| 2.5.2 ApiClient para CLI | 4h | 2.5.1 | **Sim** |
| 2.5.3 Modificar Program.cs | 3h | 2.5.1, 2.5.2 | **Sim** |
| 2.5.4 Testes Integração | 3h | 2.5.3 | **Sim** |

**Total:** 13 horas (2-3 dias)

---

## Tecnologias

- **System.CommandLine** - Parsing de argumentos CLI
- **System.Text.Json** - Serialização de config
- **HttpClient** - Chamadas HTTP para API
- **FluentResults** - Result pattern (já existente)
- **Microsoft.Extensions.Logging** - Logging (já existente)
- **Microsoft.Extensions.DependencyInjection** - DI container (já existente)

---

## Arquitetura de Decisão

### Por que não usar SignalR no CLI?

**Decisão:** CLI usa **HTTP polling** para progresso, não SignalR.

**Razões:**
1. **Simplicidade:** CLI não precisa de updates em tempo real < 100ms
2. **Compatibilidade:** HttpClient é padrão do .NET, sem dependências extras
3. **Standalone:** CLI funciona sem SignalR client dependency
4. **Polling é suficiente:** 1-2s de latência é aceitável para CLI

**Trade-offs:**
- ✅ **Pro:** Menos complexidade, sem dependências
- ❌ **Con:** Updates de progresso menos frequentes (1s vs < 100ms)
- ✅ **Pro:** Funciona com qualquer HTTP server (não precisa SignalR)
- ❌ **Con:** Maior carga no servidor (requests a cada 1s)

**Futuro:** Se necessário, adicionar SignalR como opcional.

---

## Troubleshooting Comum

### Problema: Config file não é encontrado

**Sintoma:** `Config show` mostra config padrão (sem apiUrl)

**Causa:** Caminho incorreto para config file

**Solução:**
```bash
# Ver caminho correto para seu OS
# Linux/Mac: ~/.config/cutube/config.json
# Windows: %APPDATA%/cutube/config.json

# Criar manualmente
mkdir -p ~/.config/cutube
cat > ~/.config/cutube/config.json << EOF
{
  "apiUrl": "http://localhost:5000"
}
EOF
```

### Problema: CLI retorna erro de conexão

**Sintoma:** `Erro de conexão com API: Connection refused`

**Causa:** API não está rodando ou URL incorreta

**Solução:**
```bash
# Verificar se API está rodando
curl http://localhost:5000/health

# Ver config
cutube config show

# Forçar modo local (se API offline)
cutube download --local <url>
```

### Problema: Progresso não atualiza

**Sintoma:** CLI mostra "Progresso: 0.0%" por muito tempo

**Causa:** Polling interval muito longo ou API não retornando progresso

**Solução:**
```bash
# Verificar se API está processando download
curl http://localhost:5000/api/downloads/<id>

# Ver logs da API
# Se API usa in-memory repository, pode ter perdido status ao reiniciar
```

---

## Próximos Passos

Após completar Fase 2.5:

1. ✅ **Épico 2 COMPLETO!** 🎉
   - Todas 5 fases concluídas
   - CLI, API, Web funcionando integrados
   - Arquitetura híbrida pronta para Épico 3

2. 🎯 **Code Review Final**
   - Revisar toda arquitetura do Épico 2
   - Validar integração CLI ↔ API
   - Verificar documentação completa

3. 📝 **Documentação de Usuário**
   - Como configurar API remota
   - Exemplos de uso
   - Troubleshooting

4. 🚀 **Épico 3: Sistema Distribuído**
   - RabbitMQ para fila distribuída
   - Workers separados
   - Dashboard de monitoramento

---

## Exemplos de Uso

### Configurar API remota

```bash
# 1. Criar config
cat > ~/.config/cutube/config.json << EOF
{
  "apiUrl": "http://192.168.1.100:5000",
  "defaultOutputPath": "~/Videos/Downloads",
  "maxConcurrentDownloads": 5
}
EOF

# 2. Verificar config
cutube config show

# 3. Usar CLI (agora usa API automaticamente)
cutube download https://youtube.com/watch?v=example
```

### Modo local (forçar)

```bash
# Ignora config da API, usa modo local
cutube download --local https://youtube.com/watch?v=example

# Útil quando API está offline ou para debug
```

### Override temporário de API

```bash
# Usa API específica (não salva na config)
cutube download --api-url http://localhost:5000 https://youtube.com/watch?v=example

# Útil para testar múltiplas APIs
```

### Debug verbose

```bash
# Mostra logs detalhados
cutube download --verbose https://youtube.com/watch?v=example
```

---

## Documentação de Referência

### HttpClient Timeout

```csharp
// Configurar timeout para evitar hangs
_httpClient.Timeout = TimeSpan.FromSeconds(config.TimeoutSeconds);
```

### Polling Strategy

```csharp
// Poll a cada 1s (balance entre carga e responsividade)
while (!ct.IsCancellationRequested)
{
    await Task.Delay(1000, ct);
    // Buscar status...
}
```

### Error Handling

```csharp
// Tratar erros de rede
try
{
    var response = await _httpClient.GetAsync(...);
}
catch (HttpRequestException ex)
{
    // API offline ou erro de rede
    return Result.Fail($"Erro de conexão: {ex.Message}");
}
catch (TaskCanceledException ex)
{
    // Timeout
    return Result.Fail("Timeout ao conectar com API");
}
```

---

**Fim do Plano - Fase 2.5**
