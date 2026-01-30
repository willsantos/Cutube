# Plano de Testes - Cutube

## Metas

- **Framework:** xUnit 2.9+
- **Mocking:** Moq 4.20+
- **Cobertura Alvo:** 90%+
- **Cobertura de Branches:** 85%+

---

## 1. Estrutura do Projeto de Testes

### 1.1 Criar Projeto de Testes

```bash
dotnet new xunit -n Cutube.Tests -f net9.0
dotnet add Cutube.Tests reference ../cutube/cutube.csproj
dotnet add Cutube.Tests package Moq
dotnet add Cutube.Tests package FluentAssertions
dotnet add Cutube.Tests package coverlet.msbuild
```

### 1.2 Estrutura de Diretórios

```
Cutube.Tests/
├── Cutube.Tests.csproj
├── Helpers/
│   ├── TestDataGenerator.cs
│   └── ProcessFake.cs
├── Unit/
│   ├── MenuTests.cs
│   ├── TimeHelperTests.cs
│   └── TitleHelperTests.cs
├── Integration/
│   ├── FfmpegHelperTests.cs
│   └── ProgressBarTests.cs
└── E2E/
    └── ProgramWorkflowTests.cs
```

---

## 2. Análise de Cobertura por Classe

### 2.1 YtDlpHelper.cs (363 linhas)

**Cenários de Teste:**

#### INT-01: Path resolution - versão do usuário existe e é recente
- **Setup:**
  - Mock `File.Exists(_userPath)` retorna true
  - Mock `IsRecentVersion(_userPath)` retorna true
- **Expectativa:** `ResolveYtDlpPath()` retorna _userPath

#### INT-02: Path resolution - usa bundle do app
- **Setup:**
  - Mock `File.Exists(_userPath)` retorna false
  - Mock `File.Exists(_bundledPath)` retorna true
- **Expectativa:** Retorna _bundledPath

#### INT-03: Path resolution - usa PATH do sistema
- **Setup:**
  - Mock `File.Exists(_userPath)` retorna false
  - Mock `File.Exists(_bundledPath)` retorna false
  - Mock `GetSystemPath()` retorna "/usr/bin/yt-dlp"
- **Expectativa:** Retorna _systemPath

#### INT-04: Path resolution - download automático
- **Setup:**
  - Todos os `File.Exists` retornam false
  - Mock `DownloadLatestVersion()` executa com sucesso
- **Expectativa:** Download automático, retorna _userPath

#### INT-05: Path resolution - falha total lança exceção
- **Setup:**
  - Todos os `File.Exists` retornam false
  - Mock `DownloadLatestVersion()` falha
- **Expectativa:** `Exception` com mensagem sobre instalação manual

#### INT-06: Auto-update - versão atualizada
- **Setup:**
  - Mock `GetCurrentVersion()` retorna "2026.01.30"
  - Mock `GetLatestVersion()` retorna "2026.01.30"
- **Expectativa:** Nenhum download, log de "atualizado"

#### INT-07: Auto-update - versão desatualizada
- **Setup:**
  - `GetCurrentVersion()` retorna "2026.01.01"
  - `GetLatestVersion()` retorna "2026.01.30"
  - Mock `DownloadLatestVersion()` sucesso
- **Expectativa:** Download executado, _ytdl atualizado

#### INT-08: Auto-update - falha na atualização
- **Setup:**
  - `GetLatestVersion()` lança exceção
- **Expectativa:** Exceção capturada, log de aviso, não falha app

#### INT-09: GetVideoTitleAsync - sucesso
- **Setup:**
  - Mock `_ytdl.RunVideoDataFetch()` retorna dict com "title"
- **Expectativa:** Título sanitizado retornado

#### INT-10: DownloadAsync - sucesso
- **Setup:**
  - Mock `_ytdl.DownloadVideoAsync()` executa sem erro
- **Expectativa:** Download chamado com opções corretas

#### INT-11: DownloadWithTimeRangeAsync - fluxo completo
- **Setup:**
  - Mock `DownloadAsync()` sucesso
  - Mock `FfmpegHelper.ExecuteFfmpeg()` sucesso
  - Mock `File.Delete()` para cleanup
- **Expectativa:** Download + corte + limpeza temp

#### INT-12: DownloadWithTimeRangeAsync - falha no FFmpeg
- **Setup:**
  - Mock `FfmpegHelper.ExecuteFfmpeg()` lança exceção
- **Expectativa:** Exceção propagada, temp deletado no finally

**Mocks Necessários:**
```csharp
// YoutubeDL
Mock<YoutubeDL> mockYtdl = new Mock<YoutubeDL>();
mockYtdl.Setup(x => x.RunVideoDataFetch(It.IsAny<string>()))
        .ReturnsAsync(new VideoData(...));
mockYtdl.Setup(x => x.DownloadVideoAsync(
        It.IsAny<string>(), 
        It.IsAny<string>(),
        It.IsAny<DownloadOptions>(),
        It.IsAny<IProgress<DownloadProgress>>()))
        .Returns(Task.CompletedTask);

// File
Mock<IFileService> mockFile;
mockFile.Setup(x => x.Exists(It.IsAny<string>()))
        .Returns(false);

// HttpClient (para update)
Mock<HttpClient> mockHttp;
mockHttp.Setup(x => x.GetStringAsync(It.IsAny<string>()))
        .ReturnsAsync("{\"tag_name\": \"2026.01.30\"}");

// Process (para chmod)
Mock<IProcessService> mockProcess;
```

**Cobertura Esperada:** 90%+

---

### 2.2 Program.cs (50 linhas)

**Cenários de Teste:**

#### E2E-01: Fluxo completo com sucesso
- **Setup:**
  - Mock `YtDlpHelper.GetVideoTitleAsync()` retorna título
  - Mock `YtDlpHelper.DownloadWithTimeRangeAsync()` sucesso
  - Mock Console output
- **Input:** URL válida, tempos válidos (00:00:10 - 00:01:00)
- **Expectativa:** Download executado, mensagem final exibida

#### E2E-02: Exceção no download
- **Setup:**
  - Mock `DownloadWithTimeRangeAsync()` lança exceção
- **Expectativa:** Exceção capturada e relançada, mensagem de erro

#### E2E-03: Sanitização de título
- **Input:** Título com acentos e caracteres especiais
- **Expectativa:** Nome de arquivo sanitizado (sem acentos, apenas alphanum)

**Mocks Necessários:**
```csharp
// YtDlpHelper
Mock<YtDlpHelper> mockYtdl = new Mock<YtDlpHelper>() { CallBase = true };
mockYtdl.Setup(x => x.GetVideoTitleAsync(It.IsAny<string>()))
        .ReturnsAsync("Video Teste");
mockYtdl.Setup(x => x.DownloadWithTimeRangeAsync(
        It.IsAny<string>(),
        It.IsAny<string>(),
        It.IsAny<string>(),
        It.IsAny<string>(),
        It.IsAny<IProgress<DownloadProgress>>()))
        .Returns(Task.CompletedTask);
```

**Cobertura Esperada:** 95%+

---

### 2.2 Menu.cs (24 linhas)

**Cenários de Teste:**

#### UNIT-01: Input válido completo
- **Setup:** Mock Console.ReadLine() retornando valores válidos
- **Input:**
  - URL: "https://youtube.com/watch?v=abc123"
  - Start: "00:00:10"
  - End: "00:01:00"
- **Expectativa:** Propriedades Url, Start, End preenchidas corretamente

#### UNIT-02: URL vazia lança exceção
- **Setup:** Console.ReadLine() retorna string.Empty
- **Expectativa:** `InvalidOperationException` com mensagem "A url não pode ser vazia"

#### UNIT-03: URL null lança exceção
- **Setup:** Console.ReadLine() retorna null
- **Expectativa:** `InvalidOperationException`

#### UNIT-04: Start time vazio lança exceção
- **Setup:** Segunda chamada retorna string.Empty
- **Expectativa:** Exceção com mensagem sobre tempo de início

#### UNIT-05: End time vazio lança exceção
- **Setup:** Terceira chamada retorna string.Empty
- **Expectativa:** Exceção com mensagem sobre tempo de fim

**Mocks Necessários:**
```csharp
// Console precisa ser mockado via TextWriter customizado
using var sw = new StringWriter();
Console.SetOut(sw);
Console.SetIn(new StringReader("url\n00:00:10\n00:01:00\n"));
```

**Cobertura Esperada:** 100% (todas as branches)

---

### 2.3 FfmpegHelper.cs (162 linhas)

**Cenários de Teste:**

#### INT-01: FFmpeg encontrado no AppData (Windows)
- **Setup:**
  - Mock `Environment.GetFolderPath` retornando path AppData
  - Mock `File.Exists` retornando true para AppData/ffmpeg.exe
- **Expectativa:** `GetFfmpegPath()` retorna path completo do AppData

#### INT-02: FFmpeg encontrado no PATH
- **Setup:**
  - Mock `File.Exists` retornando false para AppData
  - Mock `Environment.GetEnvironmentVariable("PATH")` retornando "/usr/bin:/usr/local/bin"
  - Mock `File.Exists` retornando true para "/usr/bin/ffmpeg"
- **Expectativa:** Retorna path do sistema

#### INT-03: FFmpeg não encontrado lança exceção
- **Setup:** Todos os `File.Exists` retornam false
- **Expectativa:** `Exception` com mensagem "Não foi possível encontrar o FFmpeg"

#### INT-04: FFprobe encontrado no AppData
- **Setup:** Similar ao INT-01 mas para ffprobe
- **Expectativa:** `GetFfprobePath()` retorna path correto

#### INT-05: FFprobe não encontrado lança exceção
- **Setup:** Todos os `File.Exists` retornam false para ffprobe
- **Expectativa:** `Exception` para FFprobe

#### INT-06: ExecuteFfmpeg com sucesso (regex completo)
- **Setup:**
  - Mock `Process.Start()` retornando Process fake
  - Process fake emite stderr com Duration e time progress
- **Input stderr:**
  ```
  Duration: 00:01:30.00
  frame=  123 fps= 30 q=28.0 size=    1234kB time=00:00:45.00 bitrate=1234.5kbits/s speed=1.2x
  ```
- **Expectativa:** ProgressBar.Report() chamado com 50% (45/90 segundos)

#### INT-07: ExecuteFfmpeg sem Duration no stderr
- **Setup:** stderr não contém "Duration"
- **Expectativa:** ProgressBar não atualizado (não divide por zero)

#### INT-08: ExecuteFfmpeg sem time no stderr
- **Setup:** stderr contém Duration mas não "time="
- **Expectativa:** ProgressBar.Report() nunca chamado

#### INT-09: ExecuteFfmpeg lança exceção
- **Setup:** Process.Start() lança exceção
- **Expectativa:** Exceção capturada, escrita no Console, relançada

**Mocks Necessários:**
```csharp
// Environment
Mock<EnvironmentWrapper> mockEnv
mockEnv.Setup(x => x.GetFolderPath(It.IsAny<SpecialFolder>()))
       .Returns("/mock/appdata");
mockEnv.Setup(x => x.GetEnvironmentVariable("PATH"))
       .Returns("/usr/bin:/usr/local/bin");

// File
Mock<IFileWrapper> mockFile
mockFile.Setup(x => x.Exists(It.IsAny<string>()))
        .Returns(false);

// Process (precisa de wrapper ou usar System.Diagnostics real)
Mock<IProcessWrapper> mockProcess
mockProcess.Setup(x => x.Start(It.IsAny<ProcessStartInfo>()))
           .Returns(mockProcess.Object);

// Regex parsing pode ser testado sem mock - é parsing de strings
```

**Refatoração Necessária:**
Criar wrappers para:
- `Environment` → `IEnvironmentService`
- `File` → `IFileService`
- `Process` → `IProcessService`

**Cobertura Esperada:** 90%+

---

### 2.4 TimeHelper.cs (29 linhas)

**Cenários de Teste:**

#### UNIT-01: GetStartSeconds formato válido hh:mm:ss
- **Input:** "00:05:30"
- **Expectativa:** 330 segundos

#### UNIT-02: GetStartSeconds com horas > 0
- **Input:** "01:10:45"
- **Expectativa:** 4245 segundos

#### UNIT-03: GetStartSeconds formato inválido lança exceção
- **Input:** "05:30" (faltando horas)
- **Expectativa:** `FormatException`

#### UNIT-04: GetEndSeconds formato válido
- **Input:** "00:10:00"
- **Expectativa:** 600 segundos

#### UNIT-05: GetEndSeconds formato inválido lança exceção
- **Input:** "invalid"
- **Expectativa:** `FormatException`

#### UNIT-06: GetTimeDiff cálculo correto
- **Input:** start="00:00:00", end="00:05:00"
- **Expectativa:** 300 segundos

#### UNIT-07: GetTimeDiff com carry
- **Input:** start="00:58:30", end="01:03:45"
- **Expectativa:** 315 segundos

#### UNIT-08: GetTimeDiff resultado negativo
- **Input:** start="00:10:00", end="00:05:00"
- **Expectativa:** -300 (deveria validar?)

**Cobertura Esperada:** 100%

---

### 2.5 TitleHelper.cs (22 linhas)

**Cenários de Teste:**

#### UNIT-01: Título com acentos
- **Input:** "Vídeo Incrível de Teste"
- **Expectativa:** "Video Incrivel de Teste"

#### UNIT-02: Título com caracteres especiais
- **Input:** "Meu Vídeo @2024! #cool"
- **Expectativa:** "Meu Video 2024 cool"

#### UNIT-03: Título com emojis
- **Input:** "Vídeo 🎥 de Teste"
- **Expectativa:** "Video  de Teste" (emoji removido)

#### UNIT-04: Título apenas números
- **Input:** "12345"
- **Expectativa:** "12345"

#### UNIT-05: Título vazio
- **Input:** ""
- **Expectativa:** ""

#### UNIT-06: Título com espaços extras
- **Input:** "  Vídeo  Com  Espaços  "
- **Expectativa:** "Video Com Espacos" (após trim)

#### UNIT-07: Título com caracteres Unicode variados
- **Input:** "日本語 Vídeo العربية"
- **Expectativa:** Apenas caracteres latinos preservados

#### UNIT-08: Título null (se aplicável)
- **Input:** null
- **Expectativa:** ArgumentException ou string vazia

**Cobertura Esperada:** 100%

---

### 2.6 ProgressBar.cs (90 linhas)

**Cenários de Teste:**

#### INT-01: Report com valor válido
- **Setup:** Mock Timer e Console
- **Action:** `progressBar.Report(50)`
- **Expectativa:** `_currentProgress` = 50, Timer disparado

#### INT-02: Report com valor > 100 é clamped
- **Input:** 150
- **Expectativa:** `_currentProgress` = 100

#### INT-03: Report com valor < 0 é clamped
- **Input:** -10
- **Expectativa:** `_currentProgress` = 0

#### INT-04: Report múltiplas vezes
- **Action:** Report(0), Report(50), Report(100)
- **Expectativa:** TimerHandler chamado a cada vez

#### INT-05: Dispose para atualizações
- **Action:** Dispose() seguido de Report(50)
- **Expectativa:** TimerHandler não executa quando _disposed=true

#### INT-06: Animação de caracteres
- **Setup:** Criar ProgressBar, aguardar 4 ticks do timer
- **Expectativa:** Caracteres | / - \ em sequência

#### INT-07: Console.IsOutputRedirected false
- **Setup:** Console não redirecionado
- **Expectativa:** Timer iniciado no construtor

#### INT-08: Console.IsOutputRedirected true
- **Setup:** Mock Console.IsOutputRedirected = true
- **Expectativa:** Timer NÃO iniciado

**Mocks Necessários:**
```csharp
// Timer
Mock<Timer> mockTimer;

// Console (precisa de wrapper para Console.IsOutputRedirected)
Mock<IConsoleService> mockConsole;
mockConsole.Setup(x => x.IsOutputRedirected).Returns(true);

// Console cursor (mais complexo, pode precisar de wrapper também)
```

**Refatoração Necessária:**
- Criar `IConsoleService` para `IsOutputRedirected`, `CursorLeft`, `CursorTop`, `Write()`
- Injetar `IConsoleService` no construtor de ProgressBar

**Cobertura Esperada:** 90%+

---

## 3. Estratégia de Mocking

### 3.1 Interfaces para Criar (Refatoração)

```csharp
// IEnvironmentService.cs
public interface IEnvironmentService
{
    string GetFolderPath(Environment.SpecialFolder folder);
    string? GetEnvironmentVariable(string variable);
}

// IFileService.cs
public interface IFileService
{
    bool Exists(string path);
}

// IProcessService.cs
public interface IProcessService
{
    IProcessWrapper Start(ProcessStartInfo startInfo);
}

// IConsoleService.cs
public interface IConsoleService
{
    bool IsOutputRedirected { get; }
    int CursorLeft { get; set; }
    int CursorTop { get; set; }
    void Write(string value);
}
```

### 3.2 Classes Existentes que Podem ser Mockadas

- `YoutubeDL` - wrapper do YoutubeDLSharp, métodos virtuais disponíveis
- `HttpClient` - usado para auto-update do yt-dlp
- `Stream` - para simular arquivos

### 3.3 Classes que NÃO Precisam de Mock (testáveis diretamente)

- `TimeHelper` - métodos estáticos puros
- `TitleHelper` - métodos estáticos puros

---

## 4. Matriz de Cobertura Alvo

| Classe | Linhas | Branches | Cobertura Alvo |
|--------|--------|----------|----------------|
| YtDlpHelper.cs | 320 | 25 | 90% |
| Program.cs | 50 | 8 | 95% |
| Menu.cs | 20 | 10 | 100% |
| FfmpegHelper.cs | 140 | 18 | 90% |
| TimeHelper.cs | 25 | 6 | 100% |
| TitleHelper.cs | 18 | 4 | 100% |
| ProgressBar.cs | 75 | 8 | 90% |
| **TOTAL** | **648** | **79** | **92%** |

---

## 5. Plano de Implementação

### Fase 1: Setup e Testes Simples (Dia 1)
1. Criar projeto de testes
2. Adicionar pacotes (xUnit, Moq, FluentAssertions, coverlet)
3. Criar estrutura de diretórios
4. Implementar testes de TimeHelper (mais simples, sem refatoração)
5. Implementar testes de TitleHelper
6. Configurar coverlet para medir cobertura

### Fase 2: Testes de Menu (Dia 2)
1. Criar helper para Console mocking
2. Implementar testes de Menu
3. Validar cobertura 100%

### Fase 3: Refatoração e Testes de ProgressBar (Dia 3)
1. Criar IConsoleService
2. Refatorar ProgressBar para usar IConsoleService
3. Implementar testes de ProgressBar
4. Validar cobertura 90%+

### Fase 4: Refatoração e Testes de FfmpegHelper (Dia 4)
1. Criar IEnvironmentService, IFileService, IProcessService
2. Refatorar FfmpegHelper para usar interfaces
3. Implementar testes de descoberta de FFmpeg
4. Implementar testes de ExecuteFfmpeg com process fake
5. Validar cobertura 90%+

### Fase 5: Testes de YtDlpHelper (Dia 5)
1. Criar wrappers necessários (IFileService, IHttpClientService)
2. Refatorar YtDlpHelper para injetar dependências
3. Implementar testes de path resolution (4 caminhos)
4. Implementar testes de auto-update
5. Implementar testes de download e título
6. Validar cobertura 90%+

### Fase 6: Testes E2E de Program (Dia 6)
1. Criar mocks para YtDlpHelper
2. Implementar testes E2E com todos os mocks
3. Testar cenários de exceção
4. Testar limpeza de arquivos temp
5. Validar cobertura 95%+

### Fase 7: Integração e Validação (Dia 7)
1. Executar todos os testes
2. Validar cobertura global >90%
3. Adicionar testes para branches faltantes
4. Documentar casos não testados (justificativa)
5. Configurar CI para rodar testes automaticamente

---

## 6. Configuração de Cobertura

### 6.1 coverlet.runsettings

```xml
<?xml version="1.0" encoding="utf-8" ?>
<RunSettings>
  <DataCollectionRunSettings>
    <DataCollectors>
      <DataCollector friendlyName="XPlat Code Coverage">
        <Configuration>
          <Format>opencover</Format>
          <Exclude>[cutube.tests]*,[*.Tests]*</Exclude>
          <ExcludeByAttribute>Obsolete,GeneratedCode,CompilerGenerated</ExcludeByAttribute>
        </Configuration>
      </DataCollector>
    </DataCollectors>
  </DataCollectionRunSettings>
</RunSettings>
```

### 6.2 Comando para Gerar Relatório

```bash
dotnet test --collect:"XPlat Code Coverage" --results-directory ./coverage
dotnet tool run reportgenerator -reports:**/coverage.cobertura.xml -targetdir:./coverage-report
```

### 6.3 Metas de Qualidade

- **Cobertura de Linhas:** ≥90%
- **Cobertura de Branches:** ≥85%
- **Zero testes flaky**
- **Todos os testes passam em <5 segundos**

---

## 7. Exemplos de Implementação

### 7.1 Teste de TimeHelper

```csharp
public class TimeHelperTests
{
    [Theory]
    [InlineData("00:00:00", 0)]
    [InlineData("00:01:00", 60)]
    [InlineData("00:05:30", 330)]
    [InlineData("01:00:00", 3600)]
    [InlineData("02:30:45", 9045)]
    public void GetStartSeconds_ValidFormat_ReturnsCorrectSeconds(string time, int expected)
    {
        // Act
        var result = TimeHelper.GetStartSeconds(time);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("00:30")]      // formato incompleto
    [InlineData("invalid")]
    [InlineData("25:00:00")]   // horas inválidas
    public void GetStartSeconds_InvalidFormat_ThrowsFormatException(string time)
    {
        // Act
        Action act = () => TimeHelper.GetStartSeconds(time);

        // Assert
        act.Should().Throw<FormatException>();
    }
}
```

### 7.2 Teste de TitleHelper

```csharp
public class TitleHelperTests
{
    [Theory]
    [InlineData("Vídeo Simples", "Video Simples")]
    [InlineData("Título com Acentuação", "Titulo com Acentuacao")]
    [InlineData("Teste @2024! #Hash", "Teste 2024 Hash")]
    [InlineData("日本語 Test", " Test")]
    public void FormatTitle_RemovesAccentsAndSpecialChars(string input, string expected)
    {
        // Act
        var result = TitleHelper.FormatTitle(input);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void FormatTitle_EmptyString_ReturnsEmpty()
    {
        // Act
        var result = TitleHelper.FormatTitle("");

        // Assert
        result.Should().Be("");
    }
}
```

### 7.3 Teste de Menu com Console Mock

```csharp
public class MenuTests
{
    [Fact]
    public void Show_ValidInput_SetsPropertiesCorrectly()
    {
        // Arrange
        var input = "https://youtube.com/watch?v=abc123\n00:00:10\n00:01:00\n";
        using var sw = new StringWriter();
        using var sr = new StringReader(input);
        Console.SetOut(sw);
        Console.SetIn(sr);

        // Act
        Menu.Show();

        // Assert
        Menu.Url.Should().Be("https://youtube.com/watch?v=abc123");
        Menu.Start.Should().Be("00:00:10");
        Menu.End.Should().Be("00:01:00");
    }

    [Fact]
    public void Show_EmptyUrl_ThrowsInvalidOperationException()
    {
        // Arrange
        using var sr = new StringReader("\n00:00:10\n00:01:00\n");
        Console.SetIn(sr);

        // Act
        Action act = () => Menu.Show();

        // Assert
        act.Should().Throw<InvalidOperationException>()
           .WithMessage("*url não pode ser vazia*");
    }
}
 ```

 ### 7.4 Teste de YtDlpHelper

 ```csharp
 public class YtDlpHelperTests
 {
     [Fact]
     public async Task GetVideoTitleAsync_ValidUrl_ReturnsSanitizedTitle()
     {
         // Arrange
         var mockYtdl = new Mock<YoutubeDL>();
         var expectedData = new Dictionary<string, string>
         {
             ["title"] = "Vídeo Incrível de Teste @2024!"
         };
         
         mockYtdl.Setup(x => x.RunVideoDataFetch(It.IsAny<string>()))
                .ReturnsAsync(new VideoDownloadResult(expectedData));
         
         var helper = new YtDlpHelper(mockYtdl.Object);

         // Act
         var result = await helper.GetVideoTitleAsync("https://youtu.be/dQw4w9WgXcQ");

         // Assert
         result.Should().Be("Video Incrivel de Teste 2024");
     }

     [Fact]
     public void ResolveYtDlpPath_RecentUserVersion_ReturnsUserPath()
     {
         // Arrange
         var mockFile = new Mock<IFileService>();
         mockFile.Setup(x => x.Exists(It.IsRegex(@".*\.local.*yt-dlp")))
                .Returns(true);
         
         var helper = new YtDlpHelper(mockFile.Object);

         // Act
         var result = helper.ResolveYtDlpPath();

         // Assert
         result.Should().Contain(".local");
     }

     [Fact]
     public async Task UpdateYtDlpIfNeeded_VersionMismatch_DownloadsUpdate()
     {
         // Arrange
         var mockYtdl = new Mock<YoutubeDL>();
         var mockHttp = new Mock<IHttpClientService>();
         var mockFile = new Mock<IFileService>();
         
         mockHttp.Setup(x => x.GetStringAsync(It.IsAny<string>()))
                .ReturnsAsync("{\"tag_name\": \"2026.01.30\"}");
         
         var helper = new YtDlpHelper(mockYtdl.Object, mockHttp.Object, mockFile.Object);

         // Act
         await helper.UpdateYtDlpIfNeeded();

         // Assert
         mockHttp.Verify(x => x.GetByteArrayAsync(It.IsAny<string>()), Times.Once);
     }
 }
 ```

 ---

 ## 8. Notas sobre Migração yt-dlp

**Contexto:** Este plano de testes foi atualizado para refletir a migração de YoutubeExplode para yt-dlp + YoutubeDLSharp (concluída em Jan/2026).

**Mudanças na arquitetura:**
- `YoutubeClient` foi substituído por `YtDlpHelper`
- Download agora usa yt-dlp via YoutubeDLSharp
- Auto-update do yt-dlp é executado em background
- Path resolution prioriza: user > bundle > system > download

**Impacto nos testes:**
- Removida necessidade de mockar `YoutubeClient` e streams
- Adicionados mocks para `YoutubeDL`, `HttpClient` (update), `File` (path resolution)
- Nova classe `YtDlpHelper.cs` (363 linhas) adicionada à matriz de cobertura
- Testes de Program.cs simplificados (menos complexidade de streams)

---

## 9. Checklist Final

- [ ] Projeto de testes criado
- [ ] xUnit, Moq, FluentAssertions instalados
- [ ] Coverlet configurado
- [ ] TimeHelper: 100% cobertura
- [ ] TitleHelper: 100% cobertura
- [ ] Menu: 100% cobertura
- [ ] ProgressBar: 90%+ cobertura (com refatoração)
- [ ] FfmpegHelper: 90%+ cobertura (com refatoração)
- [ ] YtDlpHelper: 90%+ cobertura (com refatoração)
- [ ] Program: 95%+ cobertura
- [ ] Cobertura global ≥90%
- [ ] Todos os testes passando
- [ ] CI configurado
- [ ] Documentação de testes criada

---

**Status:** Plano Completo (Atualizado para yt-dlp)
**Estimativa:** 7 dias de desenvolvimento
**Cobertura Alvo:** 92% global (648 de ~700 linhas)
**Data Atualização:** 30/01/2026
