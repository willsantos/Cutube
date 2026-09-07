# Handoff

## 2026-09-07 (migracao Orotube / Track A)

### Contexto
- Branch: `feat/track-a-cli-only` → PR #62 para `main` (o unico PR da migracao neste repo)
- **Beads (bd) removido do repo**: `.beads/`, hooks bd e branch `beads-sync` deletados. Issues → Linear; planejamento → specs versionadas. `AGENTS.md` atualizado.
- NOTA: o CLI `bd` nao esta instalado nesta maquina; ficou moot com a remocao

### O que foi concluido (Track A: A1–A4)
- Projetos distribuidos removidos (Api/Web/Worker/Contracts/testes/docker) — sistema distribuido migrou para o repo Orotube (Azure DevOps `oroborus/oroborus-auto/orotube`)
- Modo API da CLI removido (`--api-url`, `ApiClient`, `UseApi`); CLI standalone-only
- Turborepo/pnpm removidos; repo voltou a ser solucao .NET pura
- Docs/CI atualizados (README, CHANGELOG, AGENTS.md, docker-build.yml removido)
- Quality gates: `dotnet build -warnaserror` 0 warnings; `dotnet test` 364 passed / 0 failed (1 skip pre-existente justificado)
- Backup do develop verificado no Orotube: branch `legacy/cutube-develop` (SHA 17f4663e, 303 commits)

### Proximo passo ao retornar
- Review/merge do PR #62
- Apos merge: Track C liberada (deletar `develop`; gate de backup ja verificado)
- Orotube: seguir com B2 (Nx monorepo) a partir da mainline nova; spec macro em `.specs/orotube-migration/spec.md` no repo Orotube

## 2026-02-12 (pausa)

### Contexto
- Branch atual: `develop`
- Estado do repo: limpo (`git status` sem alteracoes)
- Remote: `develop` sincronizado com `origin/develop`

### O que foi concluido
- PR #47 mergeado em `develop` (`2731c66`)
- Fase 5.3 do Epico 5 concluida com quality gates verdes
- Task Beads `Cutube-vmv.3` fechada
- `bd sync` executado apos fechamento
- Branch de trabalho `fix/Cutube-vmv.3-quality-gates` removida

### O que entrou no merge
1. `chore(worker): align Serilog packages for hosted worker`
2. `refactor(api): remove deprecated WithOpenApi endpoint calls`
3. `chore(cli): remove redundant System.Net.Http.Json package`

### Validacao executada
- `dotnet test`: passou
- `dotnet build`: passou com 0 warnings e 0 errors

### Proximo passo sugerido ao retornar
- Rodar `bd ready` para selecionar a proxima task do epico (ou novo backlog pronto)
