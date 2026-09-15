# Product

<!-- impeccable:product-schema 1 -->

## Platform

web

## Users

Criadores e editores de podcast que precisam baixar e cortar episódios/clipes do
YouTube direto do terminal, sem fluxo de edição manual. Público secundário
confirmado implicitamente pelo produto: usuários técnicos (devs/power users)
confortáveis com CLI que baixam mídia para uso pessoal.

## Product Purpose

Cutube é uma CLI standalone escrita em C#/.NET que baixa vídeos do YouTube em
diferentes formatos e qualidades e corta no tempo desejado, localmente, sem
servidor, fila ou Docker. Sucesso é: instalar sem fricção, baixar/cortar em um
comando (interativo ou direto) e continuar funcionando quando o YouTube muda.

## Positioning

Ferramenta técnica da marca Oroborus com configuração zero: yt-dlp vem
bundleado com auto-update, FFmpeg é baixado automaticamente no Windows, e o
corte por tempo é cidadão de primeira classe — um concorrente CLI genérico não
pode alegar o pacote completo "baixar + cortar + auto-update + auto-ffmpeg" sem
servidor.

## Operating Context

- Uso em terminal (Linux, macOS, Windows); saída pode ser redirecionada (pipes,
  CI), onde a apresentação precisa permanecer legível sem cor.
- Distribuição: scripts de instalação (`scripts/install.sh`, `install.ps1`),
  releases do GitHub e página de instalação publicada no GitHub Pages
  (`docs/index.html` → https://willsantos.github.io/Cutube/).
- Documentação de instalação em `docs/installation.md`.

## Capabilities and Constraints

- Comandos: modo interativo, `download <url>` com `--audio`, corte
  `-s/-e`, `-o` (destino), `--resume`, `config show/reset`, `update`.
- Barra de progresso de corte existente; progresso de download ainda no roadmap.
- Constraints duráveis (spec `.specs/oroborus-design-system/spec.md`):
  - FR-16/17/19: cores ANSI da CLI somente com TTY interativo; fallback
    legível sem escape codes; mensagens, códigos de saída e comportamento de
    progresso não mudam de significado.
  - FR-20/21: não alterar contratos de instalação, assets, URLs públicas,
    releases ou scripts; não adicionar dependência só para colorir a CLI.
  - FR-22: `dotnet test` e `dotnet build` devem passar sem warnings.
- Decisão em aberto: suporte a outras plataformas (Instagram etc.) está no
  roadmap, não confirmado.

## Brand Commitments

Identidade Oroborus, tornada vinculante pela spec da branch
(`.specs/oroborus-design-system/spec.md`):

- Paleta dark Oroborus: fundo `#0e0e13`, texto `#f8f5fd`, superfícies
  `#131318`–`#25252c`, bordas `#48474d`/`#76747b`, texto secundário `#acaab1`;
  primária `#b3a1ff` (intensa `#7a53ff`, container `#a690ff`); secundária
  `#ff6a9d`; gradiente da marca `#5b2eff` → `#ff2e88`; estados: sucesso
  `#4ade80`, aviso `#fbbf24`, erro `#ff6e84`. O azul atual da GitHub Pages deixa
  de ser identidade.
- Tipografia da página: Space Grotesk (títulos/números), Manrope
  (corpo/labels), Geist Mono ou monoespaçada (comandos).
- Referências de mundo: base dark de `orodocker`/`orofinance`; variante
  clara/editorial de `willsantos.dev`. Tom: técnico e direto, sem catálogo
  institucional (FR-10).

## Evidence on Hand

- README.md (comandos, instalação, roadmap, motivação).
- Spec completa com paleta, tipografia e critérios de aceitação em
  `.specs/oroborus-design-system/` (spec.md, design.md, tasks.md).
- Landing atual em `docs/index.html` (identidade azul a ser substituída).
- Não há testemunhos, benchmarks, métricas de adoção ou press: trabalho futuro
  não deve fabricar esse conteúdo.

## Product Principles

- Zero configuração: o comando certo funciona na primeira execução.
- O terminal é o produto: a CLI precisa ser excelente sem cor, sem rede e sem
  interação; o enfeite nunca quebra o pipe.
- Local e privado: tudo roda na máquina do usuário, sem servidor.
- Una identidade, superfícies honestas: a página e a CLI compartilham a
  hierarquia Oroborus, cada uma com a expressão que seu meio suporta.

## Accessibility & Inclusion

WCAG 2.1 AA como requisito formal da GitHub Pages (contraste ≥ 4.5:1 em texto,
foco visível, estados distinguíveis sem depender só de cor — FR-13) e
`prefers-reduced-motion` respeitado (FR-15). Na CLI: legibilidade garantida em
terminal sem suporte a cor.
