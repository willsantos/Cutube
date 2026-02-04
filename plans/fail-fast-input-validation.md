# Plano de Implementação - Fail Fast Input Validation

## Objetivo
Implementar validação de inputs com **fail fast imediato** - cada input é validado logo após ser coletado, com feedback instantâneo e limite de 3 tentativas antes de cancelar a operação.

## Problema Atual
Os 6 inputs são coletados primeiro em `Menu.Show()`, e só depois validados em `TryValidateInput()`. Isso causa uma experiência ruim onde o usuário pode preencher tudo incorretamente e só descobrir no final.

## Solução
Validar cada input **imediatamente** após a coleta, com:
- Mensagens de erro detalhadas (opção B)
- Limite de 3 tentativas por input
- Métodos estáticos em `InteractiveValidator`

## Branch
`feature/fail-fast-input-validation`

---

## Commits Planejados

### 1. `feat: add InteractiveValidator with retry logic`
**Arquivo:** `cutube/Validation/InteractiveValidator.cs` (NOVO)

**Classe estática com métodos:**

```csharp
public static class InteractiveValidator
{
    private const int MaxAttempts = 3;
    
    public static string GetValidUrl(IConsoleService console, IErrorHandler errorHandler)
    public static string GetValidStartTime(IConsoleService console, IErrorHandler errorHandler)
    public static string GetValidEndTime(IConsoleService console, IErrorHandler errorHandler, string startTime)
    public static string GetValidFileName(IConsoleService console, IErrorHandler errorHandler)
    public static string GetValidDirectory(IConsoleService console, IErrorHandler errorHandler, IFileService fileService)
}
```

**Comportamento padrão de cada método:**
1. Exibe prompt inicial
2. Lê input do usuário
3. Valida usando `ValidationHelper`
4. Se válido → retorna valor
5. Se inválido → mostra erro detalhado e repete (máx 3 tentativas)
6. Após 3 falhas → lança exceção e cancela operação

**Exemplo de implementação (GetValidUrl):**

```csharp
public static string GetValidUrl(IConsoleService console, IErrorHandler errorHandler)
{
    for (int attempt = 1; attempt <= MaxAttempts; attempt++)
    {
        console.Write("URL do vídeo: ");
        string? input = console.ReadLine();
        
        if (string.IsNullOrWhiteSpace(input))
        {
            ShowError(console, $"❌ URL não pode ser vazia. Use uma URL do YouTube (ex: https://youtube.com/watch?v=... ou https://youtu.be/...)", attempt);
            continue;
        }
        
        try
        {
            ValidationHelper.ValidateUrl(input);
            return input;
        }
        catch (Exception ex)
        {
            string message = errorHandler.GetUserFriendlyMessage(ex);
            ShowError(console, message, attempt);
        }
    }
    
    throw new InvalidOperationException("❌ Máximo de tentativas atingido para URL. Operação cancelada.");
}

private static void ShowError(IConsoleService console, string message, int attempt)
{
    console.WriteLine(message);
    if (attempt < MaxAttempts)
    {
        console.WriteLine($"Tentativa {attempt} de {MaxAttempts}. Tente novamente.");
    }
}
```

**Mensagens de erro detalhadas (opção B):**

| Input | Mensagem de Erro |
|-------|------------------|
| **URL inválida** | `"❌ URL inválida. Use uma URL do YouTube (youtube.com ou youtu.be) que comece com http:// ou https://\nURL: "` |
| **Start inválido** | `"❌ Tempo de início inválido. Use formatos como: 90s, 1:30, 1h30m. O valor deve ser maior que zero.\nInício: "` |
| **End inválido** | `"❌ Tempo de fim inválido. Deve ser maior que o tempo de início ({start}s). Use formatos como: 120s, 2:00, 2m.\nFim: "` |
| **Nome inválido** | `"❌ Nome do arquivo inválido. Não use caracteres especiais (<, >:, \", \|, ?, *, /, \\) ou caminhos de diretório.\nNome do arquivo (ou Enter para pular): "` |
| **Diretório inválido** | `"❌ Diretório inválido ou sem permissão de escrita.\nDiretório de saída (ou Enter para usar atual): "` |

---

### 2. `refactor: integrate immediate validation into Menu`
**Arquivo:** `cutube/Menu.cs` (MODIFICAR)

**Mudanças principais:**

**ANTES (atual):**
```csharp
public static void Show()
{
    Console.Write("URL do vídeo: ");
    Url = Console.ReadLine() ?? throw new InvalidOperationException("URL é obrigatória");
    
    Console.Write("Tempo de início (ex: 90s, 1:30, 1h30m): ");
    Start = Console.ReadLine() ?? throw new InvalidOperationException("Início é obrigatório");
    
    Console.Write("Tempo de fim (ex: 120s, 2:00, 2m): ");
    End = Console.ReadLine() ?? throw new InvalidOperationException("Fim é obrigatório");
    
    // ... coleta demais inputs SEM validação
}
```

**DEPOIS (com validação imediata):**
```csharp
public static Result Show(IConsoleService console, IFileService fileService, IErrorHandler errorHandler)
{
    console.WriteLine("\n=== Download de Cortes do YouTube ===\n");
    
    // 1. URL com validação imediata
    Url = InteractiveValidator.GetValidUrl(console, errorHandler);
    
    // 2. Start Time com validação imediata
    Start = InteractiveValidator.GetValidStartTime(console, errorHandler);
    
    // 3. End Time com validação imediata (depende do Start)
    End = InteractiveValidator.GetValidEndTime(console, errorHandler, Start);
    
    // 4. Filename com validação imediata (opcional)
    CustomFileName = InteractiveValidator.GetValidFileName(console, errorHandler);
    
    // 5. Directory com validação imediata (opcional)
    OutputDirectory = InteractiveValidator.GetValidDirectory(console, errorHandler, fileService);
    
    // 6. Audio Only (binary choice, não precisa validação)
    AudioOnly = GetAudioOnlyChoice(console);
    
    return Result.Success();
}

private static bool GetAudioOnlyChoice(IConsoleService console)
{
    console.Write("\nTipo de download:\n1 - Vídeo\n2 - Áudio apenas\nEscolha: ");
    string? choice = console.ReadLine();
    return choice?.Trim() == "2";
}
```

**Assinatura mudada:** `void Show()` → `Result Show(IConsoleService, IFileService, IErrorHandler)`

---

### 3. `refactor: update IMenuService interface`
**Arquivo:** `cutube/IMenuService.cs` (MODIFICAR)

**Mudança de assinatura:**

```csharp
// ANTES
void Show();

// DEPOIS
Result Show(IConsoleService console, IFileService fileService, IErrorHandler errorHandler);
```

---

### 4. `refactor: remove redundant validation from ProgramWorkflow`
**Arquivo:** `cutube/ProgramWorkflow.cs` (MODIFICAR)

**Remover método:** `TryValidateInput()` (linhas 91-114)

**ANTES:**
```csharp
public async Task<Result> RunAsync()
{
    _menu.Show();
    
    var menuResult = TryValidateInput();
    if (!menuResult.IsSuccess) return menuResult;
    
    // ... resto do código
}

private Result TryValidateInput()
{
    try
    {
        ValidationHelper.ValidateUrl(_menu.Url);
        ValidationHelper.ValidateTimeRange(_menu.Start, _menu.End);
        // ...
        return Result.Success();
    }
    catch (Exception ex)
    {
        // error handling...
    }
}
```

**DEPOIS:**
```csharp
public async Task<Result> RunAsync()
{
    // Menu.Show() agora retorna Result com validação embutida
    var menuResult = _menu.Show(_consoleService, _fileService, _errorHandler);
    if (!menuResult.IsSuccess) return menuResult;
    
    // ... resto do código continua igual
}
```

---

### 5. `refactor: update MenuService implementation`
**Arquivo:** `cutube/MenuService.cs` (MODIFICAR - se existir, senão ignorar)

**Atualizar implementação de `IMenuService` para nova assinatura.**

---

### 6. `test: add InteractiveValidatorTests`
**Arquivo:** `Cutube.Tests/Unit/Validation/InteractiveValidatorTests.cs` (NOVO)

**Casos de teste:**

```csharp
public class InteractiveValidatorTests
{
    private Mock<IConsoleService> _console;
    private Mock<IErrorHandler> _errorHandler;
    private Mock<IFileService> _fileService;
    
    [SetUp]
    public void Setup()
    {
        _console = new Mock<IConsoleService>();
        _errorHandler = new Mock<IErrorHandler>();
        _fileService = new Mock<IFileService>();
        
        // Configura ErrorHandler para retornar mensagem original
        _errorHandler.Setup(h => h.GetUserFriendlyMessage(It.IsAny<Exception>()))
            .Returns<Exception>(ex => ex.Message);
    }
    
    // URL Tests
    [Test]
    public void GetValidUrl_ValidUrl_ReturnsUrl()
    {
        _console.SetupSequence(c => c.ReadLine())
            .Returns("https://youtube.com/watch?v=abc123");
        
        var result = InteractiveValidator.GetValidUrl(_console.Object, _errorHandler.Object);
        
        Assert.That(result, Is.EqualTo("https://youtube.com/watch?v=abc123"));
        _console.Verify(c => c.Write("URL do vídeo: "), Times.Once);
        _console.Verify(c => c.WriteLine(It.IsAny<string>()), Times.Never); // Sem erros
    }
    
    [Test]
    public void GetValidUrl_InvalidUrl_PromptsAgain_SucceedsOnSecondAttempt()
    {
        _console.SetupSequence(c => c.ReadLine())
            .Returns("invalid-url")
            .Returns("https://youtu.be/abc123");
        
        var result = InteractiveValidator.GetValidUrl(_console.Object, _errorHandler.Object);
        
        Assert.That(result, Is.EqualTo("https://youtu.be/abc123"));
        _console.Verify(c => c.Write("URL do vídeo: "), Times.Exactly(2));
        _console.Verify(c => c.WriteLine(It.Is<string>(s => s.Contains("❌"))), Times.Once);
    }
    
    [Test]
    public void GetValidUrl_EmptyInput_ThrowsAfter3Attempts()
    {
        _console.SetupSequence(c => c.ReadLine())
            .Returns("")
            .Returns("   ")
            .Returns("");
        
        var ex = Assert.Throws<InvalidOperationException>(() =>
            InteractiveValidator.GetValidUrl(_console.Object, _errorHandler.Object)
        );
        
        Assert.That(ex.Message, Does.Contain("Máximo de tentativas atingido"));
        _console.Verify(c => c.Write("URL do vídeo: "), Times.Exactly(3));
        _console.Verify(c => c.WriteLine(It.Is<string>(s => s.Contains("❌"))), Times.AtLeast(2));
    }
    
    // StartTime Tests
    [Test]
    public void GetValidStartTime_ValidTime_ReturnsTime()
    {
        _console.SetupSequence(c => c.ReadLine())
            .Returns("90s");
        
        var result = InteractiveValidator.GetValidStartTime(_console.Object, _errorHandler.Object);
        
        Assert.That(result, Is.EqualTo("90s"));
    }
    
    [Test]
    public void GetValidStartTime_InvalidFormat_PromptsAgain()
    {
        _console.SetupSequence(c => c.ReadLine())
            .Returns("invalid")
            .Returns("90s");
        
        var result = InteractiveValidator.GetValidStartTime(_console.Object, _errorHandler.Object);
        
        Assert.That(result, Is.EqualTo("90s"));
        _console.Verify(c => c.WriteLine(It.Is<string>(s => s.Contains("❌"))), Times.Once);
    }
    
    [Test]
    public void GetValidStartTime_ZeroOrNegative_ThrowsAfter3Attempts()
    {
        _console.SetupSequence(c => c.ReadLine())
            .Returns("0")
            .Returns("-10")
            .Returns("0");
        
        var ex = Assert.Throws<InvalidOperationException>(() =>
            InteractiveValidator.GetValidStartTime(_console.Object, _errorHandler.Object)
        );
        
        Assert.That(ex.Message, Does.Contain("Máximo de tentativas atingido"));
    }
    
    // EndTime Tests
    [Test]
    public void GetValidEndTime_ValidTimeGreaterThanStart_ReturnsTime()
    {
        _console.Setup(c => c.ReadLine()).Returns("120s");
        
        var result = InteractiveValidator.GetValidEndTime(_console.Object, _errorHandler.Object, "90s");
        
        Assert.That(result, Is.EqualTo("120s"));
    }
    
    [Test]
    public void GetValidEndTime_LessThanStart_PromptsAgain()
    {
        _console.SetupSequence(c => c.ReadLine())
            .Returns("60s")
            .Returns("120s");
        
        var result = InteractiveValidator.GetValidEndTime(_console.Object, _errorHandler.Object, "90s");
        
        Assert.That(result, Is.EqualTo("120s"));
        _console.Verify(c => c.WriteLine(It.Is<string>(s => s.Contains("maior que o tempo de início"))), Times.Once);
    }
    
    [Test]
    public void GetValidEndTime_EqualToStart_PromptsAgain()
    {
        _console.SetupSequence(c => c.ReadLine())
            .Returns("90s")
            .Returns("120s");
        
        var result = InteractiveValidator.GetValidEndTime(_console.Object, _errorHandler.Object, "90s");
        
        Assert.That(result, Is.EqualTo("120s"));
    }
    
    // FileName Tests
    [Test]
    public void GetValidFileName_ValidName_ReturnsName()
    {
        _console.Setup(c => c.ReadLine()).Returns("meu-video");
        
        var result = InteractiveValidator.GetValidFileName(_console.Object, _errorHandler.Object);
        
        Assert.That(result, Is.EqualTo("meu-video"));
    }
    
    [Test]
    public void GetValidFileName_Empty_ReturnsEmpty()
    {
        _console.Setup(c => c.ReadLine()).Returns("");
        
        var result = InteractiveValidator.GetValidFileName(_console.Object, _errorHandler.Object);
        
        Assert.That(result, Is.EqualTo(""));
    }
    
    [Test]
    public void GetValidFileName_InvalidChars_PromptsAgain_SkipsOnEmpty()
    {
               _console.SetupSequence(c => c.ReadLine())
            .Returns("video/invalid")
            .Returns("");
        
        var result = InteractiveValidator.GetValidFileName(_console.Object, _errorHandler.Object);
        
        Assert.That(result, Is.EqualTo(""));
        _console.Verify(c => c.WriteLine(It.Is<string>(s => s.Contains("❌"))), Times.Once);
    }
    
    [Test]
    public void GetValidFileName_InvalidChars_ThrowsAfter3Attempts()
    {
        _console.SetupSequence(c => c.ReadLine())
            .Returns("video|1")
            .Returns("video<test>")
            .Returns("video*invalid");
        
        var ex = Assert.Throws<InvalidOperationException>(() =>
            InteractiveValidator.GetValidFileName(_console.Object, _errorHandler.Object)
        );
        
        Assert.That(ex.Message, Does.Contain("Máximo de tentativas atingido"));
    }
    
    // Directory Tests
    [Test]
    public void GetValidDirectory_ValidDirectory_ReturnsPath()
    {
        _console.Setup(c => c.ReadLine()).Returns("/tmp");
        _fileService.Setup(f => f.DirectoryExists("/tmp")).Returns(true);
        _fileService.Setup(f => f.HasWritePermission("/tmp")).Returns(true);
        
        var result = InteractiveValidator.GetValidDirectory(_console.Object, _errorHandler.Object, _fileService.Object);
        
        Assert.That(result, Is.EqualTo("/tmp"));
    }
    
    [Test]
    public void GetValidDirectory_Empty_ReturnsEmpty()
    {
        _console.Setup(c => c.ReadLine()).Returns("");
        
        var result = InteractiveValidator.GetValidDirectory(_console.Object, _errorHandler.Object, _fileService.Object);
        
        Assert.That(result, Is.EqualTo(""));
    }
    
    [Test]
    public void GetValidDirectory_NotFound_PromptsAgain_SkipsOnEmpty()
    {
        _console.SetupSequence(c => c.ReadLine())
            .Returns("/nonexistent")
            .Returns("");
        _fileService.Setup(f => f.DirectoryExists("/nonexistent")).Returns(false);
        
        var result = InteractiveValidator.GetValidDirectory(_console.Object, _errorHandler.Object, _fileService.Object);
        
        Assert.That(result, Is.EqualTo(""));
        _console.Verify(c => c.WriteLine(It.Is<string>(s => s.Contains("❌"))), Times.Once);
    }
    
    [Test]
    public void GetValidDirectory_NoPermission_ThrowsAfter3Attempts()
    {
        _console.SetupSequence(c => c.ReadLine())
            .Returns("/root")
            .Returns("/restricted")
            .Returns("/protected");
        _fileService.Setup(f => f.DirectoryExists(It.IsAny<string>())).Returns(true);
        _fileService.Setup(f => f.HasWritePermission(It.IsAny<string>())).Returns(false);
        
        var ex = Assert.Throws<InvalidOperationException>(() =>
            InteractiveValidator.GetValidDirectory(_console.Object, _errorHandler.Object, _fileService.Object)
        );
        
        Assert.That(ex.Message, Does.Contain("Máximo de tentativas atingido"));
    }
}
```

**Total de testes:** ~20 testes unitários

---

### 7. `test: add E2E validation flow tests`
**Arquivo:** `Cutube.Tests/E2E/ValidationFlowTests.cs` (NOVO)

**Cenários de teste:**

```csharp
public class ValidationFlowTests
{
    // 1. URL inválida + recovery
    [Test]
    public async Task CompleteFlow_InvalidUrl_ShowsErrorAndPromptsAgain_Succeeds()
    {
        // Setup inputs: invalid URL → valid URL → rest valid
        // Assert: error message shown, operation succeeds
    }
    
    // 2. Time range inválido + recovery
    [Test]
    public async Task CompleteFlow_InvalidTimeRange_ShowsErrorAndPromptsAgain_Succeeds()
    {
        // Setup: start=0 → start=90s, end=60 → end=120s
        // Assert: error messages shown, operation succeeds
    }
    
    // 3. Inputs válidos → sucesso
    [Test]
    public async Task CompleteFlow_AllInputsValid_ProceedsToDownload()
    {
        // Setup: all valid inputs
        // Assert: no error messages, reaches download phase
    }
    
    // 4. Inputs opcionais vazios → defaults
    [Test]
    public async Task CompleteFlow_OptionalInputsEmpty_UsesDefaults()
    {
        // Setup: required valid, optional = empty
        // Assert: uses default values (video title, current dir, video mode)
    }
    
    // 5. 3 falhas → cancelamento
    [Test]
    public async Task CompleteFlow_ThreeFailures_CancelledWithMessage()
    {
        // Setup: 3 invalid URLs
        // Assert: throws exception, doesn't proceed to download
    }
}
```

---

## Estrutura de Arquivos

### Novos Arquivos:
```
cutube/Validation/
  └── InteractiveValidator.cs              # Validadores com retry integrado (NOVO)

Cutube.Tests/Unit/Validation/
  └── InteractiveValidatorTests.cs         # Testes unitários (NOVO)

Cutube.Tests/E2E/
  └── ValidationFlowTests.cs               # Testes E2E do fluxo (NOVO)
```

### Arquivos Modificados:
```
cutube/Menu.cs                             # Adicionar validação imediata
cutube/IMenuService.cs                     # Atualizar interface
cutube/ProgramWorkflow.cs                  # Remover TryValidateInput()
cutube/MenuService.cs                      # Atualizar implementação (se existir)
```

---

## Critérios de Aceite

### Funcionalidade:
- [ ] Cada input é validado imediatamente após coleta
- [ ] Mensagens de erro detalhadas (opção B)
- [ ] Limite de 3 tentativas por input
- [ ] Após 3 falhas, operação é cancelada com mensagem clara
- [ ] Inputs opcionais podem ser vazios (filename, directory)
- [ ] End time validado em relação ao start time
- [ ] Validação de diretório verifica existência e permissão

### Testes:
- [ ] `InteractiveValidatorTests.cs` com ~20 testes unitários
- [ ] `ValidationFlowTests.cs` com ~5 testes E2E
- [ ] 100% dos testes passando: `dotnet test`
- [ ] Cobertura de validação de URL, time, filename, directory
- [ ] Cobertura de cenários de retry (1, 2, 3 tentativas)

### Código:
- [ ] `InteractiveValidator` com métodos estáticos
- [ ] `Menu.Show()` retorna `Result` em vez de `void`
- [ ] `ProgramWorkflow.TryValidateInput()` removido
- [ ] Build sem warnings: `dotnet build`

---

## Benefícios

✅ **Fail fast imediato** - usuário descobre erro na hora  
✅ **Melhor UX** - não precisa preencher tudo de novo  
✅ **Feedback contextual** - erro específico para cada input  
✅ **Proteção contra loops** - limite de 3 tentativas  
✅ **Código mais limpo** - validação junto com coleta  
✅ **Mais testável** - cada validador isolado  

---

## Cenários de Teste Manual

1. **URL inválida → recovery:**
   - Entrar "not-a-url" → deve mostrar erro
   - Entrar "https://google.com" → deve mostrar erro
   - Entrar "https://youtube.com/watch?v=abc" → deve aceitar

2. **Time inválido → recovery:**
   - Entrar "0" → deve mostrar erro (deve ser > 0)
   - Entrar "90s" → deve aceitar
   - Entrar "60s" para end → deve mostrar erro (deve ser > 90)
   - Entrar "120s" → deve aceitar

3. **Nome inválido → recovery:**
   - Entrar "video/test" → deve mostrar erro
   - Entrar "" (vazio) → deve aceitar (opcional)

4. **Diretório inválido → recovery:**
   - Entrar "/nonexistent" → deve mostrar erro
   - Entrar "" (vazio) → deve aceitar (usa atual)

5. **3 falhas → cancelamento:**
   - Entrar 3 URLs inválidas → deve cancelar com mensagem

---

## Notas de Implementação

- **Ordem dos prompts:** Manter igual ao atual (URL → Start → End → Filename → Directory → Audio/Video)
- **Mensagens de erro:** Usar emojis (❌) para destaque visual
- **Counter de tentativas:** Mostrar "Tentativa X de 3" após cada falha
- **Exceção final:** Incluir nome do input que falhou na mensagem de cancelamento
- **Inputs opcionais:** Aceitar vazio como entrada válida (filename, directory)
- **Validação de end time:** Sempre receber start time como parâmetro para comparação

---

## Dependências

- ✅ Cutube-dsn.13 (Error Handler Centralizado) - **CONCLUÍDO**
- ✅ Cutube-bi2 (Validações Robustas) - **CONCLUÍDO**
- ✅ ErrorHandler com `GetUserFriendlyMessage()`
- ✅ ValidationHelper com todos os validadores
- ✅ Result pattern para error handling

Todas as dependências já estão implementadas.
