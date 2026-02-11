# Template: Plano de Fase

> **NOTA:** Este é um template. Substitua todo o conteúdo entre [colchetes] pelas informações específicas da fase.
> 
> Uma **Fase** é uma parte de um Épico. Use este template para detalhar implementações específicas.

---

# Fase [N.M]: [Título da Fase]

**Status:** 🎯 Planejamento  
**Épico:** [ID do Épico] (Épico [N]: [Nome do Épico])  
**Duração:** [X-Y dias]  
**Responsável:** [Role/Developer]  
**Prioridade:** 🔥 [Alta/Média/Baixa]  
**Dependência:** [Status] [Fase/Etapa dependente]

---

## Objetivo

[Descreva o objetivo específico desta fase em 1-2 parágrafos. O que será implementado e qual o resultado esperado.]

**Benefícios:**
- ✅ **[Benefício 1]**: [Descrição curta]
- ✅ **[Benefício 2]**: [Descrição curta]
- ✅ **[Benefício 3]**: [Descrição curta]
- ✅ **[Benefício 4]**: [Descrição curta]
- ✅ **[Benefício 5]**: [Descrição curta]

---

## Visão Arquitetural

### [Nome do Fluxo/Processo]

```
┌─────────────────────────────────────────────────────────────────────────┐
│                         [Título do Diagrama]                            │
│                                                                         │
│  ┌──────────────┐                                                       │
│  │ [Componente] │  [Número]. [Ação/Descrição]                           │
│  │   [Nome]     │                                                       │
│  └──────┬───────┘                                                       │
│         │                                                               │
│         ▼                                                               │
│  ┌──────────────────────────────────────────────────────────────────┐  │
│  │                    [Componente Principal]                         │  │
│  │                                                                   │  │
│  │  [Detalhes internos, sub-componentes ou fluxo]                   │  │
│  │                                                                   │  │
│  └──────────────────────────────────────────────────────────────────┘  │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### [Outro Diagrama: Estados, Fluxo ou Sequência]

```
[Diagrama ASCII mostrando estados, transições, ou sequência de operações]

Exemplo de máquina de estados:
┌──────────┐     ┌──────────┐     ┌──────────────┐
│  Estado  │────►│  Estado  │────►│   Estado     │
│    A     │     │    B     │     │   Final      │
└──────────┘     └──────────┘     └──────────────┘
```

### [Tabela de Configuração/Parâmetros]

```
┌─────────────────────────────────────────────────────────────────┐
│              [Título da Configuração]                           │
│                                                                 │
│  Parâmetro    │  Valor    │  Descrição                         │
│  ─────────────┼───────────┼────────────────────────────────────│
│  [Param1]     │  [Valor]  │  [Descrição]                       │
│  [Param2]     │  [Valor]  │  [Descrição]                       │
│  [Param3]     │  [Valor]  │  [Descrição]                       │
└─────────────────────────────────────────────────────────────────┘
```

---

## Tarefas

---

### [N.M.1 Título da Tarefa Principal]

**Estimativa:** [X-Y horas]

**Arquivos:**
```
[caminho/para/diretorio/]
  ├── [Diretorio/]
  │   └── [Arquivo1.cs]
  ├── [Diretorio2/]
  │   └── [Arquivo2.cs]
  └── [Arquivo3.cs] (atualizar)
```

#### [N.M.1.1 Subtarefa Específica]

**Implementação:**

```[linguagem]
// [caminho/do/arquivo.extensao]
namespace [Namespace];

/// <summary>
/// [Descrição da classe/interface]
/// </summary>
public class [NomeClasse]
{
    // [Comentários explicativos]
    [Implementação completa]
}
```

**Configuração (se aplicável):**

```json
{
  "[Secao]": {
    "[Propriedade1]": [valor],
    "[Propriedade2]": [valor],
    "[Propriedade3]": [valor]
  }
}
```

**Checklist:**
- [ ] [Item de verificação específico 1]
- [ ] [Item de verificação específico 2]
- [ ] [Item de verificação específico 3]
- [ ] [Item de verificação específico 4]
- [ ] [Item de verificação específico 5]

**Critérios de aceite:**
- ✅ [Critério mensurável e verificável 1]
- ✅ [Critério mensurável e verificável 2]
- ✅ [Critério mensurável e verificável 3]
- ✅ [Critério mensurável e verificável 4]

---

#### [N.M.1.2 Outra Subtarefa]

[Mesma estrutura: código, configuração, checklist, critérios]

---

### [N.M.2 Próxima Tarefa Principal]

**Estimativa:** [X-Y horas]

[Mesma estrutura da tarefa anterior]

---

## Checklist de Fase

### Implementação
- [ ] [Requisito técnico 1]
- [ ] [Requisito técnico 2]
- [ ] [Requisito técnico 3]

### Testes
- [ ] [Teste unitário 1]
- [ ] [Teste de integração 1]
- [ ] [Teste end-to-end 1]

### Documentação
- [ ] [Documentação de código]
- [ ] [Atualização de README]

### Validação
- [ ] Todos os critérios de aceite atendidos
- [ ] Código revisado
- [ ] Sem warnings no build

---

## Notas

- [Nota técnica ou restrição]
- [Decisão de design importante]
- [Referência a documentação externa]

---

**Criado em:** [DD/MM/YYYY]  
**Última atualização:** [DD/MM/YYYY]

---

## Guia de Uso deste Template

### Quando Usar

Use este template para **Fases** de um Épico:
- Fases são partes menores e específicas de um objetivo maior
- Geralmente duram 2-5 dias
- Têm escopo bem definido e limitado
- Produzem entregáveis concretos

### Estrutura Obrigatória

1. **Cabeçalho** com metadados incluindo referência ao Épico pai
2. **Objetivo** claro e focado
3. **Visão Arquitetural** com diagramas específicos da fase
4. **Tarefas** numeradas (N.M.1, N.M.2, etc.)
5. **Subtarefas** detalhadas com código completo (N.M.1.1, N.M.1.2)
6. **Checklist de Fase** no final

### Diferenças do Template de Épico

| Aspecto | Épico | Fase |
|---------|-------|------|
| Escopo | Macro, estratégico | Micro, tático |
| Duração | Semanas | Dias |
| Detalhamento | Visão geral, arquitetura | Código completo, implementação |
| Seções | Visão ampla, múltiplas tarefas | Foco específico, tarefas detalhadas |
| Código | Pseudocódigo ou snippets | Código completo e funcional |

### Convenções

- Siga as mesmas convenções do template de Épico
- Referencie sempre o Épico pai no cabeçalho
- Use numeração hierárquica: Fase N.M → Tarefa N.M.X → Subtarefa N.M.X.Y
- Código deve ser copiável e funcional (não exemplos simplificados)
