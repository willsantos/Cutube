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

**Nota:** se nao estiverem instalados, o Cutube baixa sozinho no primeiro uso:
`yt-dlp` em todas as plataformas e, no Windows, tambem o `ffmpeg` (build
estatico oficial de BtbN/FFmpeg-Builds, ~120 MB, salvo em
`%LOCALAPPDATA%\Cutube`). Em Linux/macOS, prefira o gerenciador de pacotes.

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

## Atualizacao

### Automatica (padrao)

A CLI verifica no maximo 1x por dia se existe versao nova
(`releases/latest` no GitHub, timeout de 3s). Se houver e a sessao for
interativa, ela pergunta:

```
Nova versao disponivel: v1.3.0 (atual: 1.2.1)
Deseja atualizar agora? [S/n]:
```

- Enter ou `s`: baixa o binario da sua plataforma, substitui o executavel e
  **reinicia o comando pedido na versao nova** (exit code propagado).
- `n` ou qualquer outra resposta: o comando executa normalmente na versao atual.
- Sem rede, timeout ou erro de permissao: a CLI apenas avisa e continua na
  versao atual (com o comando manual abaixo como sugestao).

Em scripts/CI (stdin redirecionado) a verificacao e pulada silenciosamente;
o comando explicito `cutube update` nesse contexto atualiza sem perguntar.

### Forcada

```bash
cutube update        # ignora o cache diario e verifica na hora
```

### Desativando a verificacao automatica

No arquivo de configuracao (`cutube config show` mostra o caminho):

```json
{ "checkForUpdates": false }
```

Configs antigas sem esse campo ficam com o padrao `true`.

### Manual

Re-execute o instalador da sua plataforma (sempre instala a latest release
e sobrescreve o binario):

```bash
# Linux e macOS
curl -fsSL https://willsantos.github.io/Cutube/install.sh | bash
```

```powershell
# Windows (PowerShell)
iwr -useb https://willsantos.github.io/Cutube/install.ps1 | iex
```

Ou baixe o asset da sua plataforma em
https://github.com/willsantos/Cutube/releases e substitua o binario.

Nota: no Windows, ao atualizar em execucao o binario anterior fica como
`cutube.exe.old` ao lado do novo e e removido na proxima inicializacao.

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
