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
- [ ] **Zero perda de funcionalidade**: tudo que funciona hoje termina em um
      dos dois repos, com quality gates verdes (`dotnet build` sem warnings,
      `dotnet test` 100%)
- [ ] Decisões de produto/arquitetura documentadas e rastreáveis nesta spec

## Out of Scope

- Novas funcionalidades de produto (novos formatos, novos sites, novos
  recursos de download) — esta migração apenas move/reorganiza
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
| D2 | Arquitetura do motor                                         | **Híbrido**: motor como library C# embutida na CLI (standalone) + **Engine Server** headless servindo web e desktop | ✅ decided (2026-09-07) |
| D3 | Destino da `develop` do Cutube após o PR para `main`         | **Reset como ÚLTIMO estágio da migração**: só quando tudo estiver movido e validado no Orotube (Track C) | ✅ decided (2026-09-07) |
| D4 | Estratégia de criação do repo Orotube                        | **Repo novo** com o histórico do `develop` atual pushado (sem vínculo de fork GitHub) | ✅ decided (2026-09-07) |
| D5 | Namespaces `Cutube.*` → `Orotube.*` no novo repo             | **Sim**, em feature dedicada (migração mecânica ampla)                  | ⚠️ open (default provisório) |
| D6 | Turborepo/pnpm no Cutube CLI-only                            | **Remover**: repo volta a ser solução .NET simples (sem workspace Node) | ⚠️ open (default provisório) |
| D7 | CLI do Orotube                                               | **Embute o motor como library** (paridade standalone com Cutube CLI); fila/servidor ficam para web/desktop | ✅ decided (2026-09-07) |
| D8 | Integração Nx ↔ .NET                                         | Preferir `@nx-dotnet/core`; fallback: targets `nx:run-commands`         | open (decidir no Design de B2) |
| D9 | Versionar `.specs/` no git                                   | **Sim** — remover `.specs/` do `.gitignore` (executa intent da tarefa aberta Cutube-vxo.3) | ⚠️ open (default provisório) |

> Decisões D1–D4 e D7 confirmadas pelo usuário em 2026-09-07. D3 inclui a
> diretriz explícita: **o reset do repo atual é o último estágio**, executado
> apenas quando todo o sistema distribuído já estiver movido e validado no
> Orotube (Track C). Revisar os `open` restantes (D5, D6, D9) antes do Design
> das features; D8 decide-se no Design de B2.

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
| Task tracking                                | beads (`.beads/`, branch `beads-sync`)     | Fica no **Cutube**; Orotube inicia `.beads` novo |

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
  (`ci.yml`) sem jobs de web/node; install hooks preservados
- **A5 `cutube/pr-merge`**: PR do branch CLI-only → `main`; após merge,
  fechar tarefas beads abertas `Cutube-vxo.20` (smoke test) e `Cutube-vxo.22`
  (docs) como obsoletas pela migração, e as já superadas `vxo.1`/`vxo.3`/
  `vxo.19`; atualizar handoff. ⚠️ A `develop` **não** é resetada aqui — o
  reset é o estágio final (Track C), com todo o sistema distribuído já
  movido e validado no Orotube

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

**Goal**: repo Orotube com histórico preservado, orquestrado por Nx,
contendo motor compartilhado (library + Engine Server), web com RabbitMQ,
desktop leve e CLI — todo o sistema distribuído atual funcionando lá.

**Features**:

- **B1 `orotube/repo-bootstrap`** (P1): criar repo novo `Orotube`, pushar o
  histórico do `develop` atual, renomear solução (`cutube.sln` →
  `Orotube.sln`) e identidade do repo (README provisório apontando para esta
  spec)
- **B2 `orotube/nx-monorepo`** (P1): Nx + pnpm, layout `apps/` + `libs/`,
  integrar os projetos .NET (D8), CI skeleton
- **B3 `orotube/engine-library`** (P1): consolidar o **motor** =
  `Core + Domain + Application + Infrastructure` como library única
  (`Orotube.Engine.*`) com namespaces migrados (D5); é a base que CLI, Engine
  Server e (via server) web/desktop consomem
- **B4 `orotube/engine-server`** (P1): host headless do motor — evolução do
  `Cutube.Api` atual: REST + SignalR + producer RabbitMQ; health checks
- **B5 `orotube/worker`** (P1): port do `Cutube.Worker` (consumer RabbitMQ,
  retry, DLQ, status tracking)
- **B6 `orotube/web`** (P1): port do `Cutube.Web` (Next.js) apontando para o
  Engine Server; e2e Playwright portados
- **B7 `orotube/desktop-tauri`** (P2): app Tauri (D1) envolvendo a UI do web
  app (componentes React compartilhados), conectando ao Engine Server; alvo de
  levezura: instalador ≤ 20MB
- **B8 `orotube/cli`** (P2): CLI do Orotube embutindo o motor como library
  (D7), paridade de features com a Cutube CLI
- **B9 `orotube/infra-cicd`** (P1): docker-compose (rabbitmq, engine-server,
  worker, web), scripts de dev, GitHub Actions (build/test/release)
- **B10 `orotube/docs-handoff`** (P1): README de arquitetura do monorepo,
  setup local, handoff; README do Cutube ganha ponteiro para Orotube (sistema
  distribuído mudou de casa)

**Acceptance Criteria**:
1. WHEN `git log` no Orotube THEN o histórico do `develop` do Cutube SHALL
   estar preservado (commits/blame originais visíveis)
2. WHEN `nx run-many --target=build` (e `test`) THEN todos os projetos SHALL
   buildar/testar orquestrados pelo Nx
3. WHEN `docker compose up` THEN rabbitmq + engine-server + worker + web SHALL
   subir healthy e um fluxo de download ponta-a-ponta (enqueue → progresso
   SignalR → arquivo baixado) SHALL completar
4. WHEN build do desktop THEN o artefato SHALL ter ≤ 20MB e carregar a UI
   conectada ao Engine Server
5. WHEN CLI do Orotube executa o checklist de paridade (download, corte, MP3,
   nome/dir custom, tempo flexível, validações, resume, CTRL+C) THEN todas
   SHALL passar igual à Cutube CLI
6. WHEN quality gates THEN `dotnet build` SHALL ter zero warnings e `dotnet
   test` SHALL passar 100% no monorepo

---

### Track C — Encerramento: reset do Cutube (último estágio) (P1)

**Goal**: desativar o Cutube como casa do sistema distribuído **somente
depois** de o Orotube estar completo e validado — o reset da `develop` é a
última ação da migração (diretriz D3).

**Features**:

- **C1 `cutube/reset-develop`** (última ação da migração): confirmar que todas
  as features das Tracks A e B estão concluídas e validadas nos dois repos;
  então resetar `develop` = `main`, limpar branches remotas obsoletas,
  executar `bd sync` final e atualizar o handoff nos dois repos

**Acceptance Criteria**:
1. WHEN qualquer feature de A ou B ainda não está validada THEN C1 SHALL NOT
   ser executada (gate explícito de conclusão)
2. WHEN C1 executa THEN `develop` SHALL ficar igual a `main` com `git status`
   limpo e branches obsoletas removidas
3. WHEN alguém precisa do sistema distribuído após o reset THEN o repo
  Orotube SHALL ter tudo funcionando (`docker compose up` healthy, fluxo e2e
   verde) — nada fica acessível apenas via histórico do Cutube

---

## Ordering Constraints

- **C1 é o último estágio de toda a migração**: depende de TODAS as features
  de A (A1–A5) e B (B1–B10) concluídas e validadas — o reset da `develop`
  só acontece com tudo já movido e funcionando no Orotube
- **B1 antes de qualquer remoção/destruição**: o histórico do `develop` deve
  estar pushado no Orotube antes mesmo das Tracks avançarem em remoções
- Dentro da Track A: A1 → A2 → A3 → A4 → A5 (sequencial); A5 só faz merge do
  PR CLI-only em `main` — não toca na `develop`
- Dentro da Track B: B1 → B2 → B3; B4, B5, B6, B8 dependem de B3; B7 depende
  de B4 e B6; B9 depende de B2; B10 por último (dentro da track)
- Entre tracks: A e B podem evoluir em paralelo; C sempre por último

## Cross-cutting Edge Cases

- WHEN os dois repos rodam compose simultaneamente na mesma máquina THEN
  portas/nomes de container do Orotube SHALL ser distintos dos atuais do
  Cutube
- WHEN usuário existente da CLI atualiza o Cutube pós-split THEN dados em
  `~/.local/share/Cutube` SHALL ser preservados (apps Orotube usam
  `~/.local/share/Orotube`)
- WHEN beads THEN o histórico de tarefas fica no Cutube; Orotube inicia
  `.beads` novo e as features desta spec viram tarefas lá via
  `spec-driven-dev`
- WHEN paths relativos de bundling do yt-dlp após os moves THEN SHALL ser
  validados em B3/B4 (regressão conhecida de reestruturações anteriores)
- WHEN issues Linear (ORO-XXX) THEN permanecem associadas ao projeto Cutube;
  criar projeto Orotube no Linear só quando a Track B iniciar (nunca
  automático, por política do AGENTS.md)

## Success Criteria

- [ ] Cutube: clone limpo de `main` → `dotnet run` funciona offline; gates
      verdes; nenhuma referência ativa ao sistema distribuído
- [ ] Orotube: Nx orquestra build/test; compose sobe o sistema completo;
      fluxo de download e2e verde; desktop ≤ 20MB (quando B7 entrar)
- [ ] Checklist de paridade da CLI 100% nos dois repos (Cutube `main` e,
      quando B8 entrar, Orotube CLI)
- [ ] `develop` do Cutube == `main`; handoff atualizado nos dois repos; beads
      sincronizado e tarefas obsoletas fechadas
