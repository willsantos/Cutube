# Plano de Implementação - Cutube-5bf

## Download de Áudio MP3 - Baixar apenas áudio em formato MP3

**Task ID:** Cutube-5bf  
**Prioridade:** P2  
**Tipo:** feature  
**Status:** 📋 Planejamento

---

## 📋 Resumo

Implementar download de áudio apenas em formato MP3 (~192kbps) com suporte a recorte de tempo, permitindo que o usuário escolha entre baixar vídeo completo (MP4) ou apenas áudio (MP3).

---

## 🎯 Objetivos

1. Adicionar opção no menu para escolha entre vídeo ou áudio
2. Implementar download de áudio MP3 com qualidade alta (~192kbps)
3. Suportar recorte de tempo em arquivos de áudio
4. Garantir validações de arquivo MP3 válido

---

## 📦 Escopo

### Funcionalidades Incluídas
- ✅ Propriedade `AudioOnly` em Menu
- ✅ Nova pergunta no menu (6ª: vídeo ou áudio?)
- ✅ Método `DownloadAudioAsync()` em IYtDlpService
- ✅ Implementação em YtDlpHelper usando yt-dlp + FFmpeg
- ✅ Atualização do ProgramWorkflow com branch condicional
- ✅ Testes unitários para nova funcionalidade

### Funcionalidades Excluídas
- ❌ Conversão de formatos (apenas MP3)
- ❌ Seleção de qualidade de áudio (fixo em ~192kbps)
- ❌ Download de múltiplos áudios
- ❌ Metadata/ID3 tags customizadas

---

## 🔧 Mudanças Técnicas

### 1. Camada de Menu

#### `cutube/Menu.cs`
```csharp
// Nova propriedade
public static bool AudioOnly { get; private set; } = false;

// Atualizar Reset()
internal static void Reset()
{
    // ... existentes ...
    AudioOnly = false;
}

// Nova pergunta no Show()
Console.WriteLine("6 - Download completo ou áudio apenas?");
Console.WriteLine("   1 - Vídeo + Áudio (MP4)");
Console.WriteLine("   2 - Apenas Áudio (MP3)");
var audioChoice = Console.ReadLine();
AudioOnly = audioChoice == "2";
```

#### `cutube/IMenuService.cs`
```csharp
bool AudioOnly { get; }
```

#### `cutube/MenuService.cs`
```csharp
public bool AudioOnly => Menu.AudioOnly;
```

---

### 2. Camada de Serviço YtDlp

#### `cutube/IYtDlpService.cs`
```csharp
Task DownloadAudioAsync(
    string url,
    string outputFile,
    string startTime,
    string endTime,
    IProgress<DownloadProgress>? progress = null);
```

#### `cutube/YtDlpHelper.cs`

**Novo método:**
```csharp
public async Task DownloadAudioAsync(
    string url,
    string outputFile,
    string startTime,
    string endTime,
    IProgress<DownloadProgress>? progress = null)
{
    // Download de áudio completo primeiro
    var tempFile = CreateTempFile(); // .mp4 temporário
    
    _consoleService.WriteLine("Baixando áudio completo...");
    await DownloadAudioFullAsync(url, tempFile, progress);
    
    // Converter para MP3 e cortar com FFmpeg
    var timeStart = TimeHelper.GetStartSeconds(startTime);
    var timeEnd = TimeHelper.GetEndSeconds(endTime);
    var duration = timeEnd - timeStart;
    
    _consoleService.WriteLine($"Convertendo para MP3 e cortando ({startTime} - {endTime})...");
    
    var arguments =
        $"-i \"{tempFile}\" " +
        $"-ss {timeStart} " +
        $"-t {duration} " +
        $"-vn " +  // No video
        $"-c:a libmp3lame " +
        $"-q:a 2 " +  // Qualidade alta (~192kbps)
        $"\"{outputFile}\"";
    
    ExecuteFfmpeg(arguments);
    
    // Limpar temp
    _fileService.Delete(tempFile);
}
```

**Método auxiliar privado:**
```csharp
private async Task DownloadAudioFullAsync(
    string url,
    string outputFile,
    IProgress<DownloadProgress>? progress = null)
{
    var options = new OptionSet
    {
        Format = "bestaudio/best",
        ExtractAudio = true,
        AudioFormat = AudioFormat.Mp3,
        AudioQuality = 2,
        Output = outputFile
    };
    
    await RunVideoDownloadAsync(url, options, progress);
}
```

---

### 3. Camada de Workflow

#### `cutube/ProgramWorkflow.cs`

**Alterações em `RunAsync()`:**

```csharp
// Determinar extensão do arquivo
var extension = _menu.AudioOnly ? ".mp3" : ".mp4";
var output = Path.Combine(outputDir, $"{fileName}{extension}");

// ... validações de diretório ...

try
{
    var typeLabel = _menu.AudioOnly ? "áudio" : "vídeo";
    _consoleService.WriteLine($"Iniciando o download e corte do {typeLabel}...");
    _consoleService.WriteLine("Esse processo pode demorar, aguarde...");

    var progress = new Progress<DownloadProgress>(p => { /* ... */ });

    // Branch condicional
    if (_menu.AudioOnly)
    {
        await _ytdl.DownloadAudioAsync(
            videoUrl,
            output,
            videoStart,
            videoEnd,
            progress
        );
    }
    else
    {
        await _ytdl.DownloadWithTimeRangeAsync(
            videoUrl,
            output,
            videoStart,
            videoEnd,
            progress
        );
    }
}
catch (Exception e)
{
    _consoleService.WriteLine($"Erro: {e.Message}");
    throw;
}
finally
{
    var typeLabel = _menu.AudioOnly ? "Áudio" : "Vídeo";
    _consoleService.WriteLine($"✓ {typeLabel} salvo em: {Path.GetFullPath(output)}");
}
```

---

### 4. Testes Unitários

#### `Cutube.Tests/Unit/YtDlpHelperTests.cs`

**Nova classe helper para testes:**
```csharp
private class AudioWorkflowYtDlpHelper : YtDlpHelper
{
    public OptionSet? CapturedAudioOptions { get; private set; }
    public string? CapturedAudioFfmpegArguments { get; private set; }

    public AudioWorkflowYtDlpHelper(
        IFileService fileService,
        IHttpClientService httpClientService,
        IEnvironmentService environmentService,
        IProcessService processService,
        IConsoleService consoleService)
        : base(fileService, httpClientService, environmentService, processService, consoleService, true)
    {
    }

    protected internal override Task RunVideoDownloadAsync(
        string url, OptionSet options, IProgress<DownloadProgress>? progress)
    {
        CapturedAudioOptions = options;
        return Task.CompletedTask;
    }

    protected internal override void ExecuteFfmpeg(string arguments)
    {
        CapturedAudioFfmpegArguments = arguments;
    }
}
```

**Novos testes:**
```csharp
[Fact]
public async Task DownloadAudioAsync_SetsCorrectOptions()
{
    var helper = new AudioWorkflowYtDlpHelper(
        _mockFile.Object,
        _mockHttp.Object,
        _mockEnv.Object,
        _mockProcess.Object,
        _mockConsole.Object
    );

    await helper.DownloadAudioAsync(
        "https://youtu.be/test",
        "output.mp3",
        "00:00:10",
        "00:00:20"
    );

    helper.CapturedAudioOptions.Should().NotBeNull();
    helper.CapturedAudioOptions!.Format.Should().Be("bestaudio/best");
    helper.CapturedAudioOptions.ExtractAudio.Should().BeTrue();
    helper.CapturedAudioOptions.AudioFormat.Should().Be(AudioFormat.Mp3);
    helper.CapturedAudioOptions.AudioQuality.Should().Be(2);
}

[Fact]
public async Task DownloadAudioAsync_UsesCorrectFfmpegCodec()
{
    _mockFile.Setup(x => x.Delete(It.IsAny<string>()));

    var helper = new AudioWorkflowYtDlpHelper(
        _mockFile.Object,
        _mockHttp.Object,
        _mockEnv.Object,
        _mockProcess.Object,
        _mockConsole.Object
    );

    await helper.DownloadAudioAsync(
        "https://youtu.be/test",
        "output.mp3",
        "00:00:10",
        "00:00:20"
    );

    helper.CapturedAudioFfmpegArguments.Should().Contain("-c:a libmp3lame");
    helper.CapturedAudioFfmpegArguments.Should().Contain("-q:a 2");
    helper.CapturedAudioFfmpegArguments.Should().Contain("-vn"); // No video
    helper.CapturedAudioFfmpegArguments.Should().Contain("-ss 10");
    helper.CapturedAudioFfmpegArguments.Should().Contain("-t 10");
    helper.CapturedAudioFfmpegArguments.Should().Contain("\"output.mp3\"");
}

[Fact]
public async Task DownloadAudioAsync_DeletesTempFile()
{
    _mockFile.Setup(x => x.Delete("/tmp/temp.mp4.mp4"));

    var helper = new AudioWorkflowYtDlpHelper(
        _mockFile.Object,
        _mockHttp.Object,
        _mockEnv.Object,
        _mockProcess.Object,
        _mockConsole.Object
    );

    await helper.DownloadAudioAsync(
        "https://youtu.be/test",
        "output.mp3",
        "00:00:10",
        "00:00:20"
    );

    _mockFile.Verify(x => x.Delete(It.IsAny<string>()), Times.Once);
}
```

---

## 📊 Configurações MP3

| Parâmetro | Valor | Descrição |
|-----------|-------|-----------|
| Format | `bestaudio/best` | Melhor qualidade de áudio |
| ExtractAudio | `true` | Extrair apenas áudio |
| AudioFormat | `Mp3` | Formato de saída MP3 |
| AudioQuality | `2` | Qualidade alta (~192kbps) |
| FFmpeg Codec | `libmp3lame` | Encoder MP3 |
| FFmpeg Quality | `-q:a 2` | VBR qualidade 2 (~190kbps) |
| FFmpeg No Video | `-vn` | Descartar stream de vídeo |

---

## ✅ Validações

### Pós-Download
- [ ] Arquivo MP3 criado (tamanho > 0)
- [ ] Duração correta (end - start)
- [ ] Codec de áudio válido (libmp3lame)
- [ ] Arquivo temporário removido
- [ ] Permissões de arquivo corretas

### Entrada do Menu
- [ ] Validação de entrada 1 ou 2
- [ ] Default para vídeo (1) se vazio
- [ ] Mensagem de erro se inválido

---

## 🚀 Fluxo de Execução

```
1. Usuário executa Cutube
2. Menu.Show() coleta dados:
   - URL
   - Tempo início
   - Tempo fim
   - Nome arquivo (opcional)
   - Diretório (opcional)
   - Tipo de download: [1] Vídeo MP4 ou [2] Áudio MP3
3. ProgramWorkflow.RunAsync():
   - Valida diretório de saída
   - Determina extensão (.mp4 ou .mp3)
   - Chama método apropriado:
     - AudioOnly = true  → DownloadAudioAsync()
     - AudioOnly = false → DownloadWithTimeRangeAsync()
4. Download + corte
5. Arquivo salvo no diretório especificado
```

---

## 📁 Arquivos Modificados

| Arquivo | Linhas (estimado) | Tipo |
|---------|------------------|------|
| `cutube/Menu.cs` | +8 | Modificação |
| `cutube/IMenuService.cs` | +1 | Modificação |
| `cutube/MenuService.cs` | +1 | Modificação |
| `cutube/IYtDlpService.cs` | +7 | Modificação |
| `cutube/YtDlpHelper.cs` | +50 | Modificação |
| `cutube/ProgramWorkflow.cs` | +15 | Modificação |
| `Cutube.Tests/Unit/YtDlpHelperTests.cs` | +80 | Modificação |

---

## 🔄 Breaking Changes

### Menu
- **ANTES:** 5 perguntas
- **DEPOIS:** 6 perguntas (nova pergunta sobre tipo de download)

### IYtDlpService
- **ANTES:** 2 métodos públicos
- **DEPOIS:** 3 métodos públicos (novo `DownloadAudioAsync`)

### Comportamento
- **ANTES:** Sempre baixa vídeo MP4
- **DEPOIS:** Permite escolher entre MP4 ou MP3

---

## 🧪 Testes

### Unitários (Novos)
- `DownloadAudioAsync_SetsCorrectOptions`
- `DownloadAudioAsync_UsesCorrectFfmpegCodec`
- `DownloadAudioAsync_DeletesTempFile`
- `DownloadAudioAsync_CorrectTimeRange`

### Integração (Manual)
1. Download de áudio completo
2. Download de áudio com recorte
3. Download de vídeo (sem regressão)
4. Validação de arquivo MP3 gerado
5. Teste com diferentes durações

---

## 📅 Cronograma

| Fase | Tarefa | Estimativa |
|------|--------|------------|
| 1 | Modificar Menu (Menu.cs, IMenuService.cs, MenuService.cs) | 15 min |
| 2 | Adicionar método em IYtDlpService.cs | 5 min |
| 3 | Implementar DownloadAudioAsync em YtDlpHelper.cs | 30 min |
| 4 | Atualizar ProgramWorkflow.cs | 20 min |
| 5 | Escrever testes unitários | 30 min |
| 6 | Testes manuais e validação | 15 min |
| 7 | Build e lint | 5 min |
| **Total** | | **~2 horas** |

---

## 🎯 Critérios de Sucesso

- [ ] Usuário pode escolher entre vídeo MP4 ou áudio MP3
- [ ] Download de áudio MP3 funciona com recorte de tempo
- [ ] Qualidade do áudio é ~192kbps (VBR q=2)
- [ ] Todos os testes passam (`dotnet test`)
- [ ] Build sem warnings (`dotnet build`)
- [ ] Código segue convenções do projeto
- [ ] Sem regressão em funcionalidades existentes

---

## 🔗 Dependências

### Dependências Atendidas ✅
- Cutube-1yx (Diretório Customizado) ✓
- Cutube-hxp (Nome Customizado) ✓

### Bloqueia
- Cutube-bi2 (Validações Robustas)

---

## 📝 Commit

```
feat: add audio-only download with MP3 support

- Add AudioOnly property to Menu and IMenuService
- Add 6th menu question: video or audio-only download
- Implement DownloadAudioAsync in IYtDlpService
- Use yt-dlp with --extract-audio and --audio-format mp3
- Convert to MP3 using FFmpeg with libmp3lame codec
- Quality setting: -q:a 2 (~192kbps VBR)
- Update ProgramWorkflow with conditional branch
- Add unit tests for audio download functionality

Closes Cutube-5bf
```

---

## 🌳 Branch

```
feature/audio-only-download
```

---

**Status do Plano:** ✅ Pronto para implementação  
**Data de Criação:** 2026-02-02  
**Última Atualização:** 2026-02-02
