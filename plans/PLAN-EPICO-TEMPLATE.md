# Template: Plano de Épico

> **NOTA:** Este é um template. Substitua todo o conteúdo entre [colchetes] pelas informações específicas do épico.

---

# Épico [N]: [Título do Épico]

**Status:** 🎯 Planejamento  
**Duração:** [X-Y dias]  
**Responsável:** [Role/Team]  
**Prioridade:** 🔥 [Alta/Média/Baixa]  
**Dependência:** [Status] [Épico/Fase dependente]

---

## Índice

- [Objetivo](#objetivo)
- [Visão Arquitetural](#visão-arquitetural)
- [Tarefas](#tarefas)
- [Checklist Geral do Épico](#checklist-geral-do-épico)
- [Próximos Passos](#próximos-passos)
- [Notas](#notas)

---

## Objetivo

[Descreva o objetivo macro do épico em 2-3 parágrafos. Explique o que será alcançado e por que é importante.]

Este épico inclui:

1. **[Item 1]** - [Breve descrição]
2. **[Item 2]** - [Breve descrição]
3. **[Item 3]** - [Breve descrição]

**Benefícios:**
- ✅ **[Benefício 1]**: [Descrição]
- ✅ **[Benefício 2]**: [Descrição]
- ✅ **[Benefício 3]**: [Descrição]
- ✅ **[Benefício 4]**: [Descrição]
- ✅ **[Benefício 5]**: [Descrição]

---

## Visão Arquitetural

### [Nome da Visão/Stack]

```
┌─────────────────────────────────────────────────────────────────────────┐
│                      [Diagrama ASCII da Arquitetura]                    │
│                                                                         │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │                    [Componente 1]                                │   │
│  │                                                                  │   │
│  │   [Sub-componentes ou fluxo]                                     │   │
│  │                                                                  │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                                                                         │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │                    [Componente 2]                                │   │
│  │                                                                  │   │
│  │   [Sub-componentes ou fluxo]                                     │   │
│  │                                                                  │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### [Fluxo/Processo Adicional]

```
[Diagrama de fluxo, sequência ou estado usando ASCII art]
```

---

## Tarefas

---

### [N.1 Título da Tarefa Principal]

**Estimativa:** [X-Y horas]  
**Prioridade:** 🔥 [Alta/Média/Baixa]

#### [N.1.1 Subtarefa Específica]

**Arquivos:**
```
[caminho/para/diretorio/]
  ├── [Arquivo1.cs]
  ├── [Arquivo2.cs]
  └── [Subdiretorio/]
      └── [Arquivo3.cs]
```

**Implementação:**

```[linguagem]
// [caminho/do/arquivo.extensao]
// [Comentário explicativo do código]

[code]
```

**Checklist:**
- [ ] [Item de verificação 1]
- [ ] [Item de verificação 2]
- [ ] [Item de verificação 3]
- [ ] [Item de verificação 4]
- [ ] [Item de verificação 5]

**Critérios de aceite:**
- ✅ [Critério mensurável 1]
- ✅ [Critério mensurável 2]
- ✅ [Critério mensurável 3]
- ✅ [Critério mensurável 4]

---

#### [N.1.2 Outra Subtarefa]

[Mesma estrutura da subtarefa anterior]

---

### [N.2 Próxima Tarefa Principal]

[Mesma estrutura da tarefa anterior]

---

## Checklist Geral do Épico

### [Categoria 1]
- [ ] [Item de verificação]
- [ ] [Item de verificação]
- [ ] [Item de verificação]

### [Categoria 2]
- [ ] [Item de verificação]
- [ ] [Item de verificação]
- [ ] [Item de verificação]

### Testes Finais
- [ ] [Teste end-to-end 1]
- [ ] [Teste end-to-end 2]
- [ ] [Teste de integração]
- [ ] [Validação de critérios de aceite]

### Documentação
- [ ] [README atualizado]
- [ ] [Documentação técnica]
- [ ] [Guia de troubleshooting]

---

## Próximos Passos

Após completar este épico:

1. **[Próximo passo 1]**: [Descrição]
2. **[Próximo passo 2]**: [Descrição]
3. **[Próximo passo 3]**: [Descrição]

---

## Notas

- [Nota técnica ou restrição importante]
- [Outra nota relevante]
- [Referência a documentação externa]

---

**Criado em:** [DD/MM/YYYY]  
**Status:** [🎯 Planejamento / 🚧 Em Andamento / ✅ Concluído]

---

## Guia de Uso deste Template

### Estrutura Obrigatória

1. **Cabeçalho** com metadados (status, duração, responsável, prioridade, dependência)
2. **Índice** com links âncora para todas as seções
3. **Objetivo** claro e mensurável
4. **Visão Arquitetural** com diagramas
5. **Tarefas** numeradas (N.1, N.2, etc.)
6. **Checklist Geral** no final
7. **Próximos Passos** para continuidade

### Convenções

- Use emojis para status: 🎯 Planejamento, 🚧 Em Andamento, ✅ Concluído
- Use 🔥 para prioridade Alta, ⚡ para Média, 💤 para Baixa
- Marque dependências com ✅ se completas, ⏳ se pendentes
- Código deve ser completo e funcional (não pseudocódigo)
- Diagramas ASCII devem ser claros e bem formatados
- Checklists devem ser verificáveis (não subjetivos)

### Nomenclatura de Arquivos

- Épicos: `epico-N-nome-do-epico.md`
- Fases: `fase-N.M-nome-da-fase.md`
- Exemplos:
  - `epico-3-integracao-rabbitmq.md`
  - `fase-3.5-retry-dead-letter-queue.md`
