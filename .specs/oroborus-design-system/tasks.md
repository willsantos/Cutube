# Tasks: oroborus-design-system — identidade Oroborus no Cutube

> Execução via fluxo spec-driven. Implementação concluída na branch
> `feature/cutube-oroborus-design-system` (PR #73); gates verificados.

## Fase 1 — auditoria e fundação

- [x] **T1** — Auditar `docs/index.html`, `IConsoleService`, `ConsoleService`, `Program`, `Menu`, `ProgressBar` e pontos de mensagem/estado para mapear a superfície visual sem alterar comportamento.
  - Verificação: inventário de cores, fontes, seletores, métodos de saída e testes impactados.
- [x] **T2** — Definir tokens locais para a página e uma abstração mínima de estilo ANSI para a CLI, com detecção de output redirecionado e fallback plain text.
  - Verificação: nenhum escape ANSI aparece em pipe/CI; tokens correspondem ao design aprovado. (`Cutube.Cli.Theming/ConsoleTheme`: desliga em redirect, `NO_COLOR`, `CI`, `TERM=dumb`/ausente e Windows sem VT.)

## Fase 2 — GitHub Pages

- [x] **T3** — Reescrever a paleta, tipografia, superfícies, CTA, links, estados, foco e responsividade de `docs/index.html` para a identidade Oroborus.
  - Verificação: paleta azul atual removida como identidade global; links de instalação/documentação/release preservados (comandos e URLs byte a byte).
- [x] **T4** — Adicionar gradiente e efeitos de interação contidos, incluindo `prefers-reduced-motion`.
  - Verificação: sem overflow mobile (scrollWidth == clientWidth em 1440 e 390) e sem perda de legibilidade com efeitos reduzidos/desativados.

## Fase 3 — CLI

- [x] **T5** — Aplicar estilos Oroborus aos pontos de saída existentes usando `IConsoleService`, sem reescrever mensagens ou alterar códigos de saída.
  - Verificação: títulos, progresso, sucesso, aviso e erro com hierarquia consistente; fallback sem cor equivalente ao comportamento atual.
- [x] **T6** — Atualizar testes unitários da apresentação para cobrir terminal com cor e saída redirecionada.
  - Verificação: nenhum código ANSI em saída não interativa (`ConsoleThemeTests`, `ProgressBarTests`); mensagens e progressão permanecem compatíveis.

## Fase 4 — gates e encerramento

- [x] **T7** — Validar manualmente a página em desktop/mobile e a CLI em terminal interativo, pipe e CI simulado.
  - Verificação: `dotnet test` passa integralmente (482); `dotnet build` termina sem warnings. Página validada em 1440/390 (evidências em `.impeccable/review/`); CLI validada em pipe real (0 escapes) e interatividade coberta pelos testes de tema.
- [x] **T8** — Registrar evidências, atualizar a documentação necessária e encerrar a feature somente após revisão de escopo e arquivos públicos preservados.
  - Verificação: scripts de instalação, URLs e assets sem alteração; `DESIGN.md` + `.impeccable/design.json` registrados; review visual com veredito *ship*; branch aberta em PR contra `main`.
