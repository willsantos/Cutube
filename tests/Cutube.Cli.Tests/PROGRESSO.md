# Progresso dos Testes - Cutube

**Data:** 30/01/2026  
**Status:** Plano implementado  
**Testes Totais:** 84 (100% passing) ✅

## Cobertura Atual

```
Cobertura de Linhas: 97.34% (476/489)
Cobertura de Branches: 94.05% (95/101)
```

## Resumo por Fase

### Fase 1: Setup e Testes Simples ✅
- xUnit + Moq + FluentAssertions + coverlet
- TimeHelper + TitleHelper testados

### Fase 2: Menu ✅
- Testes básicos de input

### Fase 3: YtDlpHelper ✅
- Path resolution (user/bundle/system/download)
- Auto-update (mesma versão, versão diferente, erro)
- Platform identifier (Windows/Linux/MacOS, arquiteturas e defaults)
- GetCurrentVersion e GetLatestVersion

### Fase 4: FfmpegHelper ✅
- Resolução de caminho via AppData/PATH
- Parsing de Duration e time
- Edge cases sem Duration/regex inválido

### Fase 5: ProgressBar ✅
- Timer start/stop
- Clamp de progresso
- Ciclo de animação
- Dispose

### Fase 6: ProgramWorkflow ✅
- Fluxo completo com sucesso
- Fluxo com erro
- Progresso/estado reportado

## Testes Implementados (resumo)

- **TimeHelperTests**: parsing + diff (100%)
- **TitleHelperTests**: sanitização + ctor (100%)
- **MenuTests**: fluxo de entrada básico
- **YtDlpHelperTests**: cobertura extensa (96%+)
- **FfmpegHelperTests**: parsing + resolução (96%+)
- **ProgressBarTests**: animação + clamp + dispose
- **ProgramWorkflowTests**: sucesso/erro/progresso

## Exclusões com ExcludeFromCodeCoverage

Wrappers de infraestrutura e entrypoint:
- FileService, HttpClientService, EnvironmentService
- ProcessService, ProcessRunner, ConsoleService
- SystemTimer, SystemTimerFactory, MenuService
- Program (entrypoint)

## Comandos Úteis

```bash
# Executar todos os testes
cd Cutube.Tests && dotnet test

# Gerar relatório de cobertura
cd Cutube.Tests && dotnet test --collect:"XPlat Code Coverage" --results-directory ./coverage
```
