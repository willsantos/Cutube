# Fase 6.3: Solution & References (Atualizar Solução)

**Status:** 🎯 Planejamento  
**Épico:** Cutube-vxo (Épico 6: Restructure Monorepo)  
**Duração:** 55 minutos  
**Responsável:** Backend / Fullstack  
**Prioridade:** 🔥 Alta  
**Dependência:** ✅ Fase 6.2 (Project Moves)

---

## Objetivo

Atualizar o arquivo de solução (.sln) para refletir novas localizações dos projetos e remover referências incorretas (API→CLI) e adicionar referências corretas (API→Core).

**Benefícios:**
- ✅ **Solução Atualizada**: Visual Studio/VS Code vê estrutura correta
- ✅ **Desacoplamento**: API não referencia mais CLI
- ✅ **Preparação para Core**: API, Worker, CLI prontos para usar Core

---

## Visão Arquitetural

### Transição de Referências

```
┌─────────────────────────────────────────────────────────────────────────┐
│               TRANSIÇÃO DE REFERÊNCIAS (ANTES → DEPOIS)             │
└─────────────────────────────────────────────────────────────────────────┘

API (ANTES):
Cutube.Api
  ├── Cutube.Domain ✅
  ├── Cutube.Contracts ✅
  └── cutube ❌ (ERRADO - referencia projeto CLI)

API (DEPOIS):
Cutube.Api
  ├── Cutube.Domain ✅
  ├── Cutube.Contracts ✅
  ├── Cutube.Application 🆕 (use cases)
  └── Cutube.Core 🆕 (lógica de negócio)

Worker (DEPOIS):
Cutube.Worker
  ├── Cutube.Contracts ✅
  ├── Cutube.Application 🆕
  └── Cutube.Core 🆕

CLI (DEPOIS):
Cutube.Cli
  ├── Cutube.Application 🆕
  └── Cutube.Core 🆕
```

### Estrutura da Solução

```
┌─────────────────────────────────────────────────────────────────────────┐
│                  ESTRUTURA DA SOLUÇÃO (.sln)                        │
└─────────────────────────────────────────────────────────────────────────┘

cutube.sln
├── src/
│   ├── Cutube.Api/
│   ├── Cutube.Worker/
│   ├── Cutube.Domain/
│   ├── Cutube.Contracts/
│   ├── Cutube.Core/            🆕
│   ├── Cutube.Infrastructure/   🆕
│   ├── Cutube.Application/     🆕
│   ├── Cutube.Cli/            ✅ (movido, renomeado)
│   └── Cutube.Web/            ✅ (movido, renomeado)
└── tests/
    ├── Cutube.Api.Tests/
    ├── Cutube.Worker.Tests/
    ├── Cutube.Domain.Tests/
    ├── Cutube.Cli.Tests/      ✅ (movido, renomeado)
    └── Cutube.Web.E2E/        ✅ (movido, renomeado)
```

---

## Tarefas

---

### 6.3.1 Atualizar Arquivo de Solução

**Estimativa:** 20 min

**Arquivos:**
```
cutube.sln (atualizar referências)
```

**Implementação:**

```bash
# Remover referências antigas
dotnet sln cutube.sln remove cutube/cutube.csproj
dotnet sln cutube.sln remove cutube-web/cutube-web.csproj
dotnet sln cutube.sln remove Cutube.Tests/Cutube.Tests.csproj

# Adicionar referências novas
dotnet sln cutube.sln add src/Cutube.Cli/Cutube.Cli.csproj
dotnet sln cutube.sln add src/Cutube.Web/Cutube.Web.csproj
dotnet sln cutube.sln add tests/Cutube.Cli.Tests/Cutube.Cli.Tests.csproj
dotnet sln cutube.sln add tests/Cutube.Web.E2E/Cutube.Web.E2E.csproj

# Verificar build
dotnet build cutube.sln
```

**Checklist:**
- [ ] Referências antigas removidas (cutube, cutube-web, Cutube.Tests)
- [ ] Referências novas adicionadas (Cutube.Cli, Cutube.Web, Cutube.Cli.Tests, Cutube.Web.E2E)
- [ ] Solução builda sem erros

**Critérios de aceite:**
- ✅ Solução builda

---

#### 6.3.2 Remover Referência API→CLI

**Estimativa:** 15 min

**Arquivos:**
```
src/Cutube.Api/Cutube.Api.csproj (remover referência)
```

**Implementação:**

```xml
<!-- src/Cutube.Api/Cutube.Api.csproj -->
<!-- ANTES -->
<ItemGroup>
  <ProjectReference Include="..\Cutube.Domain\Cutube.Domain.csproj" />
  <ProjectReference Include="..\Cutube.Contracts\Cutube.Contracts.csproj" />
  <ProjectReference Include="..\..\cutube\cutube.csproj" />
</ItemGroup>

<!-- DEPOIS -->
<ItemGroup>
  <ProjectReference Include="..\Cutube.Domain\Cutube.Domain.csproj" />
  <ProjectReference Include="..\Cutube.Contracts\Cutube.Contracts.csproj" />
  <!-- Referência a cutube REMOVIDA -->
</ItemGroup>
```

```bash
# Verificar que não quebra
dotnet build src/Cutube.Api/Cutube.Api.csproj
```

**Checklist:**
- [ ] Referência a `cutube` removida
- [ ] Build sem erros (pode haver erros de compilação se código depende de cutube)
- [ ] Referências restantes mantidas

**Critérios de aceite:**
- ✅ API não referencia mais CLI

---

#### 6.3.3 Adicionar Referências Core

**Estimativa:** 20 min

**Arquivos:**
```
src/Cutube.Api/Cutube.Api.csproj
src/Cutube.Worker/Cutube.Worker.csproj
src/Cutube.Cli/Cutube.Cli.csproj
```

**Implementação:**

```xml
<!-- src/Cutube.Api/Cutube.Api.csproj -->
<ItemGroup>
  <ProjectReference Include="..\Cutube.Domain\Cutube.Domain.csproj" />
  <ProjectReference Include="..\Cutube.Contracts\Cutube.Contracts.csproj" />
  <ProjectReference Include="..\Cutube.Application\Cutube.Application.csproj" />
  <ProjectReference Include="..\Cutube.Core\Cutube.Core.csproj" />
</ItemGroup>

<!-- src/Cutube.Worker/Cutube.Worker.csproj -->
<ItemGroup>
  <ProjectReference Include="..\Cutube.Contracts\Cutube.Contracts.csproj" />
  <ProjectReference Include="..\Cutube.Application\Cutube.Application.csproj" />
  <ProjectReference Include="..\Cutube.Core\Cutube.Core.csproj" />
</ItemGroup>

<!-- src/Cutube.Cli/Cutube.Cli.csproj -->
<ItemGroup>
  <ProjectReference Include="..\Cutube.Application\Cutube.Application.csproj" />
  <ProjectReference Include="..\Cutube.Core\Cutube.Core.csproj" />
</ItemGroup>
```

```bash
# Verificar build de cada projeto
dotnet build src/Cutube.Api/Cutube.Api.csproj
dotnet build src/Cutube.Worker/Cutube.Worker.csproj
dotnet build src/Cutube.Cli/Cutube.Cli.csproj

# Build da solução completa
dotnet build cutube.sln
```

**Checklist:**
- [ ] API referencia Application e Core
- [ ] Worker referencia Application e Core
- [ ] CLI referencia Application e Core
- [ ] Todos os projetos buildam
- [ ] Solução builda

**Critérios de aceite:**
- ✅ API, Worker, CLI referenciam Core

---

## Checklist de Fase

### Implementação
- [ ] Soluçao atualizada (remove antigos, add novos)
- [ ] Referência API→cutube removida
- [ ] Referências API→Application/Core adicionadas
- [ ] Referências Worker→Application/Core adicionadas
- [ ] Referências CLI→Application/Core adicionadas

### Validação
- [ ] `dotnet sln cutube.sln` mostra todos os projetos corretos
- [ ] `dotnet build cutube.sln` passa sem erros
- [ ] `dotnet build src/Cutube.Api` passa
- [ ] `dotnet build src/Cutube.Worker` passa
- [ ] `dotnet build src/Cutube.Cli` passa

---

## Notas

- **Build Pode Quebrar**: Após remover API→cutube, build pode quebrar se código depende de tipos em cutube
- **Próximo Épico**: Será necessário extrair lógica de cutube para Cutube.Core para restaurar build
- **Solução vs. Projects**: `.sln` é apenas um arquivo de "workspace" - projetos existem independentemente
- **Referências Transitivas**: Application já referencia Core, então API não precisa referenciar Core diretamente (mas pode para clareza)

---

**Criado em:** 12/02/2026  
**Última atualização:** 12/02/2026  
**Tasks Beads:** Cutube-vxo.7 (T8), Cutube-vxo.6 (T9), Cutube-vxo.10 (T10)

---

## Referências

- [Épico 6](./epico-6-restructure-monorepo.md)
- [dotnet sln](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-sln)
