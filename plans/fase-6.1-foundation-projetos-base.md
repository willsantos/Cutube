# Fase 6.1: Foundation (Projetos Base)

**Status:** 🎯 Planejamento  
**Épico:** Cutube-vxo (Épico 6: Restructure Monorepo)  
**Duração:** 1 hora  
**Responsável:** Backend / Fullstack  
**Prioridade:** 🔥 Alta  
**Dependência:** ⏳ Nenhuma

---

## Objetivo

Criar os três projetos foundation (Core, Infrastructure, Application) que formarão a base arquitetural para desacoplamento futuro. Estes projetos estarão vazios inicialmente, mas prontos para receber lógica de negócio extraída de outros projetos.

**Benefícios:**
- ✅ **Separação de Concerns**: Camadas claras para domínio, infraestrutura e casos de uso
- ✅ **Desacoplamento**: API e Worker poderão compartilhar lógica via Core/Application
- ✅ **Escalabilidade**: Facilita adicionar Cutube.Mobile, Cutube.Desktop no futuro
- ✅ **Preparação para Clean Architecture**: Base sólida para arquitetura limpa

---

## Visão Arquitetural

### Estrutura Foundation

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    PROJETOS FOUNDATION (NOVOS)                       │
└─────────────────────────────────────────────────────────────────────────┘

src/
├── Cutube.Core/                    # 🆕 Vazio (receberá lógica)
│   ├── Cutube.Core.csproj
│   └── (futuro: Models, Interfaces, Services)
│
├── Cutube.Infrastructure/           # 🆕 Vazio (yt-dlp, FFmpeg)
│   ├── Cutube.Infrastructure.csproj
│   └── (futuro: YtdlService, FfmpegService)
│
└── Cutube.Application/             # 🆕 Vazio (use cases)
    ├── Cutube.Application.csproj
    └── (futuro: DownloadVideo, CancelDownload, etc.)
```

### Relacionamento Futuro

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    RELACIONAMENTO DE FUTURO                         │
└─────────────────────────────────────────────────────────────────────────┘

Cutube.Api ─────────────────────────────────┐
Cutube.Worker ──────────────────────────────┤
Cutube.Cli ─────────────────────────────────┤
                                          │
                                          ▼
                    ┌─────────────────────────────┐
                    │   Cutube.Application        │
                    │   (Use Cases / Orquestração) │
                    └─────────────────────────────┘
                                          │
                                          ▼
                    ┌─────────────────────────────┐
                    │   Cutube.Core              │
                    │   (Domínio / Serviços)      │
                    └─────────────────────────────┘
                          │              │
                          ▼              ▼
            ┌─────────────────┐  ┌─────────────────────┐
            │ Cutube.Domain   │  │ Cutube.Infrastructure │
            │ (Entities/DTOs)│  │ (yt-dlp, FFmpeg)     │
            └─────────────────┘  └─────────────────────┘
```

### Configuração dos Projetos

```
┌─────────────────────────────────────────────────────────────────────────┐
│              CONFIGURAÇÃO (.NET 10.0, ImplicitUsings, Nullable)      │
└─────────────────────────────────────────────────────────────────────────┘

Parâmetro         │ Valor                │ Descrição
─────────────────┼──────────────────────┼──────────────────────────────
TargetFramework  │ net10.0             │ .NET 10.0
ImplicitUsings   │ enable               │ using automático
Nullable         │ enable               │ Tipos nullable
LangVersion       │ latest               │ C# 13+ features
IsPackable        │ false (classlib)     │ Não é aplicação
```

---

## Tarefas

---

### 6.1.1 Criar Projeto Cutube.Core

**Estimativa:** 10 min

**Arquivos:**
```
src/
  └── Cutube.Core/
      ├── Cutube.Core.csproj
      └── (Class1.cs deletado)
```

**Implementação:**

```bash
# Criar projeto
dotnet new classlib -n Cutube.Core -o src/Cutube.Core

# Remover arquivo gerado
rm src/Cutube.Core/Class1.cs

# Adicionar à solução
dotnet sln cutube.sln add src/Cutube.Core/Cutube.Core.csproj

# Verificar build
dotnet build src/Cutube.Core/Cutube.Core.csproj
```

```xml
<!-- src/Cutube.Core/Cutube.Core.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
</Project>
```

**Checklist:**
- [ ] `src/Cutube.Core/` criado
- [ ] `Cutube.Core.csproj` targeting .NET 10.0
- [ ] ImplicitUsings habilitado
- [ ] Nullable habilitado
- [ ] Adicionado à solução
- [ ] Build sem erros

**Critérios de aceite:**
- ✅ Projeto Cutube.Core existe e builda

---

#### 6.1.2 Criar Projeto Cutube.Infrastructure

**Estimativa:** 10 min

**Arquivos:**
```
src/
  └── Cutube.Infrastructure/
      ├── Cutube.Infrastructure.csproj
      └── (Class1.cs deletado)
```

**Implementação:**

```bash
# Criar projeto
dotnet new classlib -n Cutube.Infrastructure -o src/Cutube.Infrastructure

# Remover arquivo gerado
rm src/Cutube.Infrastructure/Class1.cs

# Adicionar à solução
dotnet sln cutube.sln add src/Cutube.Infrastructure/Cutube.Infrastructure.csproj

# Verificar build
dotnet build src/Cutube.Infrastructure/Cutube.Infrastructure.csproj
```

```xml
<!-- src/Cutube.Infrastructure/Cutube.Infrastructure.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
</Project>
```

**Checklist:**
- [ ] `src/Cutube.Infrastructure/` criado
- [ ] `Cutube.Infrastructure.csproj` targeting .NET 10.0
- [ ] ImplicitUsings habilitado
- [ ] Nullable habilitado
- [ ] Adicionado à solução
- [ ] Build sem erros

**Critérios de aceite:**
- ✅ Projeto Cutube.Infrastructure existe e builda

---

#### 6.1.3 Criar Projeto Cutube.Application

**Estimativa:** 10 min

**Arquivos:**
```
src/
  └── Cutube.Application/
      ├── Cutube.Application.csproj
      └── (Class1.cs deletado)
```

**Implementação:**

```bash
# Criar projeto
dotnet new classlib -n Cutube.Application -o src/Cutube.Application

# Remover arquivo gerado
rm src/Cutube.Application/Class1.cs

# Adicionar à solução
dotnet sln cutube.sln add src/Cutube.Application/Cutube.Application.csproj

# Verificar build
dotnet build src/Cutube.Application/Cutube.Application.csproj
```

```xml
<!-- src/Cutube.Application/Cutube.Application.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
</Project>
```

**Checklist:**
- [ ] `src/Cutube.Application/` criado
- [ ] `Cutube.Application.csproj` targeting .NET 10.0
- [ ] ImplicitUsings habilitado
- [ ] Nullable habilitado
- [ ] Adicionado à solução
- [ ] Build sem erros

**Critérios de aceite:**
- ✅ Projeto Cutube.Application existe e builda

---

## Checklist de Fase

### Implementação
- [ ] Projeto Cutube.Core criado
- [ ] Projeto Cutube.Infrastructure criado
- [ ] Projeto Cutube.Application criado
- [ ] Todos adicionados à solução

### Validação
- [ ] Todos os 3 projetos buildam
- [ ] dotnet build passa sem warnings
- [ ] TargetFramework é net10.0
- [ ] ImplicitUsings habilitado
- [ ] Nullable habilitado

---

## Notas

- **Vazios por Design**: Estes projetos são intencionalmente vazios - serão populados em épicos futuros
- **Ordem de Criação**: Core → Infrastructure → Application (embora independentes)
- **Sem Dependencies**: Projetos foundation não se referenciam ainda
- **Git mv N/A**: Criação de novos projetos, não movimentação

---

**Criado em:** 12/02/2026  
**Última atualização:** 12/02/2026  
**Tasks Beads:** Cutube-vxo.2 (T1)

---

## Referências

- [Épico 6](./epico-6-restructure-monorepo.md)
- [Design](../.specs/features/restructure/design.md)
- [.NET Class Libraries](https://learn.microsoft.com/en-us/dotnet/standard/library-guidance/)
