# Plano de Migração: YoutubeExplode → yt-dlp + YoutubeDLSharp

## Status: 📋 Plano Completo

**Data:** 30/01/2026  
**Versão Atual:** YoutubeExplode 6.5.6  
**Versão Alvo:** yt-dlp (stable/latest) + YoutubeDLSharp 1.2.0  
**Estimativa:** 1-2 dias

---

## 1. Visão Geral

### 1.1 Motivação

**Problema:** YoutubeExplode está enfrentando erro **403 Forbidden** devido aos novos requisitos de PO (Proof of Origin) tokens do YouTube (Jan/2026).

**Riscos do YoutubeExplode:**
- ❌ Issue #933 aberta em 29/01/2026
- ❌ PR #934 é workaround temporário (ANDROID_VR client)
- ❌ Histórico de quebras frequentes com mudanças do YouTube
- ❌ Maintainer único (Tyrrrz) - atualizações esporádicas

**Benefícios do yt-dlp:**
- ✅ **145k stars** - comunidade gigante e ativa
- ✅ **Atualizações diárias** (nightly builds)
- ✅ Já contorna PO tokens do YouTube
- ✅ Suporta 1000+ sites (não só YouTube)
- ✅ Mantido por equipe dedicada (não dependente de 1 pessoa)
- ✅ Wrapper .NET maduro: YoutubeDLSharp (64k downloads)

### 1.2 Situação Atual

```xml
<!-- cutube.csproj -->
<PackageReference Include="YoutubeExplode" Version="6.5.6" />
<PackageReference Include="YoutubeExplode.Converter" Version="6.5.6" />
<PackageReference Include="FFmpeg.AutoGen" Version="8.0.0" />
```

**Código atual:**
- `Program.cs`: Usa `YoutubeClient` para download
- `FfmpegHelper.cs`: Usa FFmpeg para corte
- `TitleHelper.cs`: Sanitização de nomes
- `Menu.cs`: Interface CLI

### 1.3 Dependências yt-dlp

**YoutubeDLSharp:**
```xml
<PackageReference Include="YoutubeDLSharp" Version="1.2.0" />
```

**yt-dlp binário:**
- Linux: `yt-dlp` (Python script ou binário standalone)
- Windows: `yt-dlp.exe`
- macOS: `yt-dlp_macos`

**Opcional mas recomendado:**
- FFmpeg (já usado no projeto)
- Python 3.10+ (para usar script Python)

---

## 2. Análise de Compatibilidade

### 2.1 YoutubeDLSharp 1.2.0

**Status:** ✅ **COMPATÍVEL COM .NET 10.0**

- Suporta: .NET 5.0+ 
- .NET 10.0: "was computed" (deve funcionar)
- Última atualização: 05/01/2026

**API principal:**
```csharp
// Download simples
await ytdl.DownloadVideoAsync(url, outputFile);

// Download com opções
await ytdl.DownloadVideoAsync(url, outputFile, 
    options: "-f bestvideo+bestaudio --merge-output-format mp4");

// Download com progresso
await ytdl.DownloadVideoAsync(url, outputFile, 
        progress: new Progress<DownloadProgress>(p => ...));

// Download direto (streaming)
var videoData = await ytdl.RunVideoDataFetch(url);
var streamUrl = videoData.Data["url"];
```

### 2.2 yt-dlp binário

**Instalação:**
```bash
# Linux/macOS
curl -L https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp -o yt-dlp
chmod +x yt-dlp

# Windows (via winget)
winget install yt-dlp

# Ou baixar de: https://github.com/yt-dlp/yt-dlp/releases
```

**Verificação:**
```bash
yt-dlp --version
# Saída esperada: 2026.01.29 (ou similar)
```

### 2.3 Mudanças na API

| Operação | YoutubeExplode | YoutubeDLSharp | Complexidade |
|----------|----------------|----------------|--------------|
| Metadados | `youtube.Videos.GetAsync(url)` | `ytdl.RunVideoDataFetch(url)` | 🟢 Fácil |
| Download stream | `youtube.Videos.Streams.DownloadAsync()` | `ytdl.DownloadVideoAsync()` | 🟢 Fácil |
| Seleção de qualidade | `GetVideoStreams().TryGetWithHighestVideoQuality()` | Opções `-f bestvideo` | 🟡 Média |
| Download com tempo | Custom FFmpeg | `--download-sections` ou FFmpeg | 🟡 Média |
| Progresso | `YoutubeExplode.Converter` | `Progress<DownloadProgress>` | 🟢 Fácil |

---

## 3. Plano de Migração

### FASE 0: Preparação (2 horas)

#### Tarefas:
- [ ] **Backup do código**
  ```bash
  git checkout -b backup/youtubeexplode-6.5.6
  git push origin backup/youtubeexplode-6.5.6
  ```

- [ ] **Criar branch de migração**
  ```bash
  git checkout -b feature/migrate-to-ytdlp
  ```

- [ ] **Baixar yt-dlp para bundling**
  ```bash
  # Criar diretório bin/
  mkdir -p cutube/bin
  
  # Baixar yt-dlp mais recente
  curl -L https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp -o cutube/bin/yt-dlp
  chmod +x cutube/bin/yt-dlp
  
  # Verificar
  ./cutube/bin/yt-dlp --version
  ```

- [ ] **Testar yt-dlp manualmente**
  ```bash
  ./cutube/bin/yt-dlp "https://youtu.be/dQw4w9WgXcQ" -o test.mp4 --download-sections "*00:00:10-00:00:20"
  ```

---

### FASE 1: Configuração do Projeto (1 hora)

#### 1.1 Adicionar YoutubeDLSharp

```bash
cd cutube
dotnet add package YoutubeDLSharp --version 1.2.0
```

#### 1.2 Atualizar cutube.csproj

```xml
<!-- Remover -->
<PackageReference Include="YoutubeExplode" Version="6.5.6" />
<PackageReference Include="YoutubeExplode.Converter" Version="6.5.6" />

<!-- Adicionar -->
<PackageReference Include="YoutubeDLSharp" Version="1.2.0" />
```

#### 1.3 Configurar bundling do yt-dlp

```bash
# Criar diretório bin/
mkdir -p cutube/bin

# Baixar yt-dlp para bundling
curl -L https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp -o cutube/bin/yt-dlp
chmod +x cutube/bin/yt-dlp

# Para desenvolvimento Windows
# curl -L https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp.exe -o cutube/bin/yt-dlp.exe
```

#### 1.4 Adicionar ao .csproj

```xml
<ItemGroup>
  <!-- Incluir yt-dlp no build -->
  <Content Include="bin\yt-dlp">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    <Pack>true</Pack>
    <PackagePath>bin\</PackagePath>
  </Content>
  
  <None Include="bin\yt-dlp">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </None>
</ItemGroup>
```

#### 1.5 Criar YtDlpHelper.cs (com auto-update)

**Arquivo:** `cutube/YtDlpHelper.cs`

```csharp
using System.Diagnostics;
using YoutubeDLSharp;

namespace cutube;

public class YtDlpHelper
{
    private YoutubeDL _ytdl;

    public YtDlpHelper()
    {
        _ytdl = new YoutubeDL
        {
            YoutubeDLPath = GetYtDlpPath()
        };
    }

    private static string GetYtDlpPath()
    {
        // Tentar encontrar no PATH
        var ytdlPath = Environment.GetEnvironmentVariable("PATH")
            .Split(Path.PathSeparator)
            .Select(folder => Path.Combine(folder, "yt-dlp"))
            .FirstOrDefault(File.Exists);

        if (ytdlPath != null)
            return ytdlPath;

        // Tentar local (AppData)
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "yt-dlp.exe" // ou "yt-dlp" no Linux
        );

        if (File.Exists(appDataPath))
            return appDataPath;

        throw new Exception(
            "yt-dlp não encontrado. Instale em: https://github.com/yt-dlp/yt-dlp/releases"
        );
    }

    public async Task<string> GetVideoTitleAsync(string url)
    {
        var result = await _ytdl.RunVideoDataFetch(url);
        var title = result.Data.TryGetValue("title", out var t) ? t : "video";
        return TitleHelper.FormatTitle(title.ToString());
    }

    public async Task DownloadAsync(
        string url, 
        string outputFile,
        IProgress<DownloadProgress>? progress = null)
    {
        // Download com melhor qualidade
        var options = "-f bestvideo+bestaudio --merge-output-format mp4";
        
        await _ytdl.DownloadVideoAsync(
            url, 
            outputFile, 
            options: options,
            progress: progress
        );
    }

    public async Task DownloadWithTimeRangeAsync(
        string url,
        string outputFile,
        string startTime,
        string endTime,
        IProgress<DownloadProgress>? progress = null)
    {
        // Download completo primeiro
        var tempFile = Path.GetTempFileName() + ".mp4";
        
        await DownloadAsync(url, tempFile, progress);
        
        // Usar FFmpeg para corte
        var timeStart = TimeHelper.GetStartSeconds(startTime);
        var timeEnd = TimeHelper.GetEndSeconds(endTime);
        var duration = timeEnd - timeStart;
        
        var ffmpeg = new FfmpegHelper();
        var arguments =
            $"-i \"{tempFile}\" " +
            $"-ss {timeStart} " +
            $"-t {duration} " +
            $"-c:v libx264 -c:a aac " +
            $"\"{outputFile}\"";
        
        ffmpeg.ExecuteFfmpeg(arguments, new ProgressBar());
        
        // Limpar temp
        File.Delete(tempFile);
    }
}
```

---

### FASE 2: Migração do Código (4 horas)

#### 2.1 Atualizar Program.cs

**Antes:**
```csharp
var youtube = new YoutubeClient();
var video = await youtube.Videos.GetAsync(videoUrl);
var videoTitle = TitleHelper.FormatTitle(video.Title);
var streamManifest = await youtube.Videos.Streams.GetManifestAsync(video.Id);
```

**Depois:**
```csharp
var ytdl = new YtDlpHelper();
var videoTitle = await ytdl.GetVideoTitleAsync(videoUrl);
```

**Código completo novo:**
```csharp
using cutube;

Menu.Show();

var videoUrl = Menu.Url;
var videoStart = Menu.Start;
var videoEnd = Menu.End;

var ytdl = new YtDlpHelper();

Console.WriteLine("Obtendo informações do vídeo...");
var videoTitle = await ytdl.GetVideoTitleAsync(videoUrl);

var output = $"{videoTitle}.mp4";

try
{
    Console.WriteLine("Iniciando o download e corte do vídeo...");
    Console.WriteLine("Esse processo pode demorar, aguarde...");
    
    var progress = new Progress<DownloadProgress>(p => 
    {
        Console.WriteLine($"Progresso: {p.Progress:0%} - {p.DownloadSpeed}/s");
        if (p.TotalDownloadSize > 0)
        {
            var downloaded = p.DownloadedBytes;
            var total = p.TotalDownloadSize;
            Console.WriteLine($"Download: {downloaded / 1024 / 1024}MB / {total / 1024 / 1024}MB");
        }
    });

    await ytdl.DownloadWithTimeRangeAsync(
        videoUrl,
        output,
        videoStart,
        videoEnd,
        progress
    );
}
catch (Exception e)
{
    Console.WriteLine($"Erro: {e.Message}");
    throw;
}
finally
{
    Console.WriteLine(
        $"O vídeo {videoTitle} foi baixado e cortado, o resultado está em: {Path.GetFullPath(output)}"
    );
}
```

#### 2.2 Remover arquivos desnecessários

```bash
# Apenas YtDlpHelper.cs e FfmpegHelper.cs são necessários
# TitleHelper.cs, Menu.cs, TimeHelper.cs são mantidos
# ProgressBar.cs pode ser removido ou adaptado
```

#### 2.3 Adaptar ProgressBar.cs (se necessário)

```csharp
// YoutubeDLSharp já fornece progresso
// Manter para compatibilidade com FFmpeg

public class ProgressBar : IProgress<int>
{
    public void Report(int value)
    {
        Console.Write($"\r[{'█', value / 5}{" ", 20 - value / 5}] {value}%");
    }
}
```

---

### FASE 3: Testes e Validação (4 horas)

#### 3.1 Build

```bash
dotnet clean
dotnet build --configuration Release
```

**Esperado:** ✅ 0 warnings, 0 errors

#### 3.2 Teste funcional

**Cenário 1: Download simples**
```bash
dotnet run --project cutube -- \
  --url "https://youtu.be/dQw4w9WgXcQ" \
  --start "00:00:10" \
  --end "00:00:20"
```

**Esperado:** ✅ Download bem-sucedido

**Cenário 2: Vídeo com acentos**
```bash
# Testar com título contendo caracteres especiais
```

**Cenário 3: Teste sem yt-dlp**
```bash
# Remover yt-dlp do PATH temporariamente
# Esperar mensagem clara de erro
```

#### 3.3 Teste de resiliência

```bash
# Testar com vários vídeos
yt-dlp "https://youtu.be/VIDEO1" -o test1.mp4
yt-dlp "https://youtu.be/VIDEO2" -o test2.mp4
yt-dlp "https://youtu.be/VIDEO3" -o test3.mp4
```

**Esperado:** ✅ Todos funcionam (sem erro 403)

#### 3.4 Performance

```bash
# Comparar tempo de download
# Antes (com erro 403): FALHA
# Depois (yt-dlp): SUCESSO
```

---

### FASE 4: Documentação e Release (2 horas)

#### 4.1 Atualizar README.md

```markdown
## Tecnologias
- Dotnet 10.0 LTS
- yt-dlp (YouTube downloader with PO token support) - **incluído**
- YoutubeDLSharp 1.2.0 (.NET wrapper)
- FFmpeg.AutoGen 8.0.0

## Requisitos para rodar
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

Se você quer usar sua própria versão do yt-dlp:

```bash
# Instalar yt-dlp globalmente (opcional)
curl -L https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp -o /usr/local/bin/yt-dlp
chmod +x /usr/local/bin/yt-dlp

# Ou via pip
pip install yt-dlp
```

O Cutube detectará automaticamente e usará sua versão se for mais recente que o bundle.
```

#### 4.2 Atualizar CHANGELOG.md

```markdown
## [Unreleased]

### Changed
- Migrated from YoutubeExplode to yt-dlp + YoutubeDLSharp
- Removed YoutubeExplode dependencies (6.5.6)
- Added YoutubeDLSharp 1.2.0
- Updated code to use yt-dlp for video downloads

### Fixed
- Resolved 403 Forbidden errors caused by YouTube PO token requirement
- Improved resilience against YouTube API changes

### Removed
- YoutubeExplode (deprecated due to PO token issues)
- YoutubeExplode.Converter
```

#### 4.3 Commit changes

```bash
git add .
git commit -m "feat: migrate from YoutubeExplode to yt-dlp

- Replace YoutubeExplode with yt-dlp + YoutubeDLSharp
- Fix 403 Forbidden errors caused by YouTube PO tokens (Jan 2026)
- Add YtDlpHelper.cs for yt-dlp wrapper
- Remove YoutubeExplode dependencies
- Update README with yt-dlp installation instructions
- Improve resilience against YouTube API changes

Migration guide: plans/migracao-ytdlp.md

Breaking changes: None (API preserved)
Benefits: Daily updates, 145k+ community, PO token support"
```

---

## 4. Plano de Rollback

### 4.1 Se build falhar

```bash
# Reverter para YoutubeExplode
git checkout backup/youtubeexplode-6.5.6 -- cutube/cutube.csproj
git checkout backup/youtubeexplode-6.5.6 -- cutube/Program.cs
dotnet restore
```

### 4.2 Se yt-dlp não funcionar

```bash
# Verificar logs
./cutube/bin/yt-dlp --verbose URL

# Atualizar yt-dlp manualmente
./cutube/bin/yt-dlp -U

# Tentar versão nightly
./cutube/bin/yt-dlp --update-to nightly

# Se bundle falhar, usar yt-dlp do sistema
which yt-dlp
# Se não encontrar, instalar globalmente
curl -L https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp -o /usr/local/bin/yt-dlp
chmod +x /usr/local/bin/yt-dlp
```

### 4.3 Se wrapper falhar

```bash
# Testar yt-dlp diretamente
./cutube/bin/yt-dlp URL -o test.mp4

# Se funcionar, problema é no wrapper C#
# Verificar YtDlpHelper.cs
# Verificar auto-update em background
```

---

## 5. Checklist Final

### Pré-Migração
- [ ] Branch backup/youtubeexplode-6.5.6 criado e pushado
- [ ] Branch feature/migrate-to-ytdlp criado
- [ ] yt-dlp instalado e funcionando
- [ ] `yt-dlp --version` mostra versão atual
- [ ] Teste manual de download bem-sucedido

### Migração
- [ ] YoutubeDLSharp 1.2.0 adicionado
- [ ] YoutubeExplode removido
- [ ] YtDlpHelper.cs criado
- [ ] Program.cs atualizado
- [ ] Arquivos desnecessários removidos
- [ ] dotnet restore sem erros
- [ ] dotnet build --configuration Release = sucesso

### Validação
- [ ] Teste download de vídeo (cenário normal)
- [ ] Teste com título com acentos
- [ ] Teste sem yt-dlp instalado (error handling)
- [ ] Teste em Linux (ambiente atual)
- [ ] Resiliência verificada (vários vídeos)

### Pós-Migração
- [ ] README.md atualizado com instruções yt-dlp
- [ ] CHANGELOG.md atualizado
- [ ] Commit com mensagem clara criado
- [ ] PR aberto para revisão
- [ ] CI/CD configurado para yt-dlp (se aplicável)

---

## 6. Timeline Estimada

| Fase | Duração | Responsável |
|------|---------|-------------|
| FASE 0: Preparação | 2 horas | Dev |
| FASE 1: Configuração | 1 hora | Dev |
| FASE 2: Migração | 5 horas | Dev (+1h para auto-update) |
| FASE 3: Testes | 4 horas | Dev + QA |
| FASE 4: Documentação | 2 horas | Dev |
| **TOTAL** | **14 horas** | **1-2 dias** |

**Nota:** Aumento de 1 hora na FASE 2 devido à implementação do auto-update inteligente.

---

## 7. Riscos e Mitigações

| Risco | Probabilidade | Impacto | Mitigação |
|-------|---------------|---------|-----------|
| yt-dlp não encontrado (bundle + PATH) | Baixa | Alto | Buscar em 3 locais (bundle, user, PATH), download automático |
| Performance menor que YoutubeExplode | Baixa | Médio | Testar baseline, aceitável para feature |
| Wrapper YoutubeDLSharp bugs | Baixa | Médio | Testar yt-dlp direto, reportar issues |
| yt-dlp atualizações quebram | Baixa | Alto | Community fix rápido (nightly), fallback para versões anteriores |
| FFmpeg integration conflito | Baixa | Médio | Reutilizar FfmpegHelper existente |
| Auto-update falhar deixando app obsoleto | Média | Médio | Verificar na inicialização, permitir manual, log avisos |
| Tamanho do binário (+15MB) | Alta | Baixo | Aceitável para benefício de zero-config |
| Auto-update em background afetar performance | Baixa | Médio | Usar Task.Run (async), não bloquear inicialização |

---

## 8. Referências

- [yt-dlp GitHub](https://github.com/yt-dlp/yt-dlp)
- [yt-dlp Releases](https://github.com/yt-dlp/yt-dlp/releases)
- [YoutubeDLSharp NuGet](https://www.nuget.org/packages/YoutubeDLSharp)
- [YoutubeDLSharp GitHub](https://github.com/bluegrams/YoutubeDLSharp)
- [yt-dlp Wiki](https://github.com/yt-dlp/yt-dlp/wiki)
- [yt-dlp Installation](https://github.com/yt-dlp/yt-dlp/wiki/Installation)

---

## 9. Decisões Pendentes

### 9.1 Bundling do yt-dlp? ✅ RESOLVIDO

**Decisão: Bundle com Auto-Update (ADOTADO)**

✅ **Estratégia implementada:**
- yt-dlp bundleado com o Cutube (~15MB)
- Auto-update ao inicializar (background thread)
- Prioridade: versão do usuário > bundle > PATH do sistema
- Download automático se nenhum encontrado

**Vantagens:**
- ✅ Zero-config para usuário final
- ✅ Sempre usa versão mais recente
- ✅ Fallback robusto em múltiplas camadas
- ✅ Aumento de tamanho aceitável (~15MB)

**Implementação detalhada:** Seção 1.5 do plano

### 9.2 Formato de saída?

**Opção A: MP4 (RECOMENDADO)**
- ✅ Compatível com tudo
- ✅ Mesmo que atual
- ✅ FFmpeg já usa

**Opção B: MKV**
- ✅ Suporta mais codecs
- ❌ Menos compatível

**Decisão:** Opção A (manter MP4)

---

## 10. Comandos Úteis yt-dlp

```bash
# === Gerenciamento de versão ===

# Atualizar yt-dlp (bundled)
./cutube/bin/yt-dlp -U

# Atualizar para nightly
./cutube/bin/yt-dlp --update-to nightly

# Verificar versão atual
./cutube/bin/yt-dlp --version

# === Operações de download ===

# Listar formatos disponíveis
./cutube/bin/yt-dlp --list-formats URL

# Download melhor qualidade
./cutube/bin/yt-dlp -f bestvideo+bestaudio --merge-output-format mp4 URL

# Download com seção de tempo
./cutube/bin/yt-dlp --download-sections "*00:00:10-00:00:20" URL

# Download com recorte de tempo (usando FFmpeg embutido)
./cutube/bin/yt-dlp --external-downloader "ffmpeg -ss 00:00:10 -to 00:00:20" URL

# === Debug ===

# Verbose (debug)
./cutube/bin/yt-dlp --verbose URL

# Simular (sem download)
./cutube/bin/yt-dlp --simulate URL

# Info do vídeo (JSON)
./cutube/bin/yt-dlp --print json URL

# Info do vídeo (legível)
./cutube/bin/yt-dlp --print "%(title)s\n%(duration)s\n%(uploader)s" URL

# === Desenvolvimento ===

# Ver caminhos usados pelo Cutube
ls -la ~/.local/share/Cutube/
ls -la ./cutube/bin/

# Forçar re-download do yt-dlp bundled
rm -f ./cutube/bin/yt-dlp
curl -L https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp -o ./cutube/bin/yt-dlp
chmod +x ./cutube/bin/yt-dlp
```

---

**Status do Plano:** ✅ COMPLETO  
**Próximos Passos:** Aguardar aprovação para iniciar FASE 0 (Preparação)
