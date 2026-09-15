# Design: oroborus-design-system — identidade Oroborus no Cutube

> Spec: `.specs/oroborus-design-system/spec.md`

## Direção visual

O Cutube deve parecer uma ferramenta operacional da Oroborus: escuro, técnico, claro e com energia pontual. A página pública concentra a expressão visual; a CLI usa cor como reforço de hierarquia, nunca como requisito para compreensão.

## Tokens

| Papel | Valor |
| --- | --- |
| Fundo | `#0e0e13` |
| Superfície baixa | `#131318` |
| Superfície | `#19191f` |
| Card | `#1f1f26` |
| Card elevado | `#25252c` |
| Texto principal | `#f8f5fd` |
| Texto secundário | `#acaab1` |
| Borda | `#48474d` |
| Borda forte | `#76747b` |
| Primária | `#b3a1ff` |
| Primária intensa | `#7a53ff` |
| Secundária | `#ff6a9d` |
| Gradiente | `#5b2eff` → `#ff2e88` |
| Sucesso | `#4ade80` |
| Aviso | `#fbbf24` |
| Erro | `#ff6e84` |

## GitHub Pages (`docs/index.html`)

- `body`: fundo `#0e0e13`, texto `#f8f5fd`, Manrope.
- Hero: título em Space Grotesk, com gradiente limitado a uma palavra, linha ou detalhe de marca.
- Painéis de instalação e release: `#19191f`/`#1f1f26`, borda `#48474d`, raio de 8–12px.
- Comandos: Geist Mono ou fallback monoespaçado, texto claro e destaque lavanda.
- CTA primário: `#b3a1ff` com texto escuro quando o contraste permitir; gradiente para a ação principal do hero.
- CTA secundário: fundo transparente, borda `#48474d`, hover/focus com primária.
- Links: primária; hover pode usar secundária em elementos de destaque.
- Status de instalação: sucesso verde, aviso âmbar, erro coral, sempre acompanhado de texto/ícone.
- Evitar glow excessivo; o conteúdo deve continuar legível em telas pequenas.

## CLI (`src/Cutube.Cli`)

Usar o `IConsoleService` existente como ponto de aplicação para não espalhar lógica de terminal pelos fluxos.

| Elemento | Estilo com cor | Fallback sem cor |
| --- | --- | --- |
| Título/seção | primária/Space-like emphasis | texto atual |
| Progresso | primária + percentual | barra atual |
| Sucesso | verde | prefixo/mensagem atual |
| Aviso | âmbar | prefixo/mensagem atual |
| Erro | erro coral | prefixo/mensagem atual |
| Comando/caminho | mono e texto claro | texto atual |

Não reescrever as mensagens nesta feature. Primeiro identificar pontos de saída e aplicar estilo no limite da abstração de console, preservando os testes de texto e o comportamento em output redirecionado.

## Validação visual e funcional

Validar `docs/index.html` em viewport móvel e desktop, incluindo links, bloco de instalação, código e estados de foco. Validar a CLI em terminal interativo e com saída redirecionada. Comparar a hierarquia com `orodocker`/`orofinance`; usar `willsantos.dev` somente como referência para eventual modo claro futuro, que não faz parte deste escopo.
