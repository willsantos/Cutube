# Fase 4.1: Implementar CI/CD com GitHub Actions

**Status:** 🎯 Planejamento
**Épico:** Cutube-858 (Épico 4: Preparação para Deploy)
**Duração:** 4-6 horas
**Responsável:** DevOps Engineer
**Prioridade:** 🔥 Alta
**Dependência:** ✅ Todas as fases anteriores completas

---

## Objetivo

Implementar pipeline de CI/CD usando GitHub Actions para automatizar build, testes, release de Docker images e binários CLI do Cutube.

**Benefícios:**
- ✅ Deploy automático - Push para main dispara build
- ✅ Qualidade garantida - Testes em todo PR
- ✅ Multi-plataforma - Binários para Linux, macOS, Windows
- ✅ Multi-arquitetura - Docker images amd64 e arm64

---

## Visão Arquitetural

### Pipeline CI/CD

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    GitHub Actions Workflow                              │
│                                                                         │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │                     Trigger Events                              │   │
│  │  • Pull Request → CI Workflow                                   │   │
│  │  • Push to main → Docker Build                                  │   │
│  │  • Tag v* → Release                                             │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                                                                         │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │                   CI Workflow (ci.yml)                           │   │
│  │                                                                   │   │
│  │   Checkout → Setup .NET → Restore → Build → Tests → Coverage     │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                                                                         │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │              Docker Build (docker-build.yml)                     │   │
│  │                                                                   │   │
│  │   Build API Image → Build Worker Image → Build Web Image         │   │
│  │   Push to GHCR (linux/amd64, linux/arm64)                       │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                                                                         │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │               Release Workflow (release.yml)                      │   │
│  │                                                                   │   │
│  │   Build CLI (linux-amd64/arm64, macos-amd64/arm64, windows)      │   │
│  │   Create GitHub Release with binaries                             │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## Tarefas

### 4.1.1 Criar Workflow de CI

**Estimativa:** 1 hora

**Arquivo:** `.github/workflows/ci.yml`

```yaml
name: CI

on:
  push:
    branches: [main, dev, develop]
  pull_request:
    branches: [main, dev, develop]

jobs:
  build-and-test:
    runs-on: ubuntu-latest

    steps:
      - name: Checkout code
        uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'

      - name: Cache NuGet packages
        uses: actions/cache@v4
        with:
          path: ~/.nuget/packages
          key: ${{ runner.os }}-nuget-${{ hashFiles('**/*.csproj') }}
          restore-keys: |
            ${{ runner.os }}-nuget-

      - name: Restore dependencies
        run: dotnet restore

      - name: Build solution
        run: dotnet build --configuration Release --no-restore

      - name: Run tests
        run: dotnet test --no-build --verbosity normal --logger "trx;LogFileName=test-results.trx"

      - name: Upload test results
        uses: actions/upload-artifact@v4
        if: always()
        with:
          name: test-results
          path: '**/TestResults/*.trx'
```

**Checklist:**
- [ ] Criar diretório `.github/workflows/`
- [ ] Criar arquivo `ci.yml`
- [ ] Configurar trigger em PRs
- [ ] Executar testes
- [ ] Upload de test results

**Critérios de aceito:**
- ✅ CI executa automaticamente em todo PR
- ✅ Build da solution sem erros
- ✅ Todos os testes executam
- ✅ Test results disponíveis

---

### 4.1.2 Criar Workflow de Docker Build

**Estimativa:** 2 horas

**Arquivo:** `.github/workflows/docker-build.yml`

```yaml
name: Docker Build

on:
  push:
    branches: [main]
    tags: ['v*']
  pull_request:
    branches: [main]

env:
  REGISTRY: ghcr.io

jobs:
  build-api:
    runs-on: ubuntu-latest
    permissions:
      contents: read
      packages: write

    steps:
      - name: Checkout
        uses: actions/checkout@v4

      - name: Set up Docker Buildx
        uses: docker/setup-buildx-action@v3

      - name: Login to GHCR
        uses: docker/login-action@v3
        with:
          registry: ${{ env.REGISTRY }}
          username: ${{ github.actor }}
          password: ${{ secrets.GITHUB_TOKEN }}

      - name: Extract metadata
        id: meta
        uses: docker/metadata-action@v5
        with:
          images: ${{ env.REGISTRY }}/${{ github.repository }}/api
          tags: |
            type=ref,event=branch
            type=semver,pattern={{version}}
            type=sha

      - name: Build and push API
        uses: docker/build-push-action@v5
        with:
          context: .
          file: ./src/Cutube.Api/Dockerfile
          push: ${{ github.event_name != 'pull_request' }}
          tags: ${{ steps.meta.outputs.tags }}
          labels: ${{ steps.meta.outputs.labels }}
          platforms: linux/amd64,linux/arm64

  build-worker:
    runs-on: ubuntu-latest
    permissions:
      contents: read
      packages: write

    steps:
      - name: Checkout
        uses: actions/checkout@v4

      - name: Set up Docker Buildx
        uses: docker/setup-buildx-action@v3

      - name: Login to GHCR
        uses: docker/login-action@v3
        with:
          registry: ${{ env.REGISTRY }}
          username: ${{ github.actor }}
          password: ${{ secrets.GITHUB_TOKEN }}

      - name: Extract metadata
        id: meta
        uses: docker/metadata-action@v5
        with:
          images: ${{ env.REGISTRY }}/${{ github.repository }}/worker
          tags: |
            type=ref,event=branch
            type=semver,pattern={{version}}
            type=sha

      - name: Build and push Worker
        uses: docker/build-push-action@v5
        with:
          context: .
          file: ./src/Cutube.Worker/Dockerfile
          push: ${{ github.event_name != 'pull_request' }}
          tags: ${{ steps.meta.outputs.tags }}
          labels: ${{ steps.meta.outputs.labels }}
          platforms: linux/amd64,linux/arm64

  build-web:
    runs-on: ubuntu-latest
    permissions:
      contents: read
      packages: write

    steps:
      - name: Checkout
        uses: actions/checkout@v4

      - name: Set up Docker Buildx
        uses: docker/setup-buildx-action@v3

      - name: Login to GHCR
        uses: docker/login-action@v3
        with:
          registry: ${{ env.REGISTRY }}
          username: ${{ github.actor }}
          password: ${{ secrets.GITHUB_TOKEN }}

      - name: Extract metadata
        id: meta
        uses: docker/metadata-action@v5
        with:
          images: ${{ env.REGISTRY }}/${{ github.repository }}/web
          tags: |
            type=ref,event=branch
            type=semver,pattern={{version}}
            type=sha

      - name: Build and push Web
        uses: docker/build-push-action@v5
        with:
          context: ./cutube-web
          push: ${{ github.event_name != 'pull_request' }}
          tags: ${{ steps.meta.outputs.tags }}
          labels: ${{ steps.meta.outputs.labels }}
          platforms: linux/amd64,linux/arm64
```

**Checklist:**
- [ ] Criar arquivo `docker-build.yml`
- [ ] Configurar GHCR
- [ ] Criar jobs para API, Worker e Web
- [ ] Configurar multi-platform (amd64, arm64)
- [ ] Testar build em PR

**Critérios de aceito:**
- ✅ Docker images buildam sem erros
- ✅ Images publicadas no GHCR
- ✅ Multi-arch support

---

### 4.1.3 Criar Workflow de Release

**Estimativa:** 1-2 horas

**Arquivo:** `.github/workflows/release.yml`

```yaml
name: Release

on:
  push:
    tags:
      - 'v*'

permissions:
  contents: write

jobs:
  release-cli:
    runs-on: ${{ matrix.os }}
    strategy:
      matrix:
        include:
          - os: ubuntu-latest
            target: linux-x64
            artifact_name: cutube
            asset_name: cutube-linux-amd64
          - os: ubuntu-latest
            target: linux-arm64
            artifact_name: cutube
            asset_name: cutube-linux-arm64
          - os: macos-latest
            target: osx-x64
            artifact_name: cutube
            asset_name: cutube-macos-amd64
          - os: macos-latest
            target: osx-arm64
            artifact_name: cutube
            asset_name: cutube-macos-arm64
          - os: windows-latest
            target: win-x64
            artifact_name: cutube.exe
            asset_name: cutube-windows-amd64.exe

    steps:
      - name: Checkout
        uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'

      - name: Publish CLI
        run: |
          dotnet publish src/cutube/cutube.csproj \
            -c Release \
            -r ${{ matrix.target }} \
            --self-contained true \
            -p:PublishSingleFile=true \
            -p:PublishTrimmed=true \
            -o ./publish

      - name: Package (Unix)
        if: matrix.os != 'windows-latest'
        run: |
          cd publish
          tar -czf ../${{ matrix.asset_name }}.tar.gz ${{ matrix.artifact_name }}

      - name: Package (Windows)
        if: matrix.os == 'windows-latest'
        run: |
          cd publish
          Compress-Archive -Path ${{ matrix.artifact_name }} -DestinationPath ../${{ matrix.asset_name }}.zip

      - name: Upload artifacts
        uses: actions/upload-artifact@v4
        with:
          name: ${{ matrix.asset_name }}
          path: |
            ${{ matrix.asset_name }}.*

  create-release:
    needs: release-cli
    runs-on: ubuntu-latest

    steps:
      - name: Download all artifacts
        uses: actions/download-artifact@v4
        with:
          path: ./artifacts

      - name: Create Release
        uses: softprops/action-gh-release@v1
        with:
          files: |
            artifacts/**/*.tar.gz
            artifacts/**/*.zip
          generate_release_notes: true
          draft: false
          prerelease: ${{ contains(github.ref, '-rc') || contains(github.ref, '-beta') }}
```

**Checklist:**
- [ ] Criar arquivo `release.yml`
- [ ] Configurar matrix build
- [ ] Empacotar binários
- [ ] Criar release automático

**Critérios de aceito:**
- ✅ CLI binários gerados para todas plataformas
- ✅ Release criado automaticamente em tags v*

---

## Qualidade Gates

**ANTES de considerar esta fase completa:**

- [ ] **CI Workflow** - PR triggers build e testes
- [ ] **Test Results** - Disponíveis como artifacts
- [ ] **Docker Build** - Multi-arch images publicadas
- [ ] **Release Workflow** - Binários gerados
- [ ] **Teste Manual** - Criar tag e verificar release

---

## Cronograma

| Tarefa | Estimativa | Dependencies |
|--------|-----------|--------------|
| 4.1.1 CI Workflow | 1h | Nenhuma |
| 4.1.2 Docker Build | 2h | Nenhuma |
| 4.1.3 Release Workflow | 1-2h | Nenhuma |

**Total:** 4-5 horas

---

## Tecnologias

- **GitHub Actions** - CI/CD
- **Docker Buildx** - Multi-platform
- **GHCR** - Container Registry
- **.NET 10** - CLI publishing

---

## Entregáveis

- [ ] `.github/workflows/ci.yml`
- [ ] `.github/workflows/docker-build.yml`
- [ ] `.github/workflows/release.yml`
- [ ] CI testado em PR
- [ ] Release testado com tag

---

**Fim do Plano - Fase 4.1**
