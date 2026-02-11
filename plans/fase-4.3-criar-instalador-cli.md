# Fase 4.3: Criar Instalador para CLI

**Status:** 🎯 Planejamento
**Épico:** Cutube-858 (Épico 4: Preparação para Deploy)
**Duração:** 3-4 horas
**Responsável:** Backend Developer
**Prioridade:** 🔥 Alta
**Dependência:** ✅ Fase 4.1 completa (CI/CD funcionando)

---

## Objetivo

Criar instalador automático para o Cutube CLI que permite aos usuários instalarem a ferramenta com um único comando.

**Benefícios:**
- ✅ One-command install - `curl -sSL https://get.cutube.dev | bash`
- ✅ Multi-plataforma - Linux, macOS, Windows
- ✅ Multi-arquitetura - amd64, arm64
- ✅ Detecção automática - OS e arch
- ✅ Verificação de dependências - yt-dlp, ffmpeg

---

## Visão Arquitetural

### Fluxo de Instalação

```
User executa: curl -sSL https://get.cutube.dev | bash
        ↓
1. Detect Platform (Linux/macOS/Windows, amd64/arm64)
        ↓
2. Get Latest Version from GitHub
        ↓
3. Download Binary from GitHub Releases
        ↓
4. Extract and Install (/usr/local/bin or ~/.local/bin)
        ↓
5. Verify Installation
        ↓
6. Check Dependencies (yt-dlp, ffmpeg)
        ↓
7. Create Config (~/.cutube/config.json)
        ↓
Success!
```

---

## Tarefas

### 4.3.1 Criar Script de Instalação

**Estimativa:** 2 horas

**Arquivo:** `scripts/install.sh`

```bash
#!/bin/bash
# Cutube CLI Installer
# Usage: curl -sSL https://get.cutube.dev | bash

set -e

REPO="willsantos/cutube"
BINARY_NAME="cutube"
INSTALL_DIR="/usr/local/bin"
USE_SUDO=true

GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'

log_info() {
    echo -e "${GREEN}[INFO]${NC} $1"
}

log_warn() {
    echo -e "${YELLOW}[WARN]${NC} $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Detect OS and architecture
detect_platform() {
    local os=""
    local arch=""

    case "$(uname -s)" in
        Linux*)     os="linux";;
        Darwin*)    os="macos";;
        CYGWIN*|MINGW*|MSYS*) os="windows";;
        *)
            log_error "OS não suportado: $(uname -s)"
            exit 1
            ;;
    esac

    case "$(uname -m)" in
        x86_64|amd64)   arch="amd64";;
        arm64|aarch64)  arch="arm64";;
        *)
            log_error "Arch não suportada: $(uname -m)"
            exit 1
            ;;
    esac

    echo "${os}-${arch}"
}

# Get latest version
get_latest_version() {
    local api_url="https://api.github.com/repos/${REPO}/releases/latest"
    local version

    if command -v curl &> /dev/null; then
        version=$(curl -s "$api_url" | grep '"tag_name":' | sed -E 's/.*"([^"]+)".*/\1/')
    elif command -v wget &> /dev/null; then
        version=$(wget -qO- "$api_url" | grep '"tag_name":' | sed -E 's/.*"([^"]+)".*/\1/')
    else
        log_error "curl ou wget necessário"
        exit 1
    fi

    if [ -z "$version" ]; then
        log_error "Não foi possível obter versão"
        exit 1
    fi

    echo "$version"
}

# Download binary
download_binary() {
    local version="$1"
    local platform="$2"
    local temp_dir="$3"

    local download_url
    local file_name

    if [[ "$platform" == *"windows"* ]]; then
        file_name="${BINARY_NAME}-${platform}.exe"
        download_url="https://github.com/${REPO}/releases/download/${version}/${file_name}.zip"
    else
        file_name="${BINARY_NAME}-${platform}"
        download_url="https://github.com/${REPO}/releases/download/${version}/${file_name}.tar.gz"
    fi

    log_info "Baixando ${BINARY_NAME} ${version}..."

    if command -v curl &> /dev/null; then
        curl -sSL "$download_url" -o "${temp_dir}/download.tmp"
    else
        wget -q "$download_url" -O "${temp_dir}/download.tmp"
    fi

    cd "$temp_dir"

    if [[ "$platform" == *"windows"* ]]; then
        unzip -q "download.tmp"
        mv "${file_name}.exe" "${BINARY_NAME}.exe"
    else
        tar -xzf "download.tmp"
        mv "$file_name" "$BINARY_NAME"
    fi

    chmod +x "$BINARY_NAME"
}

# Install binary
install_binary() {
    local temp_dir="$1"
    local binary_path="${temp_dir}/${BINARY_NAME}"

    if [ "$USE_SUDO" = true ] && [ ! -w "$INSTALL_DIR" ]; then
        if ! sudo -v; then
            INSTALL_DIR="$HOME/.local/bin"
            USE_SUDO=false
            mkdir -p "$INSTALL_DIR"
        fi
    fi

    log_info "Instalando em ${INSTALL_DIR}..."

    if [ "$USE_SUDO" = true ] && [ ! -w "$INSTALL_DIR" ]; then
        sudo mv "$binary_path" "${INSTALL_DIR}/${BINARY_NAME}"
        sudo chmod +x "${INSTALL_DIR}/${BINARY_NAME}"
    else
        mv "$binary_path" "${INSTALL_DIR}/${BINARY_NAME}"
        chmod +x "${INSTALL_DIR}/${BINARY_NAME}"
    fi
}

# Check dependencies
check_dependencies() {
    log_info "Verificando dependências..."

    local missing=()

    if ! command -v yt-dlp &> /dev/null && ! command -v youtube-dl &> /dev/null; then
        missing+=("yt-dlp")
    fi

    if ! command -v ffmpeg &> /dev/null; then
        missing+=("ffmpeg")
    fi

    if [ ${#missing[@]} -ne 0 ]; then
        log_warn "Dependências faltando: ${missing[*]}"
        log_info "Para funcionalidade completa, instale:"
        log_info "  macOS: brew install ffmpeg yt-dlp"
        log_info "  Linux: sudo apt install ffmpeg && pip3 install yt-dlp"
    fi
}

# Create config
create_config() {
    local config_dir="$HOME/.cutube"
    mkdir -p "$config_dir"

    if [ ! -f "$config_dir/config.json" ]; then
        cat > "$config_dir/config.json" << 'EOF'
{
  "outputDirectory": "~/Downloads",
  "defaultQuality": "1080p",
  "api": {
    "enabled": false,
    "url": "http://localhost:5000"
  }
}
EOF
    fi
}

# Main
main() {
    echo ""
    echo "╔═══════════════════════════════════════╗"
    echo "║      Cutube CLI Installer             ║"
    echo "╚═══════════════════════════════════════╝"
    echo ""

    PLATFORM=$(detect_platform)
    log_info "Plataforma: ${PLATFORM}"

    VERSION=$(get_latest_version)
    log_info "Versão: ${VERSION}"

    TEMP_DIR=$(mktemp -d)
    trap "rm -rf $TEMP_DIR" EXIT

    download_binary "$VERSION" "$PLATFORM" "$TEMP_DIR"
    install_binary "$TEMP_DIR"

    if command -v "$BINARY_NAME" &> /dev/null; then
        echo ""
        "$BINARY_NAME" --version
    fi

    check_dependencies
    create_config

    echo ""
    log_info "Instalação concluída!"
    echo ""
    echo "Comece com: cutube --help"
    echo ""
}

main "$@"
```

**Checklist:**
- [ ] Criar script
- [ ] Detectar OS/arch
- [ ] Download do GitHub
- [ ] Instalar binário
- [ ] Verificar dependências
- [ ] Criar config

**Critérios de aceito:**
- ✅ `curl ... | bash` funciona
- ✅ Detecta plataformas
- ✅ Instala corretamente

---

### 4.3.2 Criar Script de Release

**Estimativa:** 30 minutos

**Arquivo:** `scripts/release-cli.sh`

```bash
#!/bin/bash
VERSION="${1:-}"

if [ -z "$VERSION" ]; then
    echo "Uso: $0 <version> (ex: v1.0.0)"
    exit 1
fi

echo "🚀 Criando release ${VERSION}..."

if [ -n "$(git status --porcelain)" ]; then
    echo "❌ Working directory não limpo"
    exit 1
fi

git tag -a "$VERSION" -m "Release ${VERSION}"
git push origin "$VERSION"

echo "✅ Tag criada. GitHub Actions vai buildar e publicar."
```

**Checklist:**
- [ ] Criar script
- [ ] Validar versão
- [ ] Criar e pushar tag

**Critérios de aceito:**
- ✅ Script funciona

---

### 4.3.3 Criar Documentação

**Estimativa:** 1 hora

**Arquivo:** `docs/installation.md`

```markdown
# Instalação do Cutube CLI

## Instalação Rápida

### Linux/macOS
```bash
curl -sSL https://get.cutube.dev | bash
```

### Windows (PowerShell)
```powershell
iwr -useb https://get.cutube.dev/win | iex
```

## Instalação Manual

1. Acesse [releases](https://github.com/willsantos/cutube/releases)
2. Baixe o binário para sua plataforma
3. Extraia e mova para o PATH

Exemplo (Linux):
```bash
tar -xzf cutube-linux-amd64.tar.gz
sudo mv cutube /usr/local/bin/
```

## Dependências

### Obrigatórias
- .NET 10 Runtime (incluído)

### Opcionais
- **yt-dlp**: `brew install yt-dlp` ou `pip3 install yt-dlp`
- **ffmpeg**: `brew install ffmpeg` ou `sudo apt install ffmpeg`

## Verificação

```bash
cutube --version
cutube --help
```

## Desinstalação

### Linux/macOS
```bash
sudo rm /usr/local/bin/cutube
rm -rf ~/.cutube
```

### Windows
Remova o executável e `%USERPROFILE%\.cutube`.
```

**Checklist:**
- [ ] Criar documentação
- [ ] Instalação rápida
- [ ] Instalação manual
- [ ] Dependências
- [ ] Solução de problemas

**Critérios de aceito:**
- ✅ Documentação completa

---

## Qualidade Gates

**ANTES de considerar esta fase completa:**

- [ ] **install.sh criado** - Script universal
- [ ] **Testado em Linux** - Funciona
- [ ] **Testado em macOS** - Funciona
- [ ] **release-cli.sh criado** - Script de release
- [ ] **Documentação criada** - installation.md
- [ ] **Release testado** - Tag criada

---

## Cronograma

| Tarefa | Estimativa | Dependencies |
|--------|-----------|--------------|
| 4.3.1 Script Install | 2h | Nenhuma |
| 4.3.2 Release Script | 30m | Nenhuma |
| 4.3.3 Documentação | 1h | Nenhuma |

**Total:** 3.5 horas

---

## Tecnologias

- **Bash** - Scripting
- **GitHub API** - Versão
- **GitHub Releases** - Distribuição

---

## Entregáveis

- [ ] `scripts/install.sh`
- [ ] `scripts/release-cli.sh`
- [ ] `docs/installation.md`
- [ ] Testado em múltiplas plataformas

---

**Fim do Plano - Fase 4.3**
