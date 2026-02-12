# Instalacao do Cutube CLI

## Instalacao rapida

### Linux e macOS

```bash
curl -fsSL https://willsantos.github.io/Cutube/install.sh | bash
```

Fallback (Raw GitHub):

```bash
curl -fsSL https://raw.githubusercontent.com/willsantos/Cutube/main/scripts/install.sh | bash
```

### Windows (PowerShell)

```powershell
iwr -useb https://willsantos.github.io/Cutube/install.ps1 | iex
```

Fallback (Raw GitHub):

```powershell
iwr -useb https://raw.githubusercontent.com/willsantos/Cutube/main/scripts/install.ps1 | iex
```

## Instalacao manual

1. Acesse as releases: https://github.com/willsantos/Cutube/releases
2. Baixe o pacote da sua plataforma.
3. Extraia e mova o executavel para uma pasta no PATH.

Exemplo Linux:

```bash
tar -xzf cutube-linux-amd64.tar.gz
sudo install -m 755 cutube /usr/local/bin/cutube
```

## Dependencias opcionais

- `yt-dlp` (ou `youtube-dl`) para metadados e fluxos adicionais
- `ffmpeg` para processamento multimidia

Exemplos:

```bash
# macOS
brew install ffmpeg yt-dlp

# Ubuntu/Debian
sudo apt install ffmpeg
python3 -m pip install -U yt-dlp
```

```powershell
# Windows (winget)
winget install yt-dlp.yt-dlp Gyan.FFmpeg
```

## Verificacao

```bash
cutube --version
cutube --help
```

## Desinstalacao

### Linux e macOS

```bash
sudo rm -f /usr/local/bin/cutube
rm -f ~/.local/bin/cutube
```

### Windows

Remova `cutube.exe` da pasta de instalacao e, se quiser, limpe a entrada de PATH correspondente.
