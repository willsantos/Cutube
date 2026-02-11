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
- [ ] Tornar a digitação do tempo mais flexivel
- [ ] Adicionar opção de escolher o formato do vídeo
- [ ] Adicionar opção de escolher a qualidade do vídeo
- [ ] Adicionar opção de escolher o nome do arquivo
- [ ] Adicionar opção de escolher o diretório de destino
- [ ] Adicionar opção de escolher a legenda do vídeo
- [ ] Implementar barra de progresso do download
- [x] Implementar barra de progresso do corte
- [ ] Baixar videos de outras plataformas (e.g. Instagram)
- [ ] Baixar somente o áudio
- [ ] Criar instalador para Windows
 - [ ] Testar em MacOS e Linux


## Gerenciamento de Tarefas

Este projeto utiliza dois sistemas complementares para gerenciamento de trabalho:

- **TASKS (Beads/bd):** Gerencia tarefas de desenvolvimento e features planejadas
- **ISSUES (Linear):** Gerencia bugs reportados por usuários

### Para desenvolvedores

O projeto usa o **bd (Beads)** para rastreamento de tarefas de desenvolvimento:

```bash
# Ver tarefas disponíveis
bd list

# Ver detalhes de uma tarefa
bd show <id>

# Atualizar status da tarefa
bd update <id> --status in_progress

# Marcar como concluída
bd close <id>
```

As tarefas ficam no repositório e sincronizam automaticamente com o git.


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



## Como usar
1. Baixe o projeto
2. Abra o terminal na pasta do projeto
3. Execute o comando `dotnet run`
4. Siga as instruções do programa
5. O vídeo será salvo na mesma pasta
6. Enjoy!
7. (Opcional) Se quiser, pode adicionar o executável do programa no PATH do seu sistema operacional para poder executar o programa de qualquer lugar

## RabbitMQ Setup

O Cutube usa RabbitMQ para processamento assíncrono de downloads. Veja a [documentação completa](docs/setup-rabbitmq.md) para instruções detalhadas.

### Início Rápido

```bash
# Iniciar RabbitMQ
./scripts/docker-compose-up.sh

# Acessar Management UI
# http://localhost:15672 (User: cutube, Pass: cutube123)

# Executar Worker
cd src/Cutube.Worker
dotnet run
```
