# Feature: oroborus-design-system — identidade Oroborus no Cutube

> Status: implementada (PR #73)
> Branch: `feature/cutube-oroborus-design-system`
> Origem: design system compartilhado em `oroborus/design-system`

## Resumo

Atualizar a expressão visual pública do Cutube para a identidade Oroborus, usando como referência a base dark de `orodocker`/`orofinance` e a variante clara/editorial de `willsantos.dev`. A GitHub Pages deve receber a aplicação visual principal; a CLI deve adotar a mesma hierarquia cromática apenas onde o terminal suportar apresentação estilizada.

O conteúdo, os comandos, os downloads, a instalação e os fluxos de processamento permanecem inalterados.

## Escopo

- `docs/index.html`: landing page publicada pelo GitHub Pages.
- Apresentação da CLI em `src/Cutube.Cli/`: títulos, menus, mensagens de estado, progresso e erros, preservando fallback legível em terminal sem cor.
- Testes relacionados aos componentes de apresentação que forem alterados.

## Requisitos

### Paleta compartilhada

- **FR-1** — A GitHub Pages deve usar `#0e0e13` como fundo principal e `#f8f5fd` como texto principal.
- **FR-2** — Superfícies devem usar `#131318`, `#19191f`, `#1f1f26` e `#25252c`; bordas devem usar `#48474d` ou `#76747b` conforme contraste.
- **FR-3** — Texto secundário deve usar `#acaab1`.
- **FR-4** — A cor primária deve ser `#b3a1ff`; estados intensos podem usar `#7a53ff` e containers `#a690ff`.
- **FR-5** — A cor secundária deve ser `#ff6a9d`; containers de alto contraste podem usar `#ba005d`.
- **FR-6** — O gradiente da marca pode usar `#5b2eff` → `#ff2e88` em hero, CTA principal e detalhes de identidade.
- **FR-7** — Estados semânticos devem usar sucesso `#4ade80`, aviso `#fbbf24` e erro `#ff6e84`.
- **FR-8** — O azul atual da GitHub Pages e qualquer paleta independente usada como identidade global devem ser removidos da nova composição. Cores de terceiros ou de conteúdo devem continuar somente quando semanticamente necessárias.

### GitHub Pages

- **FR-9** — A hierarquia tipográfica deve usar Space Grotesk em títulos/números de destaque, Manrope em corpo/labels e Geist Mono ou fallback monoespaçado em comandos.
- **FR-10** — O hero deve apresentar o Cutube como ferramenta técnica da Oroborus sem transformar a página em um catálogo institucional.
- **FR-11** — Blocos de instalação, download e código devem usar superfícies elevadas, bordas discretas e contraste suficiente.
- **FR-12** — CTA primário deve usar a cor primária ou o gradiente; CTA secundário deve usar borda e foco da primária.
- **FR-13** — Links, hover, foco, código e estados de sucesso/erro devem permanecer distinguíveis sem depender somente de cor.
- **FR-14** — A página deve funcionar em desktop e mobile, sem overflow horizontal e sem perder os links atuais de instalação, release e documentação.
- **FR-15** — `prefers-reduced-motion` deve reduzir ou remover transições e efeitos decorativos.

### CLI

- **FR-16** — A CLI pode usar ANSI/estilos somente quando a saída não estiver redirecionada e o ambiente suportar cor; em pipe, CI ou terminal sem suporte, o texto deve continuar legível sem códigos de escape.
- **FR-17** — Títulos e etapas principais devem usar primária/lavanda; sucesso, aviso e erro devem usar os estados semânticos definidos.
- **FR-18** — Mensagens existentes, códigos de saída, prompts, progresso e lógica de download não devem mudar de significado.
- **FR-19** — A barra de progresso deve manter largura, atualização e comportamento atuais; apenas cores/ênfase visual podem mudar.

### Compatibilidade

- **FR-20** — Não alterar contratos de instalação, assets, comandos, URLs públicas, releases ou scripts em `scripts/`.
- **FR-21** — Não adicionar dependência apenas para pintar a saída da CLI se a infraestrutura existente permitir fallback simples e testável.
- **FR-22** — O resultado deve ser validado com `dotnet test` e `dotnet build` sem warnings.

## Critérios de aceitação

- A landing page visualiza a paleta Oroborus dark e não usa o azul atual como cor de marca.
- A identidade da CLI e da página é coerente, mas a CLI funciona normalmente sem cor.
- Instalação, links, downloads e comandos continuam funcionando.
- Estados de foco e contraste são verificáveis em desktop e mobile.
- Testes e build passam sem warnings.

## Fora de escopo

- Alterar arquitetura de download, yt-dlp, FFmpeg ou processamento de cortes.
- Reescrever textos da documentação ou mudar o posicionamento do produto.
- Criar um pacote de design system compartilhado entre repositórios.
- Aplicar automaticamente a identidade a outros projetos dentro de `oroborus`.
