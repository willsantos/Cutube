# Sistema de Feature Toggle

**Status:** 🎯 Planejamento
**Épico:** Cutube-TBD (Infraestrutura)
**Duração:** 1-2 dias
**Responsável:** Backend Developer
**Prioridade:** 🔥 Média
**Dependência:** Nenhuma

---

## Objetivo

Implementar um sistema de **Feature Toggle** reutilizável no projeto Cutube.Api para permitir habilitar/desabilitar funcionalidades em runtime sem necessidade de redeploy.

**Benefícios:**
- ✅ Deploy safe - lançar features desabilitadas e habilitar gradualmente
- ✅ Rollback instantâneo - desabilitar feature problemática sem redeploy
- ✅ Testing em produção - testar com subset de usuários
- ✅ Configuração centralizada - todas as features em um lugar
- ✅ Reutilizável - padrão consistente para toda aplicação

---

## Casos de Uso Iniciais

1. **QueueFallbackEnabled** (Fase 3.2) - Fallback para processamento local se RabbitMQ indisponível
2. **NewDownloadEndpoint** (Futuro) - Nova versão de endpoint de download
3. **AdvancedValidation** (Futuro) - Validações extras em uploads

---

## Visão Arquitetural

### Fluxo de Decisão

```
┌─────────────────────────────────────────────────────────────────────────┐
│                          Request HTTP                                   │
└────────────────────────────┬────────────────────────────────────────────┘
                             │
┌────────────────────────────▼────────────────────────────────────────────┐
│                         API Endpoint                                    │
│                                                                         │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │                    IFeatureToggleService                        │   │
│  │  • IsEnabled("FeatureName")                                     │   │
│  │  • IsEnabledForUser("FeatureName", userId)                      │   │
│  └────────────────────────┬────────────────────────────────────────┘   │
│                           │                                              │
│                           │ Check feature toggle                         │
┌───────────────────────────▼──────────────────────────────────────────────┐
│                    Configuration Source                                  │
│                                                                         │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │  • appsettings.json (base)                                      │   │
│  │  • appsettings.{Environment}.json (override)                    │   │
│  │  • Environment Variables (Docker/K8s)                           │   │
│  │  • Future: Azure App Configuration / Consul                    │   │
│  └────────────────────────┬────────────────────────────────────────┘   │
└───────────────────────────┼──────────────────────────────────────────────┘
                            │
┌───────────────────────────▼──────────────────────────────────────────────┐
│                         Decision                                         │
│                                                                         │
│     ┌─────────────────────┐          ┌─────────────────────┐           │
│     │ Feature Enabled     │          │ Feature Disabled    │           │
│     │ (true)              │          │ (false)             │           │
│     └──────────┬──────────┘          └──────────┬──────────┘           │
│                │                                │                        │
│                ▼                                ▼                        │
│     ┌─────────────────────┐          ┌─────────────────────┐           │
│     │ Execute Feature     │          │ Execute Default     │           │
│     │ New Behavior        │          │ Old Behavior        │           │
│     └─────────────────────┘          └─────────────────────┘           │
└─────────────────────────────────────────────────────────────────────────┘
```

### Tipos de Feature Toggles

1. **Boolean Toggle** - Simples ligado/desligado
2. **Percentage Toggle** - Habilitar para X% dos usuários
3. **Whitelist Toggle** - Habilitar apenas para usuários específicos
4. **Timebound Toggle** - Habilitar automaticamente em data/hora específica

**Fase 1:** Implementar Boolean Toggle (suficiente para QueueFallback)

---

## Tarefas

### 1.1 Criar Interface e Serviço de Feature Toggle

**Estimativa:** 2-3 horas

**Arquivos:**
```
src/Cutube.Api/
  ├── Features/
  │   ├── IFeatureToggleService.cs
  │   ├── FeatureToggleService.cs
  │   └── FeatureFlag.cs (enum)
```

#### 1.1.1 Criar FeatureFlag Enum

```csharp
// src/Cutube.Api/Features/FeatureFlag.cs
namespace Cutube.Api.Features;

/// <summary>
/// Flags de features disponíveis no sistema.
/// </summary>
public static class FeatureFlag
{
    /// <summary>
    /// Habilita fallback para processamento local quando RabbitMQ está indisponível.
    /// Padrão: false.
    /// </summary>
    public const string QueueFallbackEnabled = "QueueFallbackEnabled";

    /// <summary>
    /// Habilita nova versão do endpoint de download.
    /// Padrão: false.
    /// </summary>
    public const string NewDownloadEndpoint = "NewDownloadEndpoint";

    /// <summary>
    /// Habilita validações avançadas em uploads.
    /// Padrão: false.
    /// </summary>
    public const string AdvancedValidation = "AdvancedValidation";
}
```

#### 1.1.2 Criar Interface IFeatureToggleService

```csharp
// src/Cutube.Api/Features/IFeatureToggleService.cs
namespace Cutube.Api.Features;

/// <summary>
/// Serviço para verificar estado de feature toggles.
/// </summary>
public interface IFeatureToggleService
{
    /// <summary>
    /// Verifica se uma feature está habilitada.
    /// </summary>
    /// <param name="featureName">Nome da feature.</param>
    /// <returns>True se habilitada, false caso contrário.</returns>
    bool IsEnabled(string featureName);

    /// <summary>
    /// Verifica se uma feature está habilitada para um usuário específico.
    /// (Futuro - para whitelist/percentage toggles)
    /// </summary>
    /// <param name="featureName">Nome da feature.</param>
    /// <param name="userId">ID do usuário.</param>
    /// <returns>True se habilitada para o usuário, false caso contrário.</returns>
    bool IsEnabledForUser(string featureName, string userId);
}
```

#### 1.1.3 Criar FeatureToggleService

```csharp
// src/Cutube.Api/Features/FeatureToggleService.cs
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cutube.Api.Features;

/// <summary>
/// Implementação de verificação de feature toggles via configuração.
/// </summary>
public class FeatureToggleService : IFeatureToggleService
{
    private readonly FeatureTogglesOptions _options;
    private readonly ILogger<FeatureToggleService> _logger;

    public FeatureToggleService(
        IOptions<FeatureTogglesOptions> options,
        ILogger<FeatureToggleService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc/>
    public bool IsEnabled(string featureName)
    {
        if (string.IsNullOrWhiteSpace(featureName))
        {
            _logger.LogWarning("Feature name is null or empty");
            return false;
        }

        // Usar reflection para acessar propriedade dinamicamente
        var property = typeof(FeatureTogglesOptions).GetProperty(featureName);

        if (property == null)
        {
            _logger.LogWarning("Feature '{FeatureName}' not found in configuration", featureName);
            return false;
        }

        var value = (bool?)(property.GetValue(_options) ?? false);

        _logger.LogDebug("Feature '{FeatureName}' is {State}", featureName, value ? "ENABLED" : "DISABLED");

        return value;
    }

    /// <inheritdoc/>
    public bool IsEnabledForUser(string featureName, string userId)
    {
        // Fase 1: Não implementado - sempre retorna IsEnabled()
        // Fase 2: Implementar whitelist/percentage logic
        _logger.LogDebug(
            "User-specific feature checks not implemented yet. Checking global state for '{FeatureName}'",
            featureName);

        return IsEnabled(featureName);
    }
}
```

**Checklist:**
- [ ] Criar diretório src/Cutube.Api/Features/
- [ ] Criar FeatureFlag.cs com constantes
- [ ] Criar interface IFeatureToggleService.cs
- [ ] Criar FeatureToggleService.cs com verificação via reflection
- [ ] Adicionar logging para debug

**Critérios de aceito:**
- ✅ Interface criada com método IsEnabled
- ✅ FeatureToggleService implementa verificação via reflection
- ✅ FeatureFlag enum com constantes centralizadas
- ✅ Logging de estado de features

---

### 1.2 Criar Configuration Options

**Estimativa:** 1 hora

**Arquivos:**
```
src/Cutube.Api/
  └── Configuration/
      └── FeatureTogglesOptions.cs
```

```csharp
// src/Cutube.Api/Configuration/FeatureTogglesOptions.cs
namespace Cutube.Api.Configuration;

/// <summary>
/// Opções de configuração para feature toggles.
/// Propriedades são mapeadas dinamicamente pelo nome da feature.
/// </summary>
public class FeatureTogglesOptions
{
    public const string SectionName = "FeatureToggles";

    /// <summary>
    /// Habilita fallback para processamento local quando RabbitMQ está indisponível.
    /// Padrão: false (retorna 503 ao invés de fazer fallback).
    /// </summary>
    public bool QueueFallbackEnabled { get; set; } = false;

    /// <summary>
    /// Habilita nova versão do endpoint de download.
    /// Padrão: false.
    /// </summary>
    public bool NewDownloadEndpoint { get; set; } = false;

    /// <summary>
    /// Habilita validações avançadas em uploads.
    /// Padrão: false.
    /// </summary>
    public bool AdvancedValidation { get; set; } = false;
}
```

**Checklist:**
- [ ] Criar FeatureTogglesOptions.cs
- [ ] Definir SectionName como "FeatureToggles"
- [ ] Adicionar propriedades para features iniciais
- [ ] Documentar padrão (false) para cada feature

**Critérios de aceito:**
- ✅ Options class criada
- ✅ Todas as features têm propriedade
- ✅ Padrão é false para todas

---

### 1.3 Configurar DI e Settings

**Estimativa:** 1-2 horas

**Arquivos a modificar:**
```
src/Cutube.Api/
  ├── Program.cs (modificar)
  └── appsettings.json (modificar)
  └── appsettings.Development.json (criar)
```

#### 1.3.1 Configurar Program.cs

```csharp
// src/Cutube.Api/Program.cs
using Cutube.Api.Configuration;
using Cutube.Api.Features;

var builder = WebApplication.CreateBuilder(args);

// ... outras configurações ...

// Feature Toggles Configuration
builder.Services.Configure<FeatureTogglesOptions>(
    builder.Configuration.GetSection(FeatureTogglesOptions.SectionName)
);

// Registrar Feature Toggle Service
builder.Services.AddSingleton<IFeatureToggleService, FeatureToggleService>();

// Log estado das features no startup
var app = builder.Build();
var featureToggleService = app.Services.GetRequiredService<IFeatureToggleService>();

// Log features no startup
logger.LogInformation("=== Feature Toggles State ===");
logger.LogInformation("QueueFallbackEnabled: {State}", featureToggleService.IsEnabled(FeatureFlag.QueueFallbackEnabled));
logger.LogInformation("NewDownloadEndpoint: {State}", featureToggleService.IsEnabled(FeatureFlag.NewDownloadEndpoint));
logger.LogInformation("AdvancedValidation: {State}", featureToggleService.IsEnabled(FeatureFlag.AdvancedValidation));
logger.LogInformation("============================");

// ... resto do código ...
```

#### 1.3.2 Configurar appsettings.json

```json
// src/Cutube.Api/appsettings.json
{
  "FeatureToggles": {
    "QueueFallbackEnabled": false,
    "NewDownloadEndpoint": false,
    "AdvancedValidation": false
  }
}
```

#### 1.3.3 Configurar appsettings.Development.json

```json
// src/Cutube.Api/appsettings.Development.json
{
  "FeatureToggles": {
    "QueueFallbackEnabled": true,
    "NewDownloadEndpoint": false,
    "AdvancedValidation": false
  }
}
```

**Checklist:**
- [ ] Adicionar configuração de FeatureTogglesOptions no Program.cs
- [ ] Registrar IFeatureToggleService como singleton
- [ ] Adicionar logging de estado das features no startup
- [ ] Criar seção FeatureToggles no appsettings.json
- [ ] Criar appsettings.Development.json com overrides
- [ ] Testar startup da API sem erros

**Critérios de aceito:**
- ✅ API inicia sem erros
- ✅ IFeatureToggleService injetável no DI
- ✅ Estado das features logado no startup
- ✅ Configuração funciona em diferentes ambientes

---

### 1.4 Criar Extension Methods para Uso Simplificado

**Estimativa:** 1-2 horas

**Arquivos:**
```
src/Cutube.Api/
  └── Features/
      └── FeatureToggleExtensions.cs
```

```csharp
// src/Cutube.Api/Features/FeatureToggleExtensions.cs
namespace Cutube.Api.Features;

/// <summary>
/// Extension methods para uso simplificado de feature toggles.
/// </summary>
public static class FeatureToggleExtensions
{
    /// <summary>
    /// Executa ação se feature está habilitada.
    /// </summary>
    public static void ExecuteIfEnabled(
        this IFeatureToggleService service,
        string featureName,
        Action action)
    {
        if (service.IsEnabled(featureName))
        {
            action();
        }
    }

    /// <summary>
    /// Executa ação se feature está desabilitada.
    /// </summary>
    public static void ExecuteIfDisabled(
        this IFeatureToggleService service,
        string featureName,
        Action action)
    {
        if (!service.IsEnabled(featureName))
        {
            action();
        }
    }

    /// <summary>
    /// Executa ação diferente baseado no estado da feature.
    /// </summary>
    public static void ExecuteBranch(
        this IFeatureToggleService service,
        string featureName,
        Action ifEnabled,
        Action ifDisabled)
    {
        if (service.IsEnabled(featureName))
        {
            ifEnabled();
        }
        else
        {
            ifDisabled();
        }
    }

    /// <summary>
    /// Retorna valor diferente baseado no estado da feature.
    /// </summary>
    public static T GetValue<T>(
        this IFeatureToggleService service,
        string featureName,
        T ifEnabled,
        T ifDisabled)
    {
        return service.IsEnabled(featureName) ? ifEnabled : ifDisabled;
    }
}
```

**Exemplo de Uso:**

```csharp
// Em qualquer service/controller
public class MyService
{
    private readonly IFeatureToggleService _featureToggle;

    public MyService(IFeatureToggleService featureToggle)
    {
        _featureToggle = featureToggle;
    }

    public void Process()
    {
        // Usando extension method
        _featureToggle.ExecuteIfEnabled(
            FeatureFlag.QueueFallbackEnabled,
            () => UseNewBehavior()
        );

        // Usando branch
        _featureToggle.ExecuteBranch(
            FeatureFlag.NewDownloadEndpoint,
            () => NewEndpoint(),
            () => OldEndpoint()
        );

        // Usando valor
        var mode = _featureToggle.GetValue(
            FeatureFlag.AdvancedValidation,
            "advanced",
            "standard"
        );
    }
}
```

**Checklist:**
- [ ] Criar FeatureToggleExtensions.cs
- [ ] Implementar ExecuteIfEnabled
- [ ] Implementar ExecuteIfDisabled
- [ ] Implementar ExecuteBranch
- [ ] Implementar GetValue
- [ ] Adicionar XML documentation
- [ ] Adicionar exemplos de uso

**Critérios de aceito:**
- ✅ Extension methods criados
- ✅ Sintaxe fluente e intuitiva
- ✅ Exemplos de uso documentados

---

### 1.5 Testes e Documentação

**Estimativa:** 2-3 horas

**Arquivos:**
```
tests/Cutube.Api.Tests/Unit/Features/
  ├── FeatureToggleServiceTests.cs
  └── FeatureToggleExtensionsTests.cs
docs/
  └── feature-toggle.md
```

#### 1.5.1 Testes Unitários

```csharp
// tests/Cutube.Api.Tests/Unit/Features/FeatureToggleServiceTests.cs
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Cutube.Api.Features;
using Cutube.Api.Configuration;

namespace Cutube.Api.Tests.Unit.Features;

public class FeatureToggleServiceTests
{
    [Fact]
    public void IsEnabled_WithFeatureEnabled_ReturnsTrue()
    {
        // Arrange
        var options = Options.Create(new FeatureTogglesOptions
        {
            QueueFallbackEnabled = true
        });
        var logger = new Mock<ILogger<FeatureToggleService>>().Object;

        var service = new FeatureToggleService(options, logger);

        // Act
        var result = service.IsEnabled(FeatureFlag.QueueFallbackEnabled);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsEnabled_WithFeatureDisabled_ReturnsFalse()
    {
        // Arrange
        var options = Options.Create(new FeatureTogglesOptions
        {
            QueueFallbackEnabled = false
        });
        var logger = new Mock<ILogger<FeatureToggleService>>().Object;

        var service = new FeatureToggleService(options, logger);

        // Act
        var result = service.IsEnabled(FeatureFlag.QueueFallbackEnabled);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsEnabled_WithInvalidFeatureName_ReturnsFalse()
    {
        // Arrange
        var options = Options.Create(new FeatureTogglesOptions());
        var logger = new Mock<ILogger<FeatureToggleService>>().Object;

        var service = new FeatureToggleService(options, logger);

        // Act
        var result = service.IsEnabled("NonExistentFeature");

        // Assert
        result.Should().BeFalse();
    }
}
```

#### 1.5.2 Documentação

```markdown
# Sistema de Feature Toggle

## Visão Geral

Sistema de feature toggle para habilitar/desabilitar funcionalidades em runtime.

## Uso Básico

### 1. Definir Feature Flag

```csharp
// src/Cutube.Api/Features/FeatureFlag.cs
public static class FeatureFlag
{
    public const string MyFeature = "MyFeature";
}
```

### 2. Adicionar Propriedade em Options

```csharp
// src/Cutube.Api/Configuration/FeatureTogglesOptions.cs
public bool MyFeature { get; set; } = false;
```

### 3. Configurar em appsettings.json

```json
{
  "FeatureToggles": {
    "MyFeature": false
  }
}
```

### 4. Usar no Código

```csharp
public class MyService
{
    private readonly IFeatureToggleService _featureToggle;

    public MyService(IFeatureToggleService featureToggle)
    {
        _featureToggle = featureToggle;
    }

    public void Process()
    {
        if (_featureToggle.IsEnabled(FeatureFlag.MyFeature))
        {
            // Nova funcionalidade
        }
        else
        {
            // Comportamento padrão
        }
    }
}
```

## Habilitar Feature em Runtime

### Via Environment Variable (Docker/K8s)
```bash
export FeatureToggles__MyFeature=true
```

### Via appsettings.{Environment}.json
```json
{
  "FeatureToggles": {
    "MyFeature": true
  }
}
```

## Best Practices

1. **Default false** - Features devem ser desabilitadas por padrão
2. **Remove after use** - Remover flags após feature ser fully released
3. **Document expiry** - Documentar quando feature será removida
4. **Test both paths** - Testar código com feature enabled/disabled
5. **Monitor** - Monitorar impacto de features quando habilitadas

## Features Atuais

| Feature | Descrição | Padrão |
|---------|----------|--------|
| QueueFallbackEnabled | Fallback para processamento local | false |
| NewDownloadEndpoint | Nova versão do endpoint de download | false |
| AdvancedValidation | Validações avançadas em uploads | false |
```

**Checklist:**
- [ ] Criar testes unitários para FeatureToggleService
- [ ] Criar testes para extension methods
- [ ] Criar docs/feature-toggle.md
- [ ] Adicionar exemplos de uso
- [ ] Atualizar README principal

**Critérios de aceito:**
- ✅ Testes unitários passam
- ✅ Cobertura > 80% para FeatureToggleService
- ✅ Documentação clara e completa
- ✅ README atualizado

---

## Qualidade Gates - Sistema de Feature Toggle

**ANTES de considerar este sistema completo, TODOS os itens abaixo devem ser verdadeiros:**

- [ ] **dotnet build** - **0 warnings** (build limpo)
- [ ] **dotnet test** - **100% pass** (todos os testes)
- [ ] **FeatureToggleService** registrado no DI
- [ ] **appsettings.json** configurado com todas as features
- [ ] **Logging de estado** no startup funcionando
- [ ] **Extension methods** funcionando
- [ ] **Testes manuais** executados com sucesso (toggle ON/OFF)

---

## Cronograma Detalhado

| Tarefa | Estimativa | Dependencies | Blocker |
|--------|-----------|--------------|---------|
| 1.1 Interface e Serviço | 2-3h | Nenhuma | Não |
| 1.2 Configuration Options | 1h | 1.1 | **Sim** |
| 1.3 DI e Settings | 1-2h | 1.1, 1.2 | **Sim** |
| 1.4 Extension Methods | 1-2h | 1.1 | **Sim** |
| 1.5 Testes e Docs | 2-3h | 1.1, 1.4 | **Sim** |

**Total:** 7-11 horas (~1-2 dias)

---

## Tecnologias

- **.NET 10** - C#, Reflection, DI
- **Microsoft.Extensions.Options** - Configuration pattern
- **xUnit** - Testes unitários
- **FluentAssertions** - Asserts

---

## Riscos e Mitigações

| Risco | Impacto | Mitigação |
|-------|---------|-----------|
| Performance impact de reflection | Baixo | Cache de PropertyInfo, measurements |
| Config drift entre ambientes | Médio | Documentação clara, validação no startup |
| Feature flags esquecidas (zombie flags) | Médio | Code review, documentação de expiry |

---

## Próximos Passos

Após completar Sistema de Feature Toggle:

1. **Fase 3.2** - Usar QueueFallbackEnabled
2. Implementar percentage toggles (Fase 2)
3. Implementar whitelist toggles (Fase 2)
4. Implementar timebound toggles (Fase 2)
5. Integração com Azure App Configuration (Fase 3)

---

## Entregáveis (Deliverables)

- [ ] IFeatureToggleService interface criada
- [ ] FeatureToggleService implementado com reflection
- [ ] FeatureFlag enum com constantes
- [ ] FeatureTogglesOptions configurada
- [ ] Extension methods para uso simplificado
- [ ] Testes unitários criados
- [ ] Documentação completa
- [ ] Exemplos de uso

---

**Fim do Plano - Sistema de Feature Toggle**
