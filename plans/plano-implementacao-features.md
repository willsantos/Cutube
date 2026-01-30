# Plano de Implementação - Cutube Features

**Data:** 30/01/2026
**Autor:** AI Agent
**Status:** Planejamento

---

## 📋 Overview

Este documento detalha a implementação de 6 features para o Cutube:

1. **Tempo Flexível** - Aceitar múltiplos formatos de tempo
2. **Nome Customizado** - Permitir nome do arquivo customizado
3. **Diretório Customizado** - Permitir escolher diretório de destino
4. **Download de Áudio MP3** - Baixar apenas áudio em MP3
5. **Cancelamento** - Permitir CTRL+C para cancelar download
6. **Validações** - Validar todos os inputs

---

## 🎯 Decisões Confirmadas

- **Branch strategy:** Uma branch por feature
- **Commits:** Conventional Commits (feat:, fix:, refactor:, test:, docs:, chore:)
- **Formato áudio:** MP3 (~192kbps, qualidade 2)
- **Backwards compatibility:** NÃO mantida (breaking changes OK)
- **Log file:** NÃO implementado

---

## Feature #1: Tempo Flexível ⭐⭐⭐

**Branch:** `feature/flexible-time-input`
**Priority:** HIGH
**Estimativa:** 4-6h
**Dependências:** Nenhuma

### Objetivo
Aceitar múltiplos formatos de tempo, não apenas `hh:mm:ss` rígido.

### Implementação

**Arquivos modificados:**
- `cutube/TimeHelper.cs` - Reescrever completamente
- `cutube/Menu.cs` - Atualizar help text
- `Cutube.Tests/Unit/TimeHelperTests.cs` - Novos testes

**Formatos suportados:**

| Formato | Exemplo | Segundos |
|---------|---------|----------|
| hh:mm:ss | 00:01:30 | 90 |
| mm:ss | 01:30 | 90 |
| Decimal | 90 ou 90.5 | 90 |
| Compacto | 1h30m | 5400 |
| Compacto | 90m | 5400 |
| Compacto | 90s | 90 |
| Compacto | 1h30m15s | 5415 |

**Código principal:**

```csharp
// TimeHelper.cs - REESCREVER COMPLETAMENTE
public class TimeHelper
{
    public static int ParseToSeconds(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            throw new ArgumentException("Tempo não pode ser vazio");

        input = input.Trim().ToLower();

        // 1. Tentar formato notação compacta (1h30m, 90s, etc)
        if (TryParseCompactNotation(input, out var seconds))
            return seconds;

        // 2. Tentar formato com colons (hh:mm:ss, mm:ss, etc)
        if (TryParseColonFormat(input, out seconds))
            return seconds;

        // 3. Tentar formato decimal (90, 90.5)
        if (double.TryParse(input, out var totalSeconds))
            return (int)totalSeconds;

        throw new FormatException($"Formato de tempo não reconhecido: {input}");
    }

    private static bool TryParseCompactNotation(string input, out int seconds)
    {
        seconds = 0;
        var regex = new Regex(@"^(?:(?<hours>\d+)h)?(?:(?<minutes>\d+)m)?(?:(?<secs>\d+(?:\.\d+)?)s)?$",
            RegexOptions.IgnoreCase);
        var match = regex.Match(input);

        if (!match.Success) return false;

        var total = 0.0;
        if (match.Groups["hours"].Success)
            total += double.Parse(match.Groups["hours"].Value) * 3600;
        if (match.Groups["minutes"].Success)
            total += double.Parse(match.Groups["minutes"].Value) * 60;
        if (match.Groups["secs"].Success)
            total += double.Parse(match.Groups["secs"].Value);

        if (total == 0) return false;

        seconds = (int)total;
        return true;
    }

    private static bool TryParseColonFormat(string input, out int seconds)
    {
        seconds = 0;
        var parts = input.Split(':');

        if (parts.Length == 2) // mm:ss
        {
            if (int.TryParse(parts[0], out var minutes) &&
                double.TryParse(parts[1], out var secs))
            {
                seconds = minutes * 60 + (int)secs;
                return true;
            }
        }
        else if (parts.Length == 3) // hh:mm:ss ou hh:mm:ss:fff
        {
            if (int.TryParse(parts[0], out var hours) &&
                int.TryParse(parts[1], out var minutes) &&
                double.TryParse(parts[2], out var secs))
            {
                seconds = hours * 3600 + minutes * 60 + (int)secs;
                return true;
            }
        }

        return false;
    }

    public static int GetStartSeconds(string start) => ParseToSeconds(start);
    public static int GetEndSeconds(string end) => ParseToSeconds(end);
    public static int GetTimeDiff(string start, string end)
        => GetEndSeconds(end) - GetStartSeconds(start);
}
```

**Validações:**
- Tempo não vazio
- Tempo > 0
- End > Start
- Formato reconhecido, senão mostrar exemplos

**Mensagens de erro:**
```
❌ Formato inválido. Use um destes formatos:
   - 00:01:30 ou 1:30 (hora:minuto:segundo)
   - 90 ou 90.5 (segundos)
   - 1h30m ou 90m ou 90s (notação compacta)

❌ Tempo final deve ser maior que tempo inicial
```

**Menu atualizado:**
```csharp
Console.WriteLine("2 - Digite o tempo de início (ex: 00:01:30, 1:30, 90s, 1h30m)");
Console.WriteLine("3 - Digite o tempo de fim (ex: 00:02:00, 2:00, 120s, 2m)");
```

**Breaking Changes:**
- TimeHelper muda completamente (API diferente)
- Menu muda texto de help
- Sem backwards compatibility

**Testes necessários:**
- Unit test para cada formato suportado
- Unit test para formato inválido (exception)
- Unit test para formato edge case (0, decimal, etc)
- Integration test com Menu real

---

## Feature #2: Nome Customizado ⭐

**Branch:** `feature/custom-filename`
**Priority:** MEDIUM
**Estimativa:** 1-2h
**Dependências:** Nenhuma

### Objetivo
Permitir nome customizado para o arquivo de saída.

### Implementação

**Arquivos modificados:**
- `cutube/Menu.cs` - Adicionar `CustomFileName`
- `cutube/IMenuService.cs` - Adicionar propriedade
- `cutube/ProgramWorkflow.cs` - Usar nome customizado
- `Cutube.Tests/Unit/MenuTests.cs` - Testes

**Código principal:**

```csharp
// Menu.cs
public static string CustomFileName { get; private set; } = string.Empty;

public static void Show()
{
    // ... código existente (URL, Start, End) ...
    Console.WriteLine("4 - Digite o nome do arquivo (opcional, pressione Enter para usar título)");
    var input = Console.ReadLine() ?? string.Empty;

    if (!string.IsNullOrWhiteSpace(input))
    {
        CustomFileName = Path.GetFileNameWithoutExtension(input.Trim());
    }
}

// IMenuService.cs
public interface IMenuService
{
    void Show();
    string Url { get; }
    string Start { get; }
    string End { get; }
    string CustomFileName { get; }  // NOVO
}

// ProgramWorkflow.cs
var fileName = string.IsNullOrWhiteSpace(_menu.CustomFileName)
    ? videoTitle
    : TitleHelper.FormatTitle(_menu.CustomFileName);
var output = $"{fileName}.mp4";
```

**Validações:**
- Sanitizar nome (TitleHelper)
- Remover extensão se usuário digitou `.mp4`
- Nome não vazio após sanitização
- Caracteres inválidos removidos automaticamente

**Mensagens:**
```
4 - Nome do arquivo (opcional, Enter para usar título)
   Ex: meu-podcast-01

⚠ Nome sanitizado: meu_podcast_01.mp4
```

**Breaking Changes:**
- Menu agora pergunta nome (4ª pergunta)
- IMenuService tem nova propriedade
- ProgramWorkflow usa nome customizado

**Testes necessários:**
- Unit test para Menu com nome customizado
- Unit test para ProgramWorkflow usando nome customizado
- Unit test para fallback ao título quando nome é vazio

---

## Feature #3: Diretório de Destino ⭐

**Branch:** `feature/custom-output-directory`
**Priority:** MEDIUM
**Estimativa:** 1-2h
**Dependências:** #2

### Objetivo
Permitir escolher onde salvar o arquivo.

### Implementação

**Arquivos modificados:**
- `cutube/Menu.cs` - Adicionar `OutputDirectory`
- `cutube/IMenuService.cs` - Adicionar propriedade
- `cutube/IFileService.cs` - Adicionar `DirectoryExists()`
- `cutube/FileService.cs` - Implementar validação
- `cutube/ProgramWorkflow.cs` - Combinar dir + nome
- `Cutube.Tests/Unit/MenuTests.cs` - Testes

**Código principal:**

```csharp
// Menu.cs
public static string OutputDirectory { get; private set; } = string.Empty;

public static void Show()
{
    // ... inputs existentes ...
    Console.WriteLine("5 - Digite o diretório de destino (opcional, pressione Enter para usar atual)");
    var input = Console.ReadLine() ?? string.Empty;
    OutputDirectory = input.Trim();
}

// IFileService.cs
public interface IFileService
{
    bool Exists(string path);
    bool DirectoryExists(string path);  // NOVO
}

// FileService.cs
public bool DirectoryExists(string path)
{
    return Directory.Exists(path);
}

// ProgramWorkflow.cs
var directory = string.IsNullOrWhiteSpace(_menu.OutputDirectory)
    ? Directory.GetCurrentDirectory()
    : _menu.OutputDirectory;

// Validar diretório
if (!_fileService.DirectoryExists(directory))
{
    throw new DirectoryNotFoundException($"Diretório não encontrado: {directory}");
}

var output = Path.Combine(directory, $"{fileName}.mp4");
```

**Validações:**
- Diretório existe
- Tem permissão de escrita
- Caminho válido para o OS
- Criar diretório se não existir (perguntar)

**Mensagens:**
```
5 - Diretório de destino (opcional, Enter para usar atual)
   Ex: /home/user/downloads ou C:\Videos

❌ Diretório não encontrado: /invalid/path
   Deseja criá-lo? (s/N)
```

**Breaking Changes:**
- Menu agora pergunta diretório (5ª pergunta)
- IFileService tem novo método
- ProgramWorkflow muda lógica de output path

**Testes necessários:**
- Unit test para Menu com diretório customizado
- Unit test para validação de diretório existente
- Unit test para exception quando diretório não existe
- Unit test para fallback ao diretório atual

---

## Feature #4: Download de Áudio MP3 ⭐⭐

**Branch:** `feature/audio-only-download`
**Priority:** MEDIUM
**Estimativa:** 3-4h
**Dependências:** #2, #3

### Objetivo
Baixar apenas o áudio em MP3.

### Implementação

**Arquivos modificados:**
- `cutube/Menu.cs` - Adicionar `AudioOnly`
- `cutube/IMenuService.cs` - Adicionar propriedade
- `cutube/IYtDlpService.cs` - Adicionar `DownloadAudioAsync()`
- `cutube/YtDlpHelper.cs` - Implementar download MP3
- `cutube/ProgramWorkflow.cs` - Escolher método baseado em AudioOnly
- `Cutube.Tests/Unit/YtDlpHelperTests.cs` - Testes

**Código principal:**

```csharp
// Menu.cs
public static bool AudioOnly { get; private set; }

public static void Show()
{
    // ... inputs existentes ...
    Console.WriteLine("6 - Deseja baixar apenas o áudio? (s/N)");
    var audioOnly = Console.ReadLine()?.ToLower();
    AudioOnly = audioOnly == "s" || audioOnly == "sim";
}

// IMenuService.cs
public interface IMenuService
{
    void Show();
    string Url { get; }
    string Start { get; }
    string End { get; }
    string CustomFileName { get; }
    string OutputDirectory { get; }
    bool AudioOnly { get; }  // NOVO
}

// IYtDlpService.cs
public interface IYtDlpService
{
    Task<string> GetVideoTitleAsync(string url);
    Task DownloadWithTimeRangeAsync(
        string url,
        string outputFile,
        string startTime,
        string endTime,
        IProgress<DownloadProgress>? progress = null);

    Task DownloadAudioAsync(  // NOVO MÉTODO
        string url,
        string outputFile,
        string startTime,
        string endTime,
        IProgress<DownloadProgress>? progress = null);
}

// YtDlpHelper.cs
public async Task DownloadAudioAsync(
    string url,
    string outputFile,
    string startTime,
    string endTime,
    IProgress<DownloadProgress>? progress = null)
{
    var tempFile = CreateTempFile();

    // Download de áudio apenas
    var options = new OptionSet
    {
        Format = "bestaudio/best",
        ExtractAudio = true,
        AudioFormat = AudioConversionFormat.Mp3,
        AudioQuality = 2,  // Alta qualidade (~192kbps)
        Output = tempFile
    };

    await RunVideoDownloadAsync(url, options, progress);

    // Cortar áudio com FFmpeg
    var timeStart = TimeHelper.GetStartSeconds(startTime);
    var timeEnd = TimeHelper.GetEndSeconds(endTime);
    var duration = timeEnd - timeStart;

    _consoleService.WriteLine($"Cortando áudio ({startTime} - {endTime})...");

    var arguments =
        $"-i \"{tempFile}\" " +
        $"-ss {timeStart} " +
        $"-t {duration} " +
        $"-c:a libmp3lame " +
        $"-q:a 2 " +
        $"\"{outputFile}\"";

    ExecuteFfmpeg(arguments);
    _fileService.Delete(tempFile);
}

// ProgramWorkflow.cs
var extension = _menu.AudioOnly ? ".mp3" : ".mp4";
var output = Path.Combine(directory, $"{fileName}{extension}");

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
```

**Configurações MP3:**
```
Format: bestaudio/best
AudioFormat: Mp3
AudioQuality: 2 (escala 0-9, 2 é alta qualidade)
Bitrate: ~192k
Extension: .mp3
```

**Validações:**
- Arquivo MP3 válido após download
- Tamanho mínimo (evitar arquivos corrompidos)
- Duração correta (start -> end)

**Mensagens:**
```
6 - Download completo ou áudio apenas?
   1 - Vídeo + Áudio (MP4)
   2 - Apenas Áudio (MP3)
   Escolha: 2

⏳ Baixando áudio (MP3, 192kbps)...
```

**Breaking Changes:**
- Menu pergunta vídeo ou áudio (6ª pergunta)
- IYtDlpService tem novo método
- ProgramWorkflow tem branch condicional

**Testes necessários:**
- Unit test para Menu com AudioOnly true/false
- Unit test para YtDlpHelper.DownloadAudioAsync
- Integration test para download + corte de áudio
- Verificar que arquivo de saída é áudio válido

---

## Feature #5: Cancelamento de Download ⭐⭐

**Branch:** `feature/download-cancellation`
**Priority:** MEDIUM
**Estimativa:** 2-3h
**Dependências:** Nenhuma

### Objetivo
Permitir cancelar download com CTRL+C sem corromper arquivo.

### Implementação

**Arquivos modificados:**
- `cutube/Program.cs` - Adicionar `Console.CancelKeyPress`
- `cutube/IYtDlpService.cs` - Adicionar `CancellationToken`
- `cutube/YtDlpHelper.cs` - Suportar cancelamento
- `cutube/IFfmpegHelper.cs` - Adicionar `CancellationToken`
- `cutube/FfmpegHelper.cs` - Implementar cancelamento
- `cutube/ProgramWorkflow.cs` - Cleanup adequado

**Código principal:**

```csharp
// Program.cs
public static async Task Main()
{
    using var cts = new CancellationTokenSource();

    Console.CancelKeyPress += (s, e) => {
        e.Cancel = true;
        Console.WriteLine("\n⚠ Cancelando download...");
        cts.Cancel();
    };

    var app = new ProgramWorkflow(
        new MenuService(),
        new YtDlpHelper(),
        new ConsoleService(),
        cts.Token  // NOVO
    );

    try
    {
        await app.RunAsync();
    }
    catch (OperationCanceledException)
    {
        Console.WriteLine("\n✓ Download cancelado pelo usuário");
        Environment.Exit(0);
    }
}

// IYtDlpService.cs
public interface IYtDlpService
{
    Task DownloadWithTimeRangeAsync(
        string url,
        string outputFile,
        string startTime,
        string endTime,
        IProgress<DownloadProgress>? progress = null,
        CancellationToken cancellationToken = default);  // NOVO
}

// YtDlpHelper.cs
public async Task DownloadWithTimeRangeAsync(
    string url,
    string outputFile,
    string startTime,
    string endTime,
    IProgress<DownloadProgress>? progress = null,
    CancellationToken cancellationToken = default)  // NOVO
{
    var tempFile = CreateTempFile();

    try
    {
        _consoleService.WriteLine("Baixando vídeo completo...");
        await DownloadAsync(url, tempFile, progress, cancellationToken);

        var timeStart = TimeHelper.GetStartSeconds(startTime);
        var timeEnd = TimeHelper.GetEndSeconds(endTime);
        var duration = timeEnd - timeStart;

        _consoleService.WriteLine($"Cortando vídeo ({startTime} - {endTime})...");

        var ffmpeg = new FfmpegHelper();
        var arguments =
            $"-i \"{tempFile}\" " +
            $"-ss {timeStart} " +
            $"-t {duration} " +
            $"-c:v libx264 -c:a aac " +
            $"\"{outputFile}\"";

        ExecuteFfmpeg(arguments, new ProgressBar(), cancellationToken);
    }
    catch (OperationCanceledException)
    {
        _consoleService.WriteLine("❌ Cancelado, limpando arquivos temporários...");
        _fileService.Delete(tempFile);
        throw;
    }
}
```

**Lógica:**
```csharp
var cts = new CancellationTokenSource();
Console.CancelKeyPress += (s, e) => {
    e.Cancel = true;
    _console.WriteLine("\n⚠ Cancelando...");
    cts.Cancel();
};

try {
    await DownloadAsync(..., cts.Token);
}
catch (OperationCanceledException) {
    _fileService.Delete(tempFile);
    _console.WriteLine("✓ Cancelado");
}
```

**Validações:**
- Temp files deletados
- Arquivos parciais removidos
- Mensagem clara de cancelamento

**Breaking Changes:**
- IYtDlpService muda assinatura (adiciona CancellationToken)
- IFfmpegHelper muda assinatura
- ProgramWorkflow trata cancelamento

**Testes necessários:**
- Unit test para cancelamento durante download
- Unit test para cancelamento durante corte FFmpeg
- Integration test para cleanup de arquivos

---

## Feature #6: Validações Robustas ⭐

**Branch:** `feature/input-validations`
**Priority:** LOW
**Estimativa:** 1-2h
**Dependências:** Todas

### Objetivo
Adicionar validações em todos os inputs.

### Implementação

**Arquivos criados:**
- `cutube/ValidationHelper.cs` - Novo helper

**Arquivos modificados:**
- `cutube/ProgramWorkflow.cs` - Adicionar validações
- `cutube/Menu.cs` - Melhorar mensagens de erro
- `Cutube.Tests/Unit/ValidationHelperTests.cs` - Testes

**Código principal:**

```csharp
// ValidationHelper.cs - NOVO ARQUIVO
public static class ValidationHelper
{
    public static bool IsValidUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        return Uri.TryCreate(url, UriKind.Absolute, out var uriResult)
            && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
    }

    public static bool IsValidTimeRange(int start, int end, int? maxDuration = null)
    {
        if (start < 0 || end < 0)
            return false;

        if (end <= start)
            return false;

        if (maxDuration.HasValue && end > maxDuration.Value)
            return false;

        return true;
    }

    public static bool IsValidFileName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;

        // Caracteres inválidos em Windows/Linux
        var invalidChars = new char[] { '<', '>', ':', '"', '/', '\\', '|', '?', '*' };
        return !name.Any(c => invalidChars.Contains(c));
    }

    public static bool IsDirectoryWritable(string path)
    {
        try
        {
            var testFile = Path.Combine(path, Guid.NewGuid().ToString());
            File.WriteAllText(testFile, "test");
            File.Delete(testFile);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static void ValidateUrl(string url)
    {
        if (!IsValidUrl(url))
            throw new ArgumentException("URL inválida. Use http:// ou https://");
    }

    public static void ValidateTimeRange(string start, string end, int? maxDuration = null)
    {
        var startSec = TimeHelper.GetStartSeconds(start);
        var endSec = TimeHelper.GetEndSeconds(end);

        if (!IsValidTimeRange(startSec, endSec, maxDuration))
            throw new ArgumentException(
                $"Tempo inválido: start ({startSec}s) deve ser menor que end ({endSec}s)"
            );
    }

    public static void ValidateFileName(string name)
    {
        if (!IsValidFileName(name))
            throw new ArgumentException("Nome de arquivo contém caracteres inválidos");
    }

    public static void ValidateDirectory(string path)
    {
        if (!Directory.Exists(path))
            throw new DirectoryNotFoundException($"Diretório não existe: {path}");

        if (!IsDirectoryWritable(path))
            throw new UnauthorizedAccessException($"Sem permissão de escrita: {path}");
    }
}

// ProgramWorkflow.cs
public async Task RunAsync()
{
    _menu.Show();

    // NOVO: Validar todos os inputs
    ValidationHelper.ValidateUrl(_menu.Url);
    ValidationHelper.ValidateTimeRange(_menu.Start, _menu.End);

    if (!string.IsNullOrWhiteSpace(_menu.CustomFileName))
        ValidationHelper.ValidateFileName(_menu.CustomFileName);

    if (!string.IsNullOrWhiteSpace(_menu.OutputDirectory))
        ValidationHelper.ValidateDirectory(_menu.OutputDirectory);

    // ... resto do código ...
}
```

**Validações:**

| Input | Validações |
|-------|------------|
| **URL** | - Formato válido<br>- Suportada pelo yt-dlp<br>- Não vazia |
| **Start Time** | - Formato válido<br>- > 0<br>- < End time |
| **End Time** | - Formato válido<br>- > Start time<br>- < duração vídeo |
| **Nome** | - Sanitizado<br>- Não vazio<br>- Sem caminho |
| **Diretório** | - Existe<br>- Writable<br>- Caminho válido |
| **Áudio?** | - Sim/Não<br>- Confirmar extensão |

**Breaking Changes:**
- ProgramWorkflow valida todos os inputs antes de processar
- Exceções mais descritivas

**Testes necessários:**
- Unit test para cada método de validação
- Unit test para exceções com inputs inválidos
- Integration test com ProgramWorkflow

---

## 📁 Estratégia de Branches

Este projeto usa **uma branch por feature** com Conventional Commits.

### Workflow

1. **Criar branch da feature:**
   ```bash
   git checkout -b feature/<nome>
   ```

2. **Implementar feature:**
   - Escrever código
   - Escrever testes
   - Rodar testes: `dotnet test`
   - Rodar build: `dotnet build`

3. **Fazer commit com conventional commit:**
   ```bash
   git add .
   git commit -m "feat: descrição"
   ```

4. **Push e criar PR:**
   ```bash
   git push origin feature/<nome>
   # Criar PR no GitHub
   ```

5. **Merge e cleanup:**
   ```bash
   git checkout main
   git pull
   git branch -d feature/<nome>
   ```

### Branches

1. `feature/flexible-time-input` - Feature #1
2. `feature/custom-filename` - Feature #2
3. `feature/custom-output-directory` - Feature #3
4. `feature/audio-only-download` - Feature #4
5. `feature/download-cancellation` - Feature #5
6. `feature/input-validations` - Feature #6

---

## 📝 Conventional Commits

### Tipos utilizados

- `feat:` - Nova feature
- `fix:` - Bug fix
- `refactor:` - Refatoração sem mudança de comportamento
- `test:` - Adicionar/atualizar testes
- `docs:` - Documentação (README, AGENTS.md)
- `chore:` - Build, dependências, tooling

### Exemplos

```bash
# Features
git commit -m "feat: add flexible time input parsing (1h30m, 90s, etc)"
git commit -m "feat: add custom filename option"
git commit -m "feat: add custom output directory option"
git commit -m "feat: add audio-only download with MP3 support"
git commit -m "feat: add CTRL+C cancellation support"
git commit -m "feat: add input validations"

# Documentação
git commit -m "docs: update AGENTS.md with branch strategy"
git commit -m "docs: update README with new features"

# Testes
git commit -m "test: add unit tests for TimeHelper"
git commit -m "test: add integration tests for audio download"

# Refatoração
git commit -m "refactor: extract validation logic to ValidationHelper"
```

---

## 📊 Ordem de Execução

```
1. feature/flexible-time-input (4-6h)
   └─> Tests, docs, commit

2. feature/custom-filename (1-2h)
   └─> Tests, docs, commit

3. feature/custom-output-directory (1-2h)
   └─> Tests, docs, commit

4. feature/audio-only-download (3-4h)
   └─> Tests, docs, commit

5. feature/download-cancellation (2-3h)
   └─> Tests, docs, commit

6. feature/input-validations (1-2h)
   └─> Tests, docs, commit
```

**Total estimado:** 12-19 horas

---

## ✅ Critérios de Sucesso

Cada feature está completa quando:

- [x] Código implementado
- [x] Testes unitários passando (`dotnet test`)
- [x] Build sem erros (`dotnet build`)
- [x] Sem warnings do compilador
- [x] README atualizado (se necessário)
- [x] AGENTS.md atualizado (se necessário)
- [x] Manual testado (rodar o programa)
- [x] Conventional commit usado
- [x] Push para remoto
- [x] PR criada (se aplicável)

---

## 🔗 Recursos

- **AGENTS.md** - Instruções para agentes
- **README.md** - Documentação do projeto
- **bd (beads)** - Issue tracking (`bd ready`, `bd show <id>`, etc.)

---

**Status:** Plano Completo
**Estimativa Total:** 12-19 horas
**Data Criação:** 30/01/2026
