# Fase 6.3: Namespace Migration (Migrar Namespaces)

**Status:** 🎯 Planejamento  
**Épico:** Cutube-vxo (Épico 6: Restructure Monorepo)  
**Duração:** 1-2 horas  
**Responsável:** Backend / Fullstack  
**Prioridade:** 🔥 Alta  
**Dependência:** ✅ Fase 6.2 (Project Moves)

---

## Objetivo

Migrar todos os namespaces do projeto `Cutube.Cli` de `Cutube.*` para `Cutube.Cli.*` para refletir a nova estrutura de diretórios e garantir que o código compile corretamente após os movimentos da Fase 6.2.

**Benefícios:**
- ✅ **Consistência**: Namespaces refletem estrutura física de diretórios
- ✅ **Compilação**: Código compila sem erros de namespace
- ✅ **Organização**: Separação clara entre domínios (Api, Cli, Worker, Domain)
- ✅ **Manutenibilidade**: Namespaces hierárquicos facilitam localização de código

---

## Visão Arquitetural

### Namespace Atual vs Futuro

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    MIGRAÇÃO DE NAMESPACES                           │
└─────────────────────────────────────────────────────────────────────────┘

ANTES (Fase 6.2):                    DEPOIS (Fase 6.3):
namespace Cutube                     → namespace Cutube.Cli
  namespace Configuration            →   namespace Configuration
  namespace ErrorHandling            →   namespace ErrorHandling
  namespace Infrastructure            →   namespace Infrastructure
  namespace Logging                   →   namespace Logging
  namespace Recovery                  →   namespace Recovery
  namespace Validation                →   namespace Validation
  
namespace YoutubeDLSharp             → namespace YoutubeDLSharp (externa, não muda)
```

### Hierarquia de Namespaces

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    NOVA HIERARQUIA DE NAMESPACES                      │
└─────────────────────────────────────────────────────────────────────────┘

Cutube (raiz)
├── Cutube.Domain          (projeto src/Cutube.Domain/)
│   ├── Models
│   ├── Interfaces
│   └── Services
│
├── Cutube.Cli             (projeto src/Cutube.Cli/) ← FOCO DESTA FASE
│   ├── Configuration
│   ├── ErrorHandling
│   ├── Infrastructure
│   ├── Logging
│   ├── Recovery
│   └── Validation
│
├── Cutube.Api             (projeto src/Cutube.Api/)
│   ├── Controllers
│   ├── Services
│   └── Middleware
│
└── Cutube.Worker          (projeto src/Cutube.Worker/)
    ├── Services
    └── Handlers
```

---

## Problemas Atuais

### Identificados na Fase 6.2

**Erro de Compilação:**
```
error CS0234: The type or namespace name 'Cli' does not exist in the namespace 'Cutube'
```

**Causa Raiz:**
1. `RootNamespace` em `Cutube.Cli.csproj` foi configurado corretamente ✅
2. Mas arquivos `.cs` ainda declaram `namespace Cutube` (antigo)
3. Sub-namespaces não seguem hierarquia (ex: `Cutube.ErrorHandling`)

**Arquivos Afetados:**
- Todos os arquivos `.cs` em `src/Cutube.Cli/` (~50 arquivos)
- Arquivos de teste em `tests/Cutube.Cli.Tests/` (~24 arquivos)

---

## Estratégia de Migração

### Abordagem: Find & Replace Systemático

**Passo 1:** Migrar declarações de namespace raiz
```bash
# Encontrar todos os namespaces Cutube (sem sub-namespace)
find src/Cutube.Cli -name "*.cs" -exec grep -l "^namespace Cutube$" {} \;
```

**Passo 2:** Migrar sub-namespaces
```bash
# Exemplo: namespace Cutube.ErrorHandling → namespace Cutube.Cli.ErrorHandling
find src/Cutube.Cli -name "*.cs" -exec sed -i 's/namespace Cutube\.ErrorHandling/namespace Cutube.Cli.ErrorHandling/g' {} \;
```

**Passo 3:** Atualizar using statements
```bash
# Em arquivos de teste e outros projetos
find . -name "*.cs" -exec sed -i 's/using Cutube\.Configuration/using Cutube.Cli.Configuration/g' {} \;
```

---

## Tarefas

---

### 6.3.1 Migrar Namespace Raiz do CLI

**Estimativa:** 20 min

**Arquivos:**
```
src/Cutube.Cli/
├── Program.cs
├── Menu.cs
├── ProgressBar.cs
├── ValidationHelper.cs
├── TimeHelper.cs
├── TitleHelper.cs
├── DomainWorkflow.cs
├── EnvironmentService.cs
├── FileService.cs
├── HttpClientService.cs
├── ProcessService.cs
├── FfmpegHelper.cs
└── YtDlpHelper.cs
```

**Implementação:**

```bash
# Encontrar arquivos com namespace Cutube (raiz)
find src/Cutube.Cli -name "*.cs" -exec grep -l "^namespace Cutube$" {} \;

# Replace namespace Cutube → Cutube.Cli
find src/Cutube.Cli -name "*.cs" -exec sed -i 's/^namespace Cutube$/namespace Cutube.Cli/g' {} \;

# Validar mudanças
git diff src/Cutube.Cli/ | grep "namespace"
```

**Antes:**
```csharp
namespace Cutube
{
    public class Program
    {
        // ...
    }
}
```

**Depois:**
```csharp
namespace Cutube.Cli
{
    public class Program
    {
        // ...
    }
}
```

**Checklist:**
- [ ] Todos os arquivos em `src/Cutube.Cli/` com `namespace Cutube` migrados
- [ ] Git diff mostra mudanças corretas
- [ ] Nenhum arquivo deixado para trás

**Critérios de aceite:**
- ✅ Declarações de namespace raiz atualizadas

---

### 6.3.2 Migrar Sub-Namespaces: Configuration

**Estimativa:** 10 min

**Arquivos:**
```
src/Cutube.Cli/Configuration/
├── AppConfig.cs
├── ConfigDefaults.cs
└── ConfigService.cs
```

**Implementação:**

```bash
# Replace namespace Cutube.Configuration → Cutube.Cli.Configuration
find src/Cutube.Cli/Configuration -name "*.cs" -exec sed -i 's/namespace Cutube\.Configuration/namespace Cutube.Cli.Configuration/g' {} \;
```

**Antes:**
```csharp
namespace Cutube.Configuration
{
    public class AppConfig
    {
        // ...
    }
}
```

**Depois:**
```csharp
namespace Cutube.Cli.Configuration
{
    public class AppConfig
    {
        // ...
    }
}
```

**Checklist:**
- [ ] Todos os arquivos em Configuration/ migrados
- [ ] Namespace atualizado para `Cutube.Cli.Configuration`

**Critérios de aceite:**
- ✅ Namespace Configuration migrado

---

### 6.3.3 Migrar Sub-Namespaces: ErrorHandling

**Estimativa:** 10 min

**Arquivos:**
```
src/Cutube.Cli/ErrorHandling/
├── ErrorHandler.cs
├── DefaultErrorHandler.cs
├── IErrorHandler.cs
├── ErrorType.cs
├── Result.cs
├── ResultExtensions.cs
└── RetryPolicy.cs
```

**Implementação:**

```bash
# Replace namespace Cutube.ErrorHandling → Cutube.Cli.ErrorHandling
find src/Cutube.Cli/ErrorHandling -name "*.cs" -exec sed -i 's/namespace Cutube\.ErrorHandling/namespace Cutube.Cli.ErrorHandling/g' {} \;
```

**Checklist:**
- [ ] Todos os arquivos em ErrorHandling/ migrados

**Critérios de aceite:**
- ✅ Namespace ErrorHandling migrado

---

### 6.3.4 Migrar Sub-Namespaces: Infrastructure

**Estimativa:** 10 min

**Arquivos:**
```
src/Cutube.Cli/Infrastructure/
├── FfmpegProcessor.cs
├── FileSystem.cs
├── YtDlpDownloader.cs
├── YtDlpMetadataProvider.cs
└── YtDlpPathResolver.cs
```

**Implementação:**

```bash
# Replace namespace Cutube.Infrastructure → Cutube.Cli.Infrastructure
find src/Cutube.Cli/Infrastructure -name "*.cs" -exec sed -i 's/namespace Cutube\.Infrastructure/namespace Cutube.Cli.Infrastructure/g' {} \;
```

**Checklist:**
- [ ] Todos os arquivos em Infrastructure/ migrados

**Critérios de aceite:**
- ✅ Namespace Infrastructure migrado

---

### 6.3.5 Migrar Sub-Namespaces: Logging

**Estimativa:** 10 min

**Arquivos:**
```
src/Cutube.Cli/Logging/
├── FileLoggerService.cs
├── ILoggerService.cs
├── ILogEntry.cs
├── LogEntry.cs
└── LogLevel.cs
```

**Implementação:**

```bash
# Replace namespace Cutube.Logging → Cutube.Cli.Logging
find src/Cutube.Cli/Logging -name "*.cs" -exec sed -i 's/namespace Cutube\.Logging/namespace Cutube.Cli.Logging/g' {} \;
```

**Checklist:**
- [ ] Todos os arquivos em Logging/ migrados

**Critérios de aceite:**
- ✅ Namespace Logging migrado

---

### 6.3.6 Migrar Sub-Namespaces: Recovery

**Estimativa:** 10 min

**Arquivos:**
```
src/Cutube.Cli/Recovery/
├── DownloadStateManager.cs
├── IDownloadStateManager.cs
├── DownloadState.cs
├── DownloadStatus.cs
├── DownloadInput.cs
└── StateCleanupService.cs
```

**Implementação:**

```bash
# Replace namespace Cutube.Recovery → Cutube.Cli.Recovery
find src/Cutube.Cli/Recovery -name "*.cs" -exec sed -i 's/namespace Cutube\.Recovery/namespace Cutube.Cli.Recovery/g' {} \;
```

**Checklist:**
- [ ] Todos os arquivos em Recovery/ migrados

**Critérios de aceite:**
- ✅ Namespace Recovery migrado

---

### 6.3.7 Migrar Sub-Namespaces: Validation

**Estimativa:** 5 min

**Arquivos:**
```
src/Cutube.Cli/Validation/
└── InteractiveValidator.cs
```

**Implementação:**

```bash
# Replace namespace Cutube.Validation → Cutube.Cli.Validation
find src/Cutube.Cli/Validation -name "*.cs" -exec sed -i 's/namespace Cutube\.Validation/namespace Cutube.Cli.Validation/g' {} \;
```

**Checklist:**
- [ ] Arquivo em Validation/ migrado

**Critérios de aceite:**
- ✅ Namespace Validation migrado

---

### 6.3.8 Atualizar Using Statements em Outros Projetos

**Estimativa:** 20 min

**Projetos Afetados:**
```
src/Cutube.Api/Cutube.Api.csproj
src/Cutube.Worker/Cutube.Worker.csproj
tests/Cutube.Cli.Tests/Cutube.Tests.csproj
```

**Implementação:**

```bash
# Em Cutube.Api (se houver referências a CLI)
find src/Cutube.Api -name "*.cs" -exec sed -i 's/using Cutube\.Configuration/using Cutube.Cli.Configuration/g' {} \;
find src/Cutube.Api -name "*.cs" -exec sed -i 's/using Cutube\.ErrorHandling/using Cutube.Cli.ErrorHandling/g' {} \;

# Em Cutube.Worker (se houver referências a CLI)
find src/Cutube.Worker -name "*.cs" -exec sed -i 's/using Cutube\.Configuration/using Cutube.Cli.Configuration/g' {} \;

# Em testes (já parcialmente feito na Fase 6.2, verificar restantes)
find tests/Cutube.Cli.Tests -name "*.cs" -exec sed -i 's/using Cutube\.Configuration/using Cutube.Cli.Configuration/g' {} \;
find tests/Cutube.Cli.Tests -name "*.cs" -exec sed -i 's/using Cutube\.ErrorHandling/using Cutube.Cli.ErrorHandling/g' {} \;
```

**Checklist:**
- [ ] Using statements atualizados em todos os projetos
- [ ] Nenhum `using Cutube.Configuration` restante (deve ser `Cutube.Cli.Configuration`)

**Critérios de aceite:**
- ✅ Using statements apontam para namespaces corretos

---

### 6.3.9 Validar Compilação

**Estimativa:** 10 min

**Implementação:**

```bash
# Build do projeto CLI
dotnet build src/Cutube.Cli/cutube.csproj

# Build dos testes CLI
dotnet build tests/Cutube.Cli.Tests/Cutube.Tests.csproj

# Build da solução completa
dotnet build cutube.sln

# Rodar testes
dotnet test tests/Cutube.Cli.Tests/Cutube.Tests.csproj
```

**Checklist:**
- [ ] Build CLI: 0 warnings, 0 errors
- [ ] Build Tests: 0 warnings, 0 errors
- [ ] Build Solution: 0 warnings, 0 errors
- [ ] Testes CLI passam
- [ ] Nenhum erro de namespace

**Critérios de aceite:**
- ✅ Build completo sem erros
- ✅ Testes passando

---

## Checklist de Fase

### Implementação
- [ ] Namespace raiz `Cutube` migrado para `Cutube.Cli`
- [ ] Sub-namespace `Configuration` migrado
- [ ] Sub-namespace `ErrorHandling` migrado
- [ ] Sub-namespace `Infrastructure` migrado
- [ ] Sub-namespace `Logging` migrado
- [ ] Sub-namespace `Recovery` migrado
- [ ] Sub-namespace `Validation` migrado
- [ ] Using statements atualizados em outros projetos

### Validação
- [ ] `dotnet build src/Cutube.Cli/cutube.csproj` passes
- [ ] `dotnet build tests/Cutube.Cli.Tests/Cutube.Tests.csproj` passes
- [ ] `dotnet build cutube.sln` passes
- [ ] `dotnet test` passes
- [ ] 0 warnings
- [ ] 0 errors

---

## Notas

- **Ordem Importa**: Migrar namespaces raiz primeiro, depois sub-namespaces
- **Busca e Substituição**: Usar `sed` com cuidado - sempre validar com `git diff` antes de commitar
- **Preservar Código**: Apenas mudar declarações de namespace, NÃO alterar lógica
- **Testes Importantes**: Rodar `dotnet test` após cada sub-namespace para detectar problemas cedo

---

## Riscos e Mitigações

| Risco | Probabilidade | Impacto | Mitigação |
|-------|---------------|---------|-----------|
| Using statements quebrados em outros projetos | Média | Alto | Validar build de todos os projetos |
| Erros de compilação em cascata | Baixa | Alto | Commits atômicos por sub-namespace |
| Referências externas (YoutubeDLSharp) quebradas | Baixa | Baixo | NÃO migrar namespaces externos |

---

**Criado em:** 13/02/2026  
**Última atualização:** 13/02/2026  
**Tasks Beads:** A criar (Cutube-vxo.16-24)

---

## Referências

- [Fase 6.2: Project Moves](./fase-6.2-project-moves-mover-projetos.md)
- [Épico 6](./epico-6-restructure-monorepo.md)
- [C# Namespaces](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/types/namespaces)
