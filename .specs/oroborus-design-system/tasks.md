# Tasks: oroborus-design-system — identidade Oroborus no Cutube

> Execução via fluxo spec-driven. Esta etapa registra o plano; nenhuma implementação visual foi feita ainda.

## Fase 1 — auditoria e fundação

- [ ] **T1** — Auditar `docs/index.html`, `IConsoleService`, `ConsoleService`, `Program`, `Menu`, `ProgressBar` e pontos de mensagem/estado para mapear a superfície visual sem alterar comportamento.
  - Verificação: inventário de cores, fontes, seletores, métodos de saída e testes impactados.
- [ ] **T2** — Definir tokens locais para a página e uma abstração mínima de estilo ANSI para a CLI, com detecção de output redirecionado e fallback plain text.
  - Verificação: nenhum escape ANSI aparece em pipe/CI; tokens correspondem ao design aprovado.

## Fase 2 — GitHub Pages

- [ ] **T3** — Reescrever a paleta, tipografia, superfícies, CTA, links, estados, foco e responsividade de `docs/index.html` para a identidade Oroborus.
  - Verificação: paleta azul atual removida como identidade global; links de instalação/documentação/release preservados.
- [ ] **T4** — Adicionar gradiente e efeitos de interação contidos, incluindo `prefers-reduced-motion`.
  - Verificação: sem overflow móvel e sem perda de legibilidade com efeitos reduzidos/desativados.

## Fase 3 — CLI

- [ ] **T5** — Aplicar estilos Oroborus aos pontos de saída existentes usando `IConsoleService`, sem reescrever mensagens ou alterar códigos de saída.
  - Verificação: títulos, progresso, sucesso, aviso e erro com hierarquia consistente; fallback sem cor equivalente ao comportamento atual.
- [ ] **T6** — Atualizar testes unitários da apresentação para cobrir terminal com cor e saída redirecionada.
  - Verificação: nenhum código ANSI em saída não interativa; mensagens e progressão permanecem compatíveis.

## Fase 4 — gates e encerramento

- [ ] **T7** — Validar manualmente a página em desktop/mobile e a CLI em terminal interativo, pipe e CI simulado.
  - Verificação: `dotnet test` passa integralmente; `dotnet build` termina sem warnings.
- [ ] **T8** — Registrar evidências, atualizar a documentação necessária e encerrar a feature somente após revisão de escopo e arquivos públicos preservados.
  - Verificação: scripts de instalação, URLs e assets sem alteração indevida; branch pronta para PR contra `main`.
