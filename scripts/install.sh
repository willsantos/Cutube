#!/usr/bin/env bash

set -euo pipefail

REPO="willsantos/Cutube"
BINARY_NAME="cutube"
DEFAULT_INSTALL_DIR="/usr/local/bin"
FALLBACK_INSTALL_DIR="$HOME/.local/bin"

GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'

log_info() {
  printf "%b\n" "${GREEN}[INFO]${NC} $1"
}

log_warn() {
  printf "%b\n" "${YELLOW}[WARN]${NC} $1"
}

log_error() {
  printf "%b\n" "${RED}[ERROR]${NC} $1" >&2
}

require_command() {
  if ! command -v "$1" >/dev/null 2>&1; then
    log_error "Required command not found: $1"
    exit 1
  fi
}

detect_platform() {
  local os arch

  case "$(uname -s)" in
    Linux*) os="linux" ;;
    Darwin*) os="macos" ;;
    *)
      log_error "Unsupported operating system for this script: $(uname -s)"
      log_error "Para Windows, use: iwr -useb https://willsantos.github.io/Cutube/install.ps1 | iex"
      exit 1
      ;;
  esac

  case "$(uname -m)" in
    x86_64|amd64) arch="amd64" ;;
    arm64|aarch64) arch="arm64" ;;
    *)
      log_error "Unsupported architecture: $(uname -m)"
      exit 1
      ;;
  esac

  printf "%s-%s" "$os" "$arch"
}

http_get() {
  local url="$1"
  if command -v curl >/dev/null 2>&1; then
    curl -fsSL "$url"
  elif command -v wget >/dev/null 2>&1; then
    wget -qO- "$url"
  else
    log_error "curl or wget is required for downloads"
    exit 1
  fi
}

download_file() {
  local url="$1"
  local output="$2"

  if command -v curl >/dev/null 2>&1; then
    curl -fsSL "$url" -o "$output"
  elif command -v wget >/dev/null 2>&1; then
    wget -q "$url" -O "$output"
  else
    log_error "curl or wget is required for downloads"
    exit 1
  fi
}

get_latest_version() {
  local api_url="https://api.github.com/repos/${REPO}/releases/latest"
  local version

  version="$(http_get "$api_url" | sed -n 's/.*"tag_name"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/p' | head -n 1)"
  if [ -z "$version" ]; then
    log_error "Could not detect latest release version from ${api_url}"
    exit 1
  fi

  printf "%s" "$version"
}

select_asset_name() {
  local platform="$1"
  printf "%s-%s.tar.gz" "$BINARY_NAME" "$platform"
}

extract_binary() {
  local archive_path="$1"
  local temp_dir="$2"

  require_command tar
  tar -xzf "$archive_path" -C "$temp_dir"

  if [ ! -f "$temp_dir/$BINARY_NAME" ]; then
    log_error "File '$BINARY_NAME' not found after extraction"
    exit 1
  fi

  chmod +x "$temp_dir/$BINARY_NAME"
}

resolve_install_dir() {
  if [ -w "$DEFAULT_INSTALL_DIR" ]; then
    printf "%s" "$DEFAULT_INSTALL_DIR"
    return
  fi

  if command -v sudo >/dev/null 2>&1 && sudo -n true >/dev/null 2>&1; then
    printf "%s" "$DEFAULT_INSTALL_DIR"
    return
  fi

  mkdir -p "$FALLBACK_INSTALL_DIR"
  printf "%s" "$FALLBACK_INSTALL_DIR"
}

install_binary() {
  local source_binary="$1"
  local install_dir="$2"
  local target="$install_dir/$BINARY_NAME"

  log_info "Installing to ${target}"

  if [ "$install_dir" = "$DEFAULT_INSTALL_DIR" ] && [ ! -w "$install_dir" ]; then
    if command -v sudo >/dev/null 2>&1; then
      sudo install -m 755 "$source_binary" "$target"
    else
      log_error "No write access to ${install_dir} and sudo is unavailable"
      exit 1
    fi
  else
    install -m 755 "$source_binary" "$target"
  fi
}

check_dependencies() {
  local platform_os="$1"
  local missing=()

  if ! command -v yt-dlp >/dev/null 2>&1 && ! command -v youtube-dl >/dev/null 2>&1; then
    missing+=("yt-dlp")
  fi

  if ! command -v ffmpeg >/dev/null 2>&1; then
    missing+=("ffmpeg")
  fi

  if [ "${#missing[@]}" -eq 0 ]; then
    log_info "Optional dependencies detected (yt-dlp/ffmpeg)."
    return
  fi

  log_warn "Missing dependencies: ${missing[*]}"
  if [ "$platform_os" = "macos" ]; then
    log_info "Install with Homebrew: brew install ffmpeg yt-dlp"
  else
    log_info "Install with apt: sudo apt install ffmpeg"
    log_info "Install yt-dlp: python3 -m pip install -U yt-dlp"
  fi
}

print_path_hint() {
  local install_dir="$1"
  case ":$PATH:" in
    *":${install_dir}:"*) ;;
    *)
      log_warn "${install_dir} is not in PATH for this session."
      log_info "Add to your shell profile: export PATH=\"${install_dir}:\$PATH\""
      ;;
  esac
}

main() {
  printf "\nCutube CLI Installer\n\n"

  local platform version asset_name asset_url temp_dir archive_path install_dir
  platform="$(detect_platform)"
  log_info "Detected platform: ${platform}"

  version="$(get_latest_version)"
  log_info "Latest version: ${version}"

  asset_name="$(select_asset_name "$platform")"
  asset_url="https://github.com/${REPO}/releases/download/${version}/${asset_name}"

  temp_dir="$(mktemp -d)"
  trap 'rm -rf "$temp_dir"' EXIT

  archive_path="${temp_dir}/${asset_name}"

  log_info "Downloading ${asset_name}"
  download_file "$asset_url" "$archive_path"

  extract_binary "$archive_path" "$temp_dir"

  install_dir="$(resolve_install_dir)"
  install_binary "${temp_dir}/${BINARY_NAME}" "$install_dir"

  print_path_hint "$install_dir"
  check_dependencies "${platform%%-*}"

  if command -v "$BINARY_NAME" >/dev/null 2>&1; then
    log_info "Installation completed. Installed version:"
    "$BINARY_NAME" --version || true
  else
    log_info "Installation completed at ${install_dir}/${BINARY_NAME}"
  fi

  printf "\nUse: cutube --help\n"
}

main "$@"
