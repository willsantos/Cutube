# Plano de Implementação - Cutube-bi2: Validações Robustas

## Objetivo
Implementar validações robustas para todos os inputs do Menu, criando validações centralizadas e melhorando as mensagens de erro.

## Branch
`feature/input-validations`

## Commits Planejados

### 1. `feat: add ValidationHelper with URL validation`
**Arquivo:** `cutube/ValidationHelper.cs` (NOVO)

**Validações implementadas:**
- `IsValidUrl(string url)` - Valida URLs do YouTube
- `ValidateUrl(string url)` - Lança exceção com mensagem explicativa

**Regras de URL do YouTube:**
- Deve usar Uri.TryCreate para validação de formato
- Esquema deve ser http ou https
- Host deve ser: youtube.com, www.youtube.com, youtu.be, m.youtube.com
- Não pode ser vazia ou whitespace

**Exceções:**
- `ArgumentException` com mensagem: "❌ URL inválida. Use uma URL do YouTube (youtube.com ou youtu.be) que comece com http:// ou https://"

---

### 2. `feat: add time range and filename validations`
**Arquivo:** `cutube/ValidationHelper.cs` (continuação)

**Validações de TimeRange:**
- `IsValidTimeRange(int start, int end)` - Valida start > 0 e end > start
- `ValidateTimeRange(string start, string end)` - Usa TimeHelper.ParseToSeconds + valida range

**Regras:**
- Deve usar TimeHelper.ParseToSeconds() para conversão
- Start deve ser > 0 (não pode começar do zero)
- End deve ser > start (range positivo)
- Propaga FormatException do TimeHelper se formato inválido

**Exceções:**
- `ArgumentException` se start <= 0: "❌ Tempo de início deve ser maior que zero. Use formatos como 00:01:30, 1:30, 90s, 1h30m."
- `ArgumentException` se end <= start: "❌ Tempo de fim deve ser maior que o tempo de início. Início: {start}s, Fim: {end}s"

**Validações de FileName:**
- `IsValidFileName(string name)` - Valida caracteres e caminho
- `ValidateFileName(string name)` - Lança exceção com mensagem

**Caracteres inválidos:** `<`, `>`, `:`, `"`, `|`, `?`, `*`, `/`, `\`
**Não pode conter:** path separators (`/` ou `\`)
**Não pode ser:** vazio ou apenas whitespace

**Exceções:**
- `ArgumentException` com mensagem: "❌ Nome do arquivo inválido. Não use caracteres especiais (<, >, :, \", |, ?, *, /, \\) ou caminhos de diretório."

---

### 3. `feat: add directory validation`
**Arquivo:** `cutube/ValidationHelper.cs` (continuação)

**Validações:**
- `IsDirectoryWritable(string path, IFileService fileService)` - Valida existência e permissão
- `ValidateDirectory(string path, IFileService fileService)` - Lança exceções descritivas

**Regras:**
- Usa `IFileService.DirectoryExists()` para verificar existência
- Usa `IFileService.HasWritePermission()` para verificar permissão
- Caminho deve ser válido (Path.GetFullPath não lança exceção)

**Exceções:**
- `DirectoryNotFoundException` se não existe: "❌ Diretório não encontrado: '{path}'. Verifique se o caminho está correto ou crie o diretório antes de continuar."
- `UnauthorizedAccessException` sem permissão: "❌ Sem permissão de escrita em '{path}'. Escolha outro diretório ou verifique as permissões."

---

### 4. `feat: integrate validations into ProgramWorkflow`
**Arquivo:** `cutube/ProgramWorkflow.cs`

**Alterações:**
- Adicionar validações no início de `RunAsync()`, após `_menu.Show()`
- Manter lógica existente de criar diretório se não existir
- Propagar exceções de validação com mensagens claras

**Código:**
```csharp
public async Task RunAsync()
{
    _menu.Show();

    // Validar todos os inputs antes de processar
    ValidationHelper.ValidateUrl(_menu.Url);
    ValidationHelper.ValidateTimeRange(_menu.Start, _menu.End);
    
    if (!string.IsNullOrWhiteSpace(_menu.CustomFileName))
    {
        ValidationHelper.ValidateFileName(_menu.CustomFileName);
    }
    
    var outputDir = string.IsNullOrWhiteSpace(_menu.OutputDirectory)
        ? Directory.GetCurrentDirectory()
        : NormalizePath(_menu.OutputDirectory);
    
    ValidationHelper.ValidateDirectory(outputDir, _fileService);

    // Resto do código continua igual...
}
```

---

### 5. `feat: improve error messages in Menu`
**Arquivo:** `cutube/Menu.cs`

**Alterações:**
- Melhorar mensagens das exceções InvalidOperationException
- Adicionar emojis para melhor visualização
- Ser mais explicativo sobre formatos esperados

**Exemplos:**
```csharp
// URL
Url = Console.ReadLine() 
    ?? throw new InvalidOperationException("❌ URL não pode ser vazia. Digite uma URL válida do YouTube (ex: https://youtube.com/watch?v=... ou https://youtu.be/...)");

// Start
Start = Console.ReadLine() 
    ?? throw new InvalidOperationException("❌ Tempo de início não pode ser vazio. Use formatos como: 00:01:30, 1:30, 90s, 1h30m.");

// End
End = Console.ReadLine() 
    ?? throw new InvalidOperationException("❌ Tempo de fim não pode ser vazio. Use formatos como: 00:02:00, 2:00, 120s, 2m.");
```

---

### 6. `test: add ValidationHelperTests`
**Arquivo:** `Cutube.Tests/Unit/ValidationHelperTests.cs` (NOVO)

**Testes de URL:**
- ✅ URLs válidas do YouTube (youtube.com, youtu.be, www, m)
- ❌ URLs inválidas (vazia, não-url, ftp, other domains)
- ❌ URLs sem esquema (youtube.com/watch?v=...)

**Testes de TimeRange:**
- ✅ Ranges válidos (start < end, formatos variados)
- ❌ start = 0
- ❌ start > end
- ❌ Formatos inválidos (propaga FormatException do TimeHelper)

**Testes de FileName:**
- ✅ Nomes válidos (com espaços, números, hífens)
- ❌ Caracteres inválidos (<, >, :, ", |, ?, *, /, \)
- ❌ Com path separator
- ❌ Vazio ou whitespace
- ❌ Null (exceção diferente)

**Testes de Directory:**
- ✅ Diretório existente e writable
- ❌ Diretório não existe
- ❌ Sem permissão de escrita
- ✅ Aceita string vazia (usa diretório atual)

**Mock:**
- Usar `Mock<IFileService>` para testes de diretório

---

## Critérios de Aceite

- [ ] ValidationHelper.cs criado com todos os métodos de validação
- [ ] URL valida apenas YouTube (youtube.com, youtu.be)
- [ ] TimeRange valida formato e range (start > 0, end > start)
- [ ] FileName valida caracteres inválidos e sem path
- [ ] Directory valida existência e permissão de escrita
- [ ] ProgramWorkflow integra todas as validações
- [ ] Menu.cs tem mensagens de erro explicativas
- [ ] ValidationHelperTests.cs com cobertura completa
- [ ] 100% dos testes passando: `dotnet test`
- [ ] Build sem warnings: `dotnet build`

---

## Dependências

- ✅ Cutube-gcb (Tempo Flexível)
- ✅ Cutube-1yx (Diretório Customizado)
- ✅ Cutube-5bf (Download de Áudio MP3)
- ✅ Cutube-c2w (Cancelamento CTRL+C)
- ✅ Cutube-hxp (Nome Customizado)

Todas as dependências já estão completadas.

---

## Breaking Changes

- Exceções lançadas antes do download começar (mais cedo que antes)
- URLs de não-YouTube serão rejeitadas (antes eram aceitas)
- Nome vazio agora lança exceção (antes era ignorado)

---

## Testes Manuais Sugeridos

1. **URL inválida:** Entrar "not-a-url" → deve mostrar erro explicativo
2. **URL não-YouTube:** Entrar "https://google.com" → deve rejeitar
3. **Tempo start = 0:** Entrar "0" → deve mostrar erro
4. **Tempo start > end:** Entrar start="120", end="60" → deve mostrar erro
5. **Nome com caminho:** Entrar "../video" → deve mostrar erro
6. **Diretório sem permissão:** Entrar "/root" → deve mostrar erro
