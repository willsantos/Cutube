# PRD - Cutube

## 1. Visão Geral

**Nome do Produto:** Cutube  
**Tipo:** CLI Application (Command Line Interface)  
**Linguagem:** C# (.NET 9.0)  
**Propósito:** Downloader e editor de vídeos do YouTube com foco em extração de trechos específicos

**Descrição:** Cutube é uma ferramenta de linha de comando que permite baixar vídeos do YouTube e cortá-los em intervalos de tempo específicos. Foi desenvolvida para facilitar o trabalho de edição de podcasts, permitindo extrair clipes rapidamente sem necessidade de editores de vídeo complexos.

---

## 2. Problema e Solução

### Problema
Editores de podcasts e criadores de conteúdo precisam frequentemente extrair trechos específicos de vídeos longos do YouTube. O processo tradicional envolve:
1. Baixar o vídeo completo
2. Abrir em um editor de vídeo
3. Localizar o trecho desejado
4. Exportar apenas o corte

### Solução
O Cutube automatiza esse processo em uma única operação de CLI:
- Download direto do YouTube
- Cortes precisos por tempo
- Processamento automático com FFmpeg
- Sem necessidade de interface gráfica

---

## 3. Público-Alvo

### Primário
- **Editores de Podcast:** Profissionais que recortam trechos de podcasts gravados em vídeo
- **Criadores de Conteúdo:** Youtubers que precisam extrair clips para redes sociais

### Secundário
- Desenvolvedores estudando C# e processamento de vídeo
- Usuários técnicos que preferem ferramentas CLI over GUI

---

## 4. Funcionalidades

### Implementadas ✅
| Funcionalidade | Status | Descrição |
|----------------|--------|-----------|
| Download de vídeos | ✅ | Baixa vídeos do YouTube usando YoutubeExplode |
| Corte por tempo | ✅ | Corta vídeo em intervalo específico (hh:mm:ss) |
| Processamento áudio+vídeo | ✅ | Combina stream de vídeo e áudio com FFmpeg |
| Barra de progresso (corte) | ✅ | Feedback visual durante processamento FFmpeg |
| Limpeza de temp | ✅ | Remove arquivos temporários após conclusão |
| Sanitização de título | ✅ | Remove acentos e caracteres especiais do nome do arquivo |
| Detecção automática FFmpeg | ✅ | Busca FFmpeg em PATH ou AppData |

### Planejadas 📋
| Prioridade | Funcionalidade | Impacto |
|------------|----------------|---------|
| Alta | Formato de tempo flexível | Aceitar "mm:ss", "30s", etc. |
| Alta | Escolha de qualidade | 1080p, 720p, 480p |
| Alta | Download de áudio apenas | Extração de MP3 |
| Média | Escolha de formato | MP4, MKV, AVI |
| Média | Diretório de destino personalizado | `-o /path/to/output` |
| Média | Download de legendas | SRT, VTT |
| Média | Barra de progresso de download | Melhor UX |
| Baixa | Suporte a outras plataformas | Instagram, TikTok |
| Baixa | Instalador Windows | MSI/EXE standalone |
| Baixa | Suporte multi-plataforma | Linux, macOS testing |

---

## 5. Arquitetura Técnica

### Estrutura de Arquivos
```
cutube/
├── Program.cs         # Entry point & orchestrator
├── Menu.cs            # CLI input collection
├── FfmpegHelper.cs    # FFmpeg discovery & execution
├── TimeHelper.cs      # Time parsing utilities
├── TitleHelper.cs     # Filename sanitization
├── ProgressBar.cs     # Progress display
└── cutube.csproj      # Project configuration
```

### Fluxo de Dados
```
1. Menu.Show() → Coleta URL, start time, end time
2. YoutubeClient → Baixa metadata e streams
3. DownloadAsync → Salva video + audio em arquivos temp
4. FFmpeg → Processa corte e merge
5. File.Delete → Limpeza de temp
6. Output → Arquivo final MP4
```

### Dependências Externas
| Biblioteca | Versão | Uso |
|------------|--------|-----|
| YoutubeExplode | 6.5.4 | API YouTube (download, metadata) |
| YoutubeExplode.Converter | 6.5.4 | Extensão de conversão |
| FFmpeg.AutoGen | 7.0.0 | Bindings do FFmpeg |
| FFmpeg (binário) | 6+ | Processamento de vídeo (externo) |

---

## 6. Requisitos Funcionais Detalhados

### RF-01: Download de Vídeo
- **Entrada:** URL válida do YouTube
- **Processamento:** Identifica streams de maior qualidade
- **Saída:** Arquivo temporário com vídeo

### RF-02: Parse de Tempo
- **Formato atual:** `hh:mm:ss` (estritamente)
- **Validação:** Deve ser tempo válido e end > start
- **TODO:** Aceitar formatos flexíveis (`mm:ss`, `300s`, `5m`)

### RF-03: Corte com FFmpeg
- **Código:** `-ss {start} -t {duration} -c:v libx264 -c:a aac`
- **Entrada:** 1 ou 2 arquivos (video + audio)
- **Saída:** MP4 final com trecho cortado

### RF-04: Descoberta de FFmpeg
- **Ordem de busca:** AppData → PATH do sistema
- **Executáveis:** `ffmpeg` ou `ffmpeg.exe`

---

## 7. Requisitos Não-Funcionais

### RNF-01: Performance
- Download progressivo (stream-based)
- Processamento de vídeo otimizado com hardware (quando disponível)

### RNF-02: Usabilidade
- Feedback visual com barra de progresso
- Mensagens de erro claras em português

### RNF-03: Compatibilidade
- .NET 9.0 runtime
- Windows (testado), macOS/Linux (não testado)

### RNF-04: Confiabilidade
- Tratamento de exceções
- Limpeza de arquivos temporários
- Validação de inputs

---

## 8. Limitações Atuais

1. **Apenas YouTube:** Integração específica com YoutubeExplode
2. **Formato de tempo rígido:** Exige `hh:mm:ss` completo
3. **Sem qualidade selecionável:** Sempre usa máxima disponível
4. **Output no diretório atual:** Sem opção de customização
5. **Sem preview:** Não é possível pré-visualizar antes de baixar
6. **Windows-centric:** Detecta `.exe` em alguns caminhos

---

## 9. Oportunidades de Melhoria

### Curto Prazo
1. **Argumentos CLI:** Aceitar parâmetros via linha de comando
   ```bash
   cutube --url "..." --start "00:05:00" --end "00:10:00"
   ```

2. **Arquivo de configuração:** `~/.cutube/config.json` para defaults

3. **Validação robusta:** Checar se FFmpeg existe antes de iniciar

### Médio Prazo
1. **Plugin system:** Suportar outras plataformas via adapters
2. **Queue processing:** Processar múltiplos vídeos em batch
3. **Preset de qualidade:** Web (720p), Mobile (480p), etc.

### Longo Prazo
1. **GUI:** Interface gráfica com WPF ou Avalonia
2. **Serviço:** API REST para integração com outras ferramentas
3. **Cloud:** Executar em container para processamento distribuído

---

## 10. Métricas de Sucesso

- **Adoção:** Número de clones/estrelas no GitHub
- **Stabilidade:** Taxa de erros em produção
- **Performance:** Tempo médio de processamento por minuto de vídeo
- **Satisfação:** Feedback de usuários via issues

---

## 11. Riscos e Mitigações

| Risco | Probabilidade | Impacto | Mitigação |
|-------|---------------|---------|-----------|
| YoutubeExplode quebrar | Média | Alto | Manter atualizado, testes automatizados |
| FFmpeg não instalado | Alta | Médio | Detectar early, mensagem clara de instalação |
| Mudança de API do YouTube | Média | Alto | Monitorar issues do YoutubeExplode |
| Crescimento desorganizado | Média | Médio | Refatoração em módulos |

---

## 12. Próximos Passos Recomendados

1. **Implementar argumentos CLI** (maior valor UX)
2. **Adicionar presets de qualidade** (requisito comum)
3. **Suporte a download de áudio apenas** (caso de uso podcasts)
4. **Testes automatizados** (garantir qualidade)
5. **Documentação de API** (para extensibilidade)

---

**Status do PRD:** ✅ Completo baseado em análise de código  
**Data:** 30/01/2026  
**Versão do Projerto:** Baseado no roadmap atual
