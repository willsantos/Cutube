---
version: 1
slug: "docs-index-html"
primary_target: "docs/index.html"
related_targets: []
---

# Surface brief: docs/index.html (GitHub Pages — instalação)

## Scope and visitor mode

Redesign substitutivo da landing de instalação do Cutube (GitHub Pages), na
identidade Oroborus. Modo: Persuade/Operate híbrido — o visitante decide
instalar e executa a instalação na própria página (copiar um comando).
Conteúdo, comandos, URLs e links preservados (spec FR-14, FR-20).

## Audience, job, action, proof, constraints

Criadores/editores de podcast e usuários técnicos; trabalho: baixar/cortar
mídia do YouTube sem fricção. Ação: rodar o comando de instalação (Linux/macOS
ou Windows). Prova: comandos reais, zero configuração (yt-dlp incluso com
auto-update; FFmpeg automático no Windows). Constraints: paleta e tipografia
vinculantes da spec (`.specs/oroborus-design-system/spec.md` FR-1..FR-15),
WCAG 2.1 AA, `prefers-reduced-motion`, sem overflow mobile.

## Chosen direction and memorable moment

Brief-pinned: mundo dark Oroborus já decidido na spec (sem roll). Momento de
assinatura: o hero técnica-operacional com a palavra-chave em gradiente da
marca (#5b2eff → #ff2e88) sobre o dark `#0e0e13` e o comando de instalação
vivo em painel de terminal, com copiar em um clique.

## Direction contract

THESIS: página-terminal operacional — o comando de instalação é o herói, não
uma landing institucional; recusa o padrão de categoria (hero genérico + três
cards iguais de features).

OWN-WORLD: dark #0e0e13, superfícies #131318–#25252c, bordas #48474d, texto
#f8f5fd/#acaab1, lavanda #b3a1ff como voz de ação, rosa #ff6a9d de energia
pontual, gradiente #5b2eff→#ff2e88 restrito a uma palavra e ao detalhe de
marca; Space Grotesk (display), Manrope (corpo), Geist Mono (comandos).

STORY: "isto é uma ferramenta técnica séria da Oroborus; um comando e eu
tenho o Cutube instalado" — o visitante copia, cola e confirma com
`cutube --version`.

FIRST VIEWPORT: topo com wordmark Cutube + 3 links (Instalar, Documentação,
GitHub); hero à esquerda com H1 Space Grotesk grande ("Baixe. Corte.
Pronto." com um segmento em gradiente), uma linha de apoio Manrope, CTA
primário lavanda (texto escuro) + CTA secundário contornado; à direita/abaixo,
painel de terminal #19191f com o comando `curl … | bash` em Geist Mono e botão
copiar; faixa fina de detalhe em gradiente no limite inferior do viewport.

FORM: forma definida pelo brief (spec aprovada), sem seed — posição única da
lista ordenada; nenhuma candidata alternativa em jogo.

FINISH: unreviewed and undocumented is unfinished; this build ends with the
finish review, the verdict, DESIGN.md, and every shipping raster carrying its
provenance.

## Unresolved decisions

Nenhuma bloqueante. Modo claro (willsantos.dev) fora de escopo nesta fase.
