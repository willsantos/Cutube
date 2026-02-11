# Instruções para Agents: Criação de Planos

> **Este documento define o padrão para criação de planos de Épicos e Fases no projeto Cutube.**

---

## 📋 Visão Geral

Este diretório (`plans/`) contém toda a documentação de planejamento do projeto. Existem dois tipos de planos:

1. **Épicos** (`epico-N-nome.md`) - Objetivos macro, estratégicos
2. **Fases** (`fase-N.M-nome.md`) - Implementações específicas, táticas

---

## 📁 Estrutura do Diretório

```
plans/
├── PLAN-EPICO-TEMPLATE.md      # Template para criar épicos
├── PLAN-FASE-TEMPLATE.md       # Template para criar fases
├── AGENTS.md                   # Este arquivo
├── epico-3-integracao-rabbitmq.md
├── epico-4-preparacao-deploy.md
├── fase-3.1-rabbitmq-setup.md
├── fase-3.5-retry-dead-letter-queue.md
└── ...
```

---

## 🎯 Regras Fundamentais

### 1. Separação de Responsabilidades

| Tipo | Escopo | Conteúdo | Exemplo |
|------|--------|----------|---------|
| **Épico** | Macro (semanas) | Visão arquitetural, múltiplas tarefas | "Integração com RabbitMQ" |
| **Fase** | Micro (dias) | Código completo, implementação detalhada | "RabbitMQ Setup" |

### 2. Hierarquia Numérica

- **Épicos**: `1`, `2`, `3`, `4`...
- **Fases**: `3.1`, `3.2`, `3.5`... (N.M onde N = épico, M = fase)
- **Tarefas**: `3.5.1`, `3.5.2`... (N.M.X)
- **Subtarefas**: `3.5.1.1`, `3.5.1.2`... (N.M.X.Y)

### 3. Nomenclatura de Arquivos

```bash
# Épicos
docs/epico-{N}-{nome-curto}.md
ex: epico-3-integracao-rabbitmq.md

# Fases
docs/fase-{N.M}-{nome-curto}.md
ex: fase-3.5-retry-dead-letter-queue.md
```

**Regras para nomes:**
- Use kebab-case (minúsculas com hífens)
- Seja descritivo mas conciso
- Evite acentos e caracteres especiais
- Mantenha consistência com o título do documento

---

## 📝 Fluxo de Criação

### Para Criar um Épico

1. **Consulte o template**: Leia `PLAN-EPICO-TEMPLATE.md`
2. **Defina o número**: Qual o próximo número disponível?
3. **Crie o arquivo**: `plans/epico-{N}-{nome}.md`
4. **Siga a estrutura**:
   - Cabeçalho com metadados
   - Índice com links âncora
   - Objetivo claro
   - Visão arquitetural
   - Tarefas numeradas
   - Checklist geral
5. **Valide**: O épico está completo mas não detalhado demais?
6. **Commit**: `docs: add Epic N plan for [objetivo]`

### Para Criar uma Fase

1. **Consulte o template**: Leia `PLAN-FASE-TEMPLATE.md`
2. **Identifique o épico pai**: A qual épico esta fase pertence?
3. **Defina o número**: Qual o próximo N.M disponível?
4. **Crie o arquivo**: `plans/fase-{N.M}-{nome}.md`
5. **Siga a estrutura**:
   - Cabeçalho com referência ao épico
   - Objetivo específico
   - Diagramas detalhados
   - Código completo e funcional
   - Checklists verificáveis
6. **Valide**: O código pode ser copiado e funcionar?
7. **Commit**: `docs: add Phase N.M plan for [objetivo]`

---

## ✅ Checklist de Qualidade

### Antes de Commitar

- [ ] Nome do arquivo segue o padrão
- [ ] Cabeçalho completo com todos os metadados
- [ ] Índice presente (para épicos) e funcionando
- [ ] Objetivo claro e mensurável
- [ ] Diagramas ASCII bem formatados
- [ ] Código completo (não pseudocódigo) para fases
- [ ] Checklists são verificáveis (não subjetivos)
- [ ] Critérios de aceite são mensuráveis
- [ ] Referências a outros planos estão corretas
- [ ] Nenhum placeholder [colchetes] foi esquecido

### Validação de Conteúdo

**Épicos devem ter:**
- Visão de alto nível
- Arquitetura macro
- Tarefas bem definidas mas sem código detalhado
- Benefícios claros listados

**Fases devem ter:**
- Código completo e funcional
- Configurações específicas
- Diagramas detalhados do fluxo
- Checklists de implementação

---

## 🎨 Convenções de Formatação

### Emojis e Status

| Uso | Emoji | Significado |
|-----|-------|-------------|
| Status | 🎯 | Planejamento |
| Status | 🚧 | Em Andamento |
| Status | ✅ | Concluído |
| Prioridade | 🔥 | Alta |
| Prioridade | ⚡ | Média |
| Prioridade | 💤 | Baixa |
| Dependência | ✅ | Completa |
| Dependência | ⏳ | Pendente |
| Critério | ✅ | Atingido |

### Cabeçalho de Épico

```markdown
# Épico [N]: [Título]

**Status:** 🎯 Planejamento  
**Duração:** [X-Y dias/semanas]  
**Responsável:** [Role/Team]  
**Prioridade:** 🔥 [Alta/Média/Baixa]  
**Dependência:** [Status] [Referência]
```

### Cabeçalho de Fase

```markdown
# Fase [N.M]: [Título]

**Status:** 🎯 Planejamento  
**Épico:** [ID] (Épico [N]: [Nome])  
**Duração:** [X-Y dias]  
**Responsável:** [Role]  
**Prioridade:** 🔥 [Alta/Média/Baixa]  
**Dependência:** [Status] [Referência]
```

### Seções Obrigatórias

#### Índice (Apenas Épicos)

```markdown
## Índice

- [Objetivo](#objetivo)
- [Visão Arquitetural](#visão-arquitetural)
- [Tarefas](#tarefas)
  - [4.1 Nome da Tarefa](#41-nome-da-tarefa)
  - [4.2 Outra Tarefa](#42-outra-tarefa)
- [Checklist Geral](#checklist-geral)
- [Próximos Passos](#próximos-passos)
```

#### Checklists

```markdown
**Checklist:**
- [ ] Item específico e verificável
- [ ] Outro item com critério claro

**Critérios de aceite:**
- ✅ [Critério mensurável 1]
- ✅ [Critério mensurável 2]
```

---

## 💻 Padrões de Código

### Para Fases (Código Completo)

**Deve incluir:**
- Nome do arquivo completo
- Namespace correto
- Todos os using/imports necessários
- Comentários explicativos
- Implementação completa (não stubs)

**Exemplo correto:**
```csharp
// src/Cutube.Worker/Configuration/RetryPolicyOptions.cs
namespace Cutube.Worker.Configuration;

/// <summary>
/// Configurações da política de retry.
/// </summary>
public class RetryPolicyOptions
{
    public const string SectionName = "RetryPolicy";

    public int MaxRetries { get; set; } = 3;
    public int InitialDelaySeconds { get; set; } = 5;
    
    public TimeSpan GetDelayForAttempt(int attemptNumber)
    {
        // Implementação completa aqui
    }
}
```

**NÃO faça:**
```csharp
// Stub incompleto
public class RetryPolicyOptions 
{
    // TODO: implementar
}
```

### Diagramas ASCII

**Use estruturas claras:**
```
┌─────────────────────────────────────┐
│           Container                 │
│  ┌──────────────┐  ┌─────────────┐  │
│  │ Componente A │──│ Componente B│  │
│  └──────────────┘  └─────────────┘  │
└─────────────────────────────────────┘
```

---

## 🔗 Referências e Links

### Link para outros planos

```markdown
**Dependência:** ✅ [Fase 3.4](fase-3.4-status-tracking.md) completa
**Épico:** Cutube-858 ([Épico 3](epico-3-integracao-rabbitmq.md))
```

### Referências internas

```markdown
Veja [PLAN-EPICO-TEMPLATE.md](PLAN-EPICO-TEMPLATE.md) para mais detalhes.
Consulte o [AGENTS.md](AGENTS.md) para instruções.
```

---

## 🚫 Anti-Patterns

### O que EVITAR

❌ **Nomes de arquivo inconsistentes**
```
# Errado
epico_3_rabbitmq.md
Epico3.md
fase35retry.md

# Certo
epico-3-integracao-rabbitmq.md
fase-3.5-retry-dead-letter-queue.md
```

❌ **Código incompleto em fases**
```csharp
// Errado - stub
public void Process() {
    // TODO: implementar lógica
}

// Certo - implementação completa
public void Process() {
    var result = _service.Execute();
    if (result.IsSuccess) {
        _repository.Save(result.Data);
    }
}
```

❌ **Checklists subjetivos**
```markdown
# Errado
- [ ] Funciona bem
- [ ] Está otimizado

# Certo
- [ ] Testes passam: `dotnet test`
- [ ] Build sem warnings: `dotnet build`
- [ ] Tempo de resposta < 100ms
```

❌ **Esquecer o índice em épicos**
```markdown
# Errado - começa direto no Objetivo
## Objetivo
...

# Certo - tem índice antes
## Índice
- [Objetivo](#objetivo)
...

## Objetivo
```

---

## 📚 Referências

- [PLAN-EPICO-TEMPLATE.md](PLAN-EPICO-TEMPLATE.md) - Template para épicos
- [PLAN-FASE-TEMPLATE.md](PLAN-FASE-TEMPLATE.md) - Template para fases
- Exemplos:
  - [epico-3-integracao-rabbitmq.md](epico-3-integracao-rabbitmq.md)
  - [fase-3.5-retry-dead-letter-queue.md](fase-3.5-retry-dead-letter-queue.md)

---

## 🔄 Manutenção

Este documento deve ser atualizado quando:
- Novos padrões forem estabelecidos
- Templates forem modificados
- Novos exemplos excelentes forem criados

**Última atualização:** 11/02/2026
