# Migração Cutube → Orotube — Especificação Macro

**Tipo**: Épico / Migração de repositórios
**Status**: Draft — aguardando aprovação antes de quebrar em features
**Data**: 2026-09-07

---

## Problem Statement

O Cutube acumulou dois produtos distintos em um único repositório: uma CLI
standalone de download de vídeos e um sistema distribuído (API + Worker +
RabbitMQ + Web Next.js). A `main` está congelada em ago/2023 enquanto a
`develop` está 291 commits à frente, misturando as duas identidades. Isso
dificulta tanto a evolução da CLI (que carrega peso de docker/web/fila quanto
do usuário que só quer baixar vídeo) quanto a do sistema distribuído (que não
pode mudar livremente sem quebrar a CLI).

## Goals

- [ ] Cutube volta a ser **apenas CLI**: PR para `main` contendo somente as
      funcionalidades CLI desenvolvidas em `develop` (+ libs de suporte)
- [ ] Orotube nasce como **repo novo** (monorepo **Nx**) com o histórico do
      `develop` preservado, abrigando: motor (engine), CLI, web app com fila
      RabbitMQ e app desktop leve
- [ ] Desktop com **modo duplo**: standalone (motor local, sem fila/servidor —
      para quem baixa esporadicamente) e ecossistema (fila RabbitMQ via
      Engine Server — para o uso robusto), selecionável pelo usuário
- [ ] **Zero perda de funcionalidade**: tudo que funciona hoje termina em um
      dos dois repos, com quality gates verdes (`dotnet build` sem warnings,
      `dotnet test` 100%)
- [ ] Decisões de produto/arquitetura documentadas e rastreáveis nesta spec

## Out of Scope

- Novas funcionalidades de produto (novos formatos, novos sites, novos
  recursos de download) — esta migração apenas move/reorganiza. **Exceção**:
  os dois modos do desktop (D10) fazem parte do escopo da migração
- Publicação do motor como pacote NuGet público (consumo interno via
  referência de projeto no monorepo)
- Migração de dados/usuários (não há dados persistentes de usuário)
- App mobile
- Manter o sistema distribuído funcionando **no repo Cutube** após o split

---

## Decisions

| #  | Decisão                                                      | Escolha                                                                 | Status              |
|----|--------------------------------------------------------------|-------------------------------------------------------------------------|---------------------|
| D1 | Tecnologia do app desktop                                    | **Tauri** (WebView nativo, binário ~5-15MB, reaproveita UI React do web) | ✅ decided (2026-09-07) |
| D2 | Arquitetura do motor                                         | **Híbrido**: motor como library C# embutida na CLI (standalone) e hospedável localmente no desktop (modo standalone) + **Engine Server** headless servindo web e desktop em modo fila | ✅ decided (2026-09-07) |
| D3 | Destino da `develop` do Cutube                                 | **Deletada** (não resetada) quando os gates da Track C confirmarem: backup verificado no Orotube + PR único CLI-only mergeado em `main`. Cutube passa a viver só em `main` | ✅ decided (2026-09-07 — revisão do "reset no final": o backup na branch de referência já preserva tudo) |
| D4 | Estratégia de criação do repo Orotube                        | **Repo novo no Azure DevOps**, remote `git@ssh.dev.azure.com:v3/oroborus/oroborus-auto/orotube`, com **mainline nova (histórico limpo)** + branch de referência `legacy/cutube-develop` contendo o histórico do `develop` atual (backup fiel e fonte dos ports) | ✅ decided (2026-09-07) |
| D5 | Namespaces `Cutube.*` → `Orotube.*` no novo repo             | **Sim**, em feature dedicada (migração mecânica ampla)                  | ⚠️ open (default provisório) |
| D6 | Turborepo/pnpm no Cutube CLI-only                            | **Remover**: repo volta a ser solução .NET simples (sem workspace Node) | ⚠️ open (default provisório) |
| D7 | CLI do Orotube                                               | **Embute o motor como library** (paridade standalone com Cutube CLI); fila/servidor ficam para web/desktop | ✅ decided (2026-09-07) |
| D8 | Integração Nx ↔ .NET                                         | Preferir `@nx-dotnet/core`; fallback: targets `nx:run-commands`         | open (decidir no Design de B2) |
| D9 | Versionar `.specs/` no git                                   | **Sim** — remover `.specs/` do `.gitignore` (executa intent da tarefa aberta Cutube-vxo.3) | ⚠️ open (default provisório) |
| D10 | Modos do app desktop                                          | **Dois modos selecionáveis**: standalone por padrão (motor local embutido, sem RabbitMQ/servidor, funciona offline — para quem baixa esporadicamente) e **fila como opção habilitável** (conecta ao Engine Server + RabbitMQ — para o ecossistema robusto) | ✅ decided (2026-09-07) |
| D11 | Task tracking no Orotube                                      | **Sem bd (beads) e sem taskmaster**: nada de `.beads/`, branch `beads-sync` ou hooks de sync no repo novo. Substituto sugerido: Azure Boards (nativo do Azure DevOps) + specs versionadas em `.specs/` | ✅ decided (2026-09-07) — substituto exato confirmar no B1 |
| D12 | Onde vivem as novidades da migração                           | **Só no Orotube**: spec, planos, skill `spec-driven-dev` atualizada e docs da migração NÃO entram no Cutube (PR #61 fechado sem merge). O Cutube recebe **exatamente um PR** — o sync CLI-only para `main` | ✅ decided (2026-09-07) |

> Decisões D1–D4, D7, D10–D12 confirmadas pelo usuário em 2026-09-07. D3 foi
> revisada: em vez de reset da `develop` no final, a `develop` é **deletada**
> quando o backup estiver verificado no Orotube (branch de referência) e o PR
> único CLI-only estiver mergeado — os ports continuam a partir da branch de
> referência. D12: nada de docs da migração no repo antigo. Revisar os `open`
> restantes (D5, D6, D9) antes do Design das features; D8 decide-se no Design
> de B2.

---

## Current State (baseline da migração)

O que existe e funciona hoje em `develop` (e precisa sobreviver ao split):

| Capacidade                                  | Onde vive hoje                              | Destino               |
|---------------------------------------------|---------------------------------------------|-----------------------|
| CLI standalone (download, corte, MP3, nome/dir custom, tempo flexível, validações, resume/recovery, CTRL+C) | `src/Cutube.Cli` + libs | **Cutube** (A) e **Orotube/cli** (B8) |
| Libs de domínio/infra                        | `src/Cutube.{Core,Domain,Application,Infrastructure}` | Núcleo do **motor** (B3) |
| Contratos RabbitMQ                           | `src/Cutube.Contracts` (RabbitMqOptions, DownloadMessage) | **Orotube** (motor/server) |
| REST API + SignalR                           | `src/Cutube.Api`                           | **Engine Server** (B4) |
| Worker consumer RabbitMQ (retry/DLQ)         | `src/Cutube.Worker`                        | **Orotube/worker** (B5) |
| Web Next.js (dashboard, downloads, dark mode)| `src/Cutube.Web`                           | **Orotube/web** (B6) |
| Modo API da CLI (`--api-url`, `UseApi`, `ApiClient`) | `src/Cutube.Cli/Program.cs`          | **Só Orotube** — remover do Cutube (A2) |
| Docker compose (rabbitmq/api/worker/web), CI, turborepo/pnpm | raiz                                  | Cutube: remover (A1/A3); Orotube: recriar em Nx (B2/B9) |
| Task tracking                                | bd/beads (`.beads/`, branch `beads-sync`)  | Fica no **Cutube**; Orotube **sem bd e sem taskmaster** (D11) |

Dependências verificadas: a CLI referencia Domain, Application, Core,
Infrastructure e Contracts — a referência a Contracts cai junto com o modo API (A2).

---

## Tracks

### Track A — Cutube volta a ser CLI (P1) ⭐

**Goal**: `main` do Cutube contém exatamente a CLI standalone + libs de
suporte, com `develop` resetada para espelhá-la.

**Features** (cada uma vira `.specs/orotube-migration/<slug>/` depois):

- **A1 `cutube/strip-distributed-projects`**: remover de um branch derivado de
  `develop` os projetos `Cutube.Api`, `Cutube.Web`, `Cutube.Worker`,
  `Cutube.Contracts`, testes correspondentes (`Cutube.Api.Tests`,
  `Cutube.Web.E2E`, `Cutube.Worker.Tests`, `Cutube.Tests` de integraação de
  domínio se acoplados), `docker/`, `docker-compose.yml`, docs de
  rabbitmq/setup distribuído
- **A2 `cutube/strip-api-mode`**: remover da CLI o modo cliente de API
  (`--api-url`, `UseApi`, `ApiClient`, `HttpClientService` associado) e a
  referência a `Cutube.Contracts`; CLI fica standalone-only
- **A3 `cutube/simplify-build`**: remover Turborepo/pnpm/workspace (root
  `package.json`, `pnpm-workspace.yaml`, `turbo.json`, `package.json` por
  projeto, scripts multi-serviço) — repo volta a ser solução .NET simples
- **A4 `cutube/docs-ci`**: README/CHANGELOG/roadmap refletem CLI-only; CI
  (`ci.yml`) sem jobs de web/node; install hooks preservados; AGENTS.md
  atualizado para o fluxo pós-migração (PRs miram `main`; `develop` deixa de
  existir após a Track C)
- **A5 `cutube/pr-merge`**: **o único PR da migração no repo Cutube** (A1–A4
  compõem esse mesmo PR): branch CLI-only → `main`; após merge, fechar tarefas
  beads abertas `Cutube-vxo.20` (smoke test) e `Cutube-vxo.22` (docs) como
  obsoletas pela migração, e as já superadas `vxo.1`/`vxo.3`/`vxo.19`;
  atualizar handoff. ⚠️ Nenhuma doc/plano/skill da migração entra neste repo
  (D12) e a `develop` não é tocada aqui — a deleção é gateada na Track C

**Acceptance Criteria**:
1. WHEN fresh clone do `main` THEN `dotnet run` SHALL iniciar a CLI standalone,
   offline, sem docker/rabbitmq/web
2. WHEN `dotnet build` THEN SHALL sair com **zero warnings**; WHEN `dotnet
   test` THEN SHALL passar **100%** (suites CLI: `Cutube.Cli.Tests`,
   `Cutube.Domain.Tests`)
3. WHEN busca por Api/Web/Worker/RabbitMQ/docker na árvore do repo THEN SHALL
   retornar apenas histórico git/CHANGELOG (nenhum arquivo ativo)
4. WHEN PR mergeado THEN `main` SHALL conter exatamente a CLI + libs e a
   `develop` SHALL permanecer intacta (fonte do sistema distribuído até a
   Track C concluir)

---

### Track B — Orotube: monorepo Nx com motor, web, fila e desktop (P1 ⭐ / P2)

**Goal**: repo Orotube com **mainline nova** (histórico limpo, orquestrada por
Nx) contendo motor compartilhado (library + Engine Server), web com RabbitMQ,
desktop leve de **modo duplo** (standalone com motor local / fila via Engine
Server) e CLI — e o histórico do `develop` do Cutube preservado na branch de
referência `legacy/cutube-develop` como fonte dos ports.

**Features**:

- **B1 `orotube/repo-bootstrap`** (P1): validar acesso SSH ao Azure DevOps
  (`git@ssh.dev.azure.com:v3/oroborus/oroborus-auto/orotube` — `git
  ls-remote` antes de qualquer push); pushar o `develop` do Cutube para a
  **branch de referência** `legacy/cutube-develop` (backup fiel + fonte dos
  ports); criar a **mainline nova** (`main` e `dev`, histórico limpo) semeada
  com esta spec, a skill `spec-driven-dev` (adaptação sem beads fica para
  B10) e README provisório; renomear solução (`cutube.sln` → `Orotube.sln`)
  na mainline nova; **verificar o backup** (mesmo SHA-1 de HEAD e mesma
  contagem de commits que o `origin/develop` do Cutube)
- **B2 `orotube/nx-monorepo`** (P1): Nx + pnpm, layout `apps/` + `libs/`,
  integrar os projetos .NET (D8), skeleton de CI (Azure Pipelines)
- **B3 `orotube/engine-library`** (P1): consolidar o **motor** =
  `Core + Domain + Application + Infrastructure` como library única
  (`Orotube.Engine.*`) com namespaces migrados (D5); é a base que CLI, Engine
  Server e (via server) web/desktop consomem
- **B4 `orotube/engine-server`** (P1): host headless do motor — evolução do
  `Cutube.Api` atual: REST + SignalR + producer RabbitMQ; health checks. O
  mesmo host em **modo local** (sem RabbitMQ, processamento em background
  in-process — como o `DownloadQueue` pré-Épico 3) atende o desktop
  standalone (D10); empacotamento (sidecar) define-se no Design
- **B5 `orotube/worker`** (P1): port do `Cutube.Worker` (consumer RabbitMQ,
  retry, DLQ, status tracking)
- **B6 `orotube/web`** (P1): port do `Cutube.Web` (Next.js) apontando para o
  Engine Server; e2e Playwright portados
- **B7 `orotube/desktop-tauri`** (P2): app Tauri (D1) envolvendo a UI do web
  app (componentes React compartilhados) com **dois modos** (D10):
  **standalone** (padrão — hospeda o motor localmente, sem RabbitMQ/docker/
  servidor externo, downloads processados em background local, funciona
  offline; para quem baixa esporadicamente) e **fila habilitável** (conecta
  ao Engine Server + RabbitMQ, downloads rastreados no ecossistema; para o
  uso robusto). Mesma UI nos dois modos. Alvo de levezura: instalador ≤ 20MB
- **B8 `orotube/cli`** (P2): CLI do Orotube embutindo o motor como library
  (D7), paridade de features com a Cutube CLI
- **B9 `orotube/infra-cicd`** (P1): docker-compose (rabbitmq, engine-server,
  worker, web), scripts de dev, **Azure Pipelines** (build/test/release —
  CI/CD do Azure DevOps, não GitHub Actions)
- **B10 `orotube/docs-handoff`** (P1): README de arquitetura do monorepo,
  setup local, handoff; **AGENTS.md próprio adaptado ao Azure DevOps**
  (Azure Pipelines em vez de GitHub Actions, fluxo de PR do Azure Repos,
  **sem bd/taskmaster** — D11) documentando as **skills ativas** do repo:
  `spec-driven-dev` copiada de Cutube e adaptada (sem a seção de integração
  com beads); README do Cutube ganha ponteiro para Orotube (sistema
  distribuído mudou de casa)

**Acceptance Criteria**:
1. WHEN `git log` na branch `legacy/cutube-develop` do Orotube THEN o
   histórico do `develop` do Cutube SHALL estar preservado por completo
   (mesmos commits/blame)
2. WHEN `nx run-many --target=build` (e `test`) THEN todos os projetos SHALL
   buildar/testar orquestrados pelo Nx
3. WHEN `docker compose up` THEN rabbitmq + engine-server + worker + web SHALL
   subir healthy e um fluxo de download ponta-a-ponta (enqueue → progresso
   SignalR → arquivo baixado) SHALL completar
4. WHEN build do desktop THEN o artefato SHALL ter ≤ 20MB e carregar a UI
   conectada ao Engine Server
5. WHEN desktop em modo standalone THEN o app SHALL funcionar offline (sem
   servidor externo, sem RabbitMQ/docker), processando downloads localmente
   com progresso na UI; WHEN o usuário habilita a fila THEN o app SHALL
   passar a enfileirar via Engine Server mantendo a mesma UI
6. WHEN CLI do Orotube executa o checklist de paridade (download, corte, MP3,
   nome/dir custom, tempo flexível, validações, resume, CTRL+C) THEN todas
   SHALL passar igual à Cutube CLI
7. WHEN quality gates THEN `dotnet build` SHALL ter zero warnings e `dotnet
   test` SHALL passar 100% no monorepo

---

### Track C — Encerramento: deleção da develop do Cutube (P1)

**Goal**: com o backup do `develop` seguro no Orotube e o sync CLI mergeado,
o Cutube passa a viver apenas em `main` (D3/D12).

**Features**:

- **C1 `cutube/delete-develop`** (gates explícitos, sem exigir Track B
  completa — a branch de referência já preserva tudo e os ports continuam a
  partir dela): verificar gate 1 — backup `legacy/cutube-develop` validado no
  Orotube (SHA-1 e contagem de commits idênticos ao `origin/develop`); gate
  2 — PR único CLI-only mergeado em `main`; então deletar `develop` (local +
  remota), apontar `origin/HEAD` para `main`, limpar branches remotas
  obsoletas restantes, `bd sync` final e handoff atualizado nos dois repos

**Acceptance Criteria**:
1. WHEN qualquer gate (backup validado OU PR CLI mergeado) não confirmado
   THEN C1 SHALL NOT ser executada
2. WHEN C1 executa THEN o Cutube SHALL ter apenas `main` como branch
   principal, `origin/HEAD` apontando para `main` e `git status` limpo
3. WHEN alguém precisa do código distribuído THEN a branch
   `legacy/cutube-develop` no Orotube SHALL conter o histórico completo
   (blame preservado) e as features portadas SHALL estar na mainline nova

---

## Ordering Constraints

- **B1 é o primeiro passo executável da migração** (validação SSH + backup
  `legacy/cutube-develop` + mainline nova): destrava C1 e nada destrutivo
  acontece antes dele
- **C1 (deleção da `develop`) exige**: backup verificado no Orotube (B1) E PR
  único CLI-only mergeado em `main` (A5) — não exige a Track B completa; os
  ports seguem a partir da branch de referência
- Dentro da Track A: A1 → A2 → A3 → A4 → A5 (sequencial, compondo **um único
  PR** para `main`)
- Dentro da Track B: B1 → B2 → B3; B4, B5, B6, B8 dependem de B3 (fonte:
  `legacy/cutube-develop`); B7 depende de B4 e B6; B9 depende de B2; B10 por
  último (dentro da track)
- Entre tracks: A e B podem evolir em paralelo após B1

## Cross-cutting Edge Cases

- WHEN os dois repos rodam compose simultaneamente na mesma máquina THEN
  portas/nomes de container do Orotube SHALL ser distintos dos atuais do
  Cutube
- WHEN push inicial do histórico para `ssh.dev.azure.com` THEN a chave SSH
  SHALL estar configurada e validada antes (`git ls-remote` retorna as refs
  do repo `v3/oroborus/oroborus-auto/orotube`)
- WHEN backup do `develop` no Orotube THEN SHALL ser verificado por SHA-1 de
  HEAD e contagem de commits ANTES de qualquer deleção no Cutube
- WHEN desktop standalone THEN nada SHALL exigir docker/RabbitMQ/Engine
  Server rodando (zero dependências externas além do próprio app)
- WHEN troca de modo no desktop (standalone ↔ fila) com download em
  andamento THEN o app SHALL lidar graciosamente (concluir localmente e/ou
  avisar antes de alternar — comportamento exato define-se no Design de B7)
- WHEN usuário existente da CLI atualiza o Cutube pós-split THEN dados em
  `~/.local/share/Cutube` SHALL ser preservados (apps Orotube usam
  `~/.local/share/Orotube`)
- WHEN task tracking THEN bd/beads fica só no Cutube; Orotube **não**
  inicializa `.beads/` nem usa taskmaster (D11) — as features desta spec
  viram itens no tracker escolhido (Azure Boards ou `.specs/` versionadas) e
  specs filhas seguem o fluxo da skill `spec-driven-dev`
- WHEN paths relativos de bundling do yt-dlp após os moves THEN SHALL ser
  validados em B3/B4 (regressão conhecida de reestruturações anteriores)
- WHEN issues Linear (ORO-XXX) THEN permanecem associadas ao projeto Cutube;
  criar projeto Orotube no Linear só quando a Track B iniciar (nunca
  automático, por política do AGENTS.md)

## Success Criteria

- [ ] Cutube: clone limpo de `main` → `dotnet run` funciona offline; gates
      verdes; nenhuma referência ativa ao sistema distribuído
- [ ] Orotube: Nx orquestra build/test; compose sobe o sistema completo;
      fluxo de download e2e verde; desktop ≤ 20MB funcionando standalone
      offline **e** em modo fila (quando B7 entrar)
- [ ] Checklist de paridade da CLI 100% nos dois repos (Cutube `main` e,
      quando B8 entrar, Orotube CLI)
- [ ] `develop` do Cutube deletada (repo vive só em `main`); backup íntegro
      em `legacy/cutube-develop` no Orotube; handoff atualizado nos dois
      repos; beads sincronizado e tarefas obsoletas fechadas
