# Cutube

![Tela inicial](/Assets/image.png)
## Description

Cutube é um simples downloader de vídeos escrito em C# que permite baixar vídeos em diferentes formatos e qualidades e cortar no tempo desejado.

Cutube is a simple video downloader written in C# that allows you to download videos in different formats and qualities and cut them at the desired time.

## Motivação
Facilitar o trabalho de edição de podcasts.
Aprofundar nos estudos de C# e .NET.
Explorar funcionalidades de processos com FFMpeg.

## Roadmap
- [x] Implementar opção de cortar o vídeo por tempo
- [x] Tornar a digitação do tempo mais flexivel
- [ ] Adicionar opção de escolher o formato do vídeo
- [ ] Adicionar opção de escolher a qualidade do vídeo
- [x] Adicionar opção de escolher o nome do arquivo
- [x] Adicionar opção de escolher o diretório de destino
- [ ] Adicionar opção de escolher a legenda do vídeo
- [ ] Implementar barra de progresso do download
- [x] Implementar barra de progresso do corte
- [ ] Baixar videos de outras plataformas (e.g. Instagram)
- [x] Baixar somente o áudio
- [ ] Criar instalador para Windows
 - [ ] Testar em MacOS e Linux


## Gerenciamento de Tarefas

Este repositório **não** usa beads (bd) nem taskmaster local. O rastreamento
é feito com:

- **ISSUES (Linear):** bugs reportados por usuários e trabalho solicitado
  explicitamente (workspace "Oroborus", projeto "Cutube")
- **Specs e planos versionados:** planejamento de features fica no repositório


# Tecnologias
- Dotnet 10.0 LTS (suporte até Nov 2028)
- yt-dlp (YouTube downloader com PO token support) - **incluído**
- YoutubeDLSharp 1.2.0 (.NET wrapper)
- FFmpeg.AutoGen 8.0.0


# Requisitos para rodar
- .NET 10.0 runtime ou superior
- FFmpeg 6+ (opcional para versões bundle)

**Nota:** O yt-dlp é **incluso automaticamente** com o Cutube e se atualiza sozinho! 🎉


## Desenvolvimento

### yt-dlp Bundling
O Cutube vem com o yt-dlp bundleado e implementa auto-update inteligente:

- ✅ **Zero configuração**: yt-dlp já vem no pacote
- ✅ **Auto-update**: Verifica atualizações ao iniciar
- ✅ **Fallback**: Usa yt-dlp do sistema se bundle falhar
- ✅ **Gerenciado**: Atualizações são baixadas para `~/.local/share/Cutube/`

### Desenvolvedores

Este projeto usa ASDF para gerenciar versões do .NET.

```bash
# Instalar ASDF
git clone https://github.com/asdf-vm/asdf.git ~/.asdf

# Instalar .NET 10.0
asdf plugin add dotnet-core
asdf install
```

O arquivo .tool-versions configura automaticamente o .NET 10.0 ao entrar no projeto.

Se você quer usar sua própria versão do yt-dlp:

```bash
# Instalar yt-dlp globalmente (opcional)
curl -L https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp -o /usr/local/bin/yt-dlp
chmod +x /usr/local/bin/yt-dlp

# Ou via pip
pip install yt-dlp
```

O Cutube detectará automaticamente e usará sua versão se for mais recente que o bundle.



## Estrutura do projeto

```
src/
  Cutube.Cli/            # aplicação CLI (entry point)
  Cutube.Domain/         # models, interfaces e serviços de domínio
  Cutube.Core/           # primitivas compartilhadas
  Cutube.Application/    # casos de uso / orquestração
  Cutube.Infrastructure/ # yt-dlp, FFmpeg, filesystem
tests/
  Cutube.Cli.Tests/      # testes unitários/integração da CLI
  Cutube.Domain.Tests/   # testes de domínio
```

Build e testes: `dotnet build` e `dotnet test` na raiz (solução `cutube.sln`).

## Como usar
1. Baixe o projeto
2. Abra o terminal na pasta do projeto
3. Execute `dotnet run --project src/Cutube.Cli`
4. Siga as instruções do programa
5. Enjoy!
6. (Opcional) Instale o binário no PATH para executar de qualquer lugar

### Comandos

```bash
cutube                                  # modo interativo
cutube download <url>                   # download direto
cutube download <url> --audio           # somente áudio (MP3)
cutube download <url> -s 00:01:00 -e 00:05:30   # corte por tempo
cutube download <url> -o ~/Videos       # diretório de destino
cutube --resume                         # retomar downloads interrompidos
cutube config show                      # ver configuração
cutube config reset                     # restaurar padrões
```

## Instalacao do CLI

Para instalacao automatica em Linux/macOS/Windows, use os scripts em `scripts/install.sh` e `scripts/install.ps1`.

- Pagina de instalacao: `https://willsantos.github.io/Cutube/`
- Guia completo: `docs/installation.md`

## Cutube é uma CLI

O Cutube é **apenas uma CLI standalone**: baixa, corta e converte vídeos
localmente, sem depender de servidor, fila ou Docker.
