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


# Tecnologias
- Dotnet 10.0 LTS (suporte até Nov 2028)
- YoutubeExplode 6.5.6
- FFmpeg.AutoGen 8.0.0


# Requisitos para rodar
- .NET 10.0 runtime ou superior
- FFmpeg 6+ (opcional para versões bundle)

## Desenvolvimento

Este projeto usa ASDF para gerenciar versões do .NET.

```bash
# Instalar ASDF
git clone https://github.com/asdf-vm/asdf.git ~/.asdf

# Instalar .NET 10.0
asdf plugin add dotnet-core
asdf install
```

O arquivo .tool-versions configura automaticamente o .NET 10.0 ao entrar no projeto.



## Como usar
1. Baixe o projeto
2. Abra o terminal na pasta do projeto
3. Execute o comando `dotnet run`
4. Siga as instruções do programa
5. O vídeo será salvo na mesma pasta
6. Enjoy!
7. (Opcional) Se quiser, pode adicionar o executável do programa no PATH do seu sistema operacional para poder executar o programa de qualquer lugar
