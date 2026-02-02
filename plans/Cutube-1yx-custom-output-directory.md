# Cutube-1yx: Diretório Customizado

**Task ID:** Cutube-1yx  
**Priority:** P2  
**Type:** Feature  
**Estimate:** 90 min  
**Status:** 📋 Planning  
**Depends on:** Cutube-hxp (Nome Customizado) ✅ COMPLETED

---

## 📋 Visão Geral

**Objetivo:** Adicionar opção de escolher diretório de destino para salvar os arquivos baixados.

**Comportamento atual:** Arquivos são salvos no diretório atual (onde você executa `dotnet run`)

**Comportamento desejado:** Usuário pode especificar diretório customizado (5ª pergunta do menu)

---

## 🔧 Mudanças Necessárias

### 1. Menu.cs - Adicionar 5ª pergunta

**Arquivo:** `cutube/Menu.cs`

```csharp
// Adicionar propriedade
public static string OutputDirectory { get; private set; } = string.Empty;

// Adicionar 5ª pergunta no método Show() após CustomFileName
Console.Write("5 - Diretório de destino (opcional, Enter para usar atual): ");
var dirInput = Console.ReadLine() ?? string.Empty;
if (!string.IsNullOrWhiteSpace(dirInput))
{
    OutputDirectory = dirInput.Trim();
}
```

**Validações:**
- Aceitar caminho vazio (usa diretório atual)
- Remover espaços em branco com `.Trim()`
- Não validar ainda (validação em ProgramWorkflow)

---

### 2. IMenuService.cs - Adicionar getter

**Arquivo:** `cutube/IMenuService.cs`

```csharp
public interface IMenuService
{
    void Show();
    string Url { get; }
    string Start { get; }
    string End { get; }
    string CustomFileName { get; }
    string OutputDirectory { get; }  // ← NOVO
}

[ExcludeFromCodeCoverage]
public class MenuService : IMenuService
{
    // ... outros métodos ...

    public string OutputDirectory => Menu.OutputDirectory;  // ← NOVO
}
```

---

### 3. IFileService.cs - Adicionar validações de diretório

**Arquivo:** `cutube/IFileService.cs`

```csharp
public interface IFileService
{
    bool Exists(string path);
    void Delete(string path);
    Task WriteAllBytesAsync(string path, byte[] data);
    DateTime GetLastWriteTime(string path);

    // ← NOVOS MÉTODOS
    bool DirectoryExists(string path);
    void CreateDirectory(string path);
    bool HasWritePermission(string path);
}
```

**Contrato:**
- `DirectoryExists()`: Verifica se diretório existe
- `CreateDirectory()`: Cria diretório (incluindo pais, se necessário)
- `HasWritePermission()`: Testa se há permissão de escrita

---

### 4. FileService.cs - Implementar métodos

**Arquivo:** `cutube/FileService.cs`

```csharp
public class FileService : IFileService
{
    // ... métodos existentes ...

    public bool DirectoryExists(string path) => Directory.Exists(path);

    public void CreateDirectory(string path)
    {
        Directory.CreateDirectory(path);  // Cria pais automaticamente
    }

    public bool HasWritePermission(string path)
    {
        try
        {
            // Tenta criar arquivo temporário para testar permissão
            var testFile = Path.Combine(path, Path.GetRandomFileName());
            File.WriteAllText(testFile, "test");
            File.Delete(testFile);
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
        catch (Exception)
        {
            // Outros erros (diretório não existe, etc) também retornam false
            return false;
        }
    }
}
```

**Implementação robusta:**
- `Directory.CreateDirectory()` já cria diretórios pais
- Teste de permissão usando arquivo temporário
- Tratamento de exceções apropriado

---

### 5. ProgramWorkflow.cs - Usar diretório customizado

**Arquivo:** `cutube/ProgramWorkflow.cs`

```csharp
public async Task RunAsync()
{
    _menu.Show();

    var videoUrl = _menu.Url;
    var videoStart = _menu.Start;
    var videoEnd = _menu.End;

    // ... código existente para obter título e nome do arquivo ...

    // ← NOVO: Determinar diretório de saída
    var outputDir = string.IsNullOrWhiteSpace(_menu.OutputDirectory)
        ? Directory.GetCurrentDirectory()
        : NormalizePath(_menu.OutputDirectory);

    // ← NOVO: Validar diretório
    if (!_fileService.DirectoryExists(outputDir))
    {
        _console.WriteLine($"⚠️  Diretório '{outputDir}' não existe.");
        _console.Write("Deseja criá-lo? (s/n): ");
        var response = _console.ReadLine()?.ToLower();
        if (response == "s")
        {
            _fileService.CreateDirectory(outputDir);
            _console.WriteLine($"✓ Diretório criado: {outputDir}");
        }
        else
        {
            _console.WriteLine("❌ Operação cancelada.");
            return;
        }
    }

    // ← NOVO: Validar permissões
    if (!_fileService.HasWritePermission(outputDir))
    {
        _console.WriteLine($"❌ Erro: Sem permissão de escrita em '{outputDir}'");
        return;
    }

    // ← NOVO: Combinar diretório + nome do arquivo
    var outputPath = Path.Combine(outputDir, $"{fileName}.mp4");

    // Usar outputPath no download
    await _ytdl.DownloadWithTimeRangeAsync(
        videoUrl,
        outputPath,
        videoStart,
        videoEnd,
        progress
    );

    _console.WriteLine(
        $"✓ Vídeo salvo em: {Path.GetFullPath(outputPath)}"
    );
}

// ← NOVO: Método auxiliar para normalizar caminho
private string NormalizePath(string path)
{
    // Converter caminhos relativos para absolutos
    var fullPath = Path.GetFullPath(path);

    // Remover trailing slash
    fullPath = fullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

    return fullPath;
}
```

**Fluxo de validação:**
1. Usa diretório atual se vazio
2. Normaliza caminho (relativo → absoluto, remove trailing slash)
3. Verifica se existe → oferece criar
4. Testa permissão de escrita
5. Combina com nome do arquivo

---

### 6. Testes - Adicionar cobertura em MenuTests.cs

**Arquivo:** `Cutube.Tests/Unit/MenuTests.cs`

```csharp
public class MenuTests
{
    [Fact]
    public void OutputDirectory_DeveSerVazioPorPadrao()
    {
        // Arrange & Act
        Menu.Show("url", "00:01:00", "00:02:00", "custom", "");

        // Assert
        Assert.Equal("", Menu.OutputDirectory);
    }

    [Fact]
    public void OutputDirectory_DeveAceitarCaminhoCustomizado()
    {
        // Arrange & Act
        Menu.Show("url", "00:01:00", "00:02:00", "custom", "/tmp/downloads");

        // Assert
        Assert.Equal("/tmp/downloads", Menu.OutputDirectory);
    }

    [Fact]
    public void OutputDirectory_DeveAceitarCaminhoWindows()
    {
        // Arrange & Act
        Menu.Show("url", "00:01:00", "00:02:00", "custom", @"C:\Videos");

        // Assert
        Assert.Equal(@"C:\Videos", Menu.OutputDirectory);
    }

    [Fact]
    public void OutputDirectory_DeveRemoverEspacosEmBranco()
    {
        // Arrange & Act
        Menu.Show("url", "00:01:00", "00:02:00", "custom", "  /tmp/videos  ");

        // Assert
        Assert.Equal("/tmp/videos", Menu.OutputDirectory);
    }

    [Fact]
    public void OutputDirectory_DeveAceitarCaminhoRelativo()
    {
        // Arrange & Act
        Menu.Show("url", "00:01:00", "00:02:00", "custom", "../downloads");

        // Assert
        Assert.Equal("../downloads", Menu.OutputDirectory);
    }
}
```

---

## 📁 Arquivos a Modificar

1. ✏️ `cutube/Menu.cs` - Adicionar propriedade + 5ª pergunta
2. ✏️ `cutube/IMenuService.cs` - Adicionar getter
3. ✏️ `cutube/IFileService.cs` - Adicionar métodos de diretório
4. ✏️ `cutube/FileService.cs` - Implementar métodos
5. ✏️ `cutube/ProgramWorkflow.cs` - Usar diretório customizado + validações
6. ✏️ `Cutube.Tests/Unit/MenuTests.cs` - Adicionar 5 novos testes

---

## 🎯 Cenários de Teste

### Cenário 1: Diretório padrão (vazio)
```
Menu:
1 - URL: https://youtube.com/watch?v=abc123
2 - Início: 00:00:00
3 - Fim: 00:01:00
4 - Nome: meu-video
5 - Diretório: [Enter]

Resultado:
→ Salva em: /home/user/projeto/meu-video.mp4
✓ Vídeo salvo em: /home/user/projeto/meu-video.mp4
```

### Cenário 2: Diretório customizado existente
```
Menu:
1 - URL: https://youtube.com/watch?v=abc123
2 - Início: 00:00:00
3 - Fim: 00:01:00
4 - Nome: meu-video
5 - Diretório: /tmp/downloads

Resultado:
→ Salva em: /tmp/downloads/meu-video.mp4
✓ Vídeo salvo em: /tmp/downloads/meu-video.mp4
```

### Cenário 3: Diretório não existe (usuário cria)
```
Menu:
1 - URL: https://youtube.com/watch?v=abc123
5 - Diretório: /novo/dir

Validação:
→ ⚠️  Diretório '/novo/dir' não existe.
→ Deseja criá-lo? (s/n): s
→ ✓ Diretório criado: /novo/dir

Resultado:
→ Salva em: /novo/dir/meu-video.mp4
```

### Cenário 4: Diretório não existe (usuário cancela)
```
Menu:
1 - URL: https://youtube.com/watch?v=abc123
5 - Diretório: /novo/dir

Validação:
→ ⚠️  Diretório '/novo/dir' não existe.
→ Deseja criá-lo? (s/n): n
→ ❌ Operação cancelada.
```

### Cenário 5: Sem permissão de escrita
```
Menu:
1 - URL: https://youtube.com/watch?v=abc123
5 - Diretório: /root/videos

Validação:
→ ❌ Erro: Sem permissão de escrita em '/root/videos'
```

### Cenário 6: Caminho relativo
```
Menu:
5 - Diretório: ../downloads

Resultado:
→ Normalizado para: /home/user/downloads (Path.GetFullPath)
→ Salva em: /home/user/downloads/meu-video.mp4
```

### Cenário 7: Caminho Windows
```
Menu (Windows):
5 - Diretório: C:\Videos

Resultado:
→ Salva em: C:\Videos\meu-video.mp4
```

### Cenário 8: Trailing slash
```
Menu:
5 - Diretório: /tmp/videos/

Resultado:
→ Normalizado para: /tmp/videos (Remove trailing slash)
→ Salva em: /tmp/videos/meu-video.mp4
```

---

## ✅ Quality Gates

**Após implementação, TODOS devem passar:**

1. **Testes unitários:**
   ```bash
   dotnet test
   ```
   - ✅ Todos os testes existentes continuam passando
   - ✅ 5 novos testes para OutputDirectory
   - ✅ Zero falhas permitidas

2. **Build:**
   ```bash
   dotnet build
   ```
   - ✅ Zero warnings permitido
   - ✅ Zero erros de compilação

3. **Testes manuais:**
   - ✅ Diretório vazio (usa atual)
   - ✅ Diretório existente
   - ✅ Diretório inexistente → criar
   - ✅ Diretório sem permissão
   - ✅ Caminho relativo
   - ✅ Caminho com espaços
   - ✅ Trailing slash

---

## 🚀 Fluxo de Implementação

### Passo 1: Preparação
```bash
# Criar branch
git checkout -b feature/custom-output-directory

# Atualizar status da task
bd update Cutube-1yx --status in_progress
```

### Passo 2: Implementação
1. Modificar `Menu.cs` + `IMenuService.cs`
2. Modificar `IFileService.cs` + `FileService.cs`
3. Modificar `ProgramWorkflow.cs`
4. Adicionar testes em `MenuTests.cs`

### Passo 3: Validação
```bash
# Rodar testes
dotnet test

# Rodar build
dotnet build

# Testar manualmente (cenários acima)
```

### Passo 4: Commit e Push
```bash
git add .
git commit -m "feat: add custom output directory option

- Add OutputDirectory property to Menu.cs (5th question)
- Add directory operations to IFileService (DirectoryExists, CreateDirectory, HasWritePermission)
- Update ProgramWorkflow to validate and use custom directory
- Add 5 new tests for OutputDirectory
- Normalize paths (relative→absolute, remove trailing slash)
- Prompt to create directory if doesn't exist
- Validate write permissions before download"

git push -u origin feature/custom-output-directory
```

### Passo 5: Pull Request
- Criar PR no GitHub
- Associar com task Cutube-1yx
- Aguardar review

---

## 🔍 Detalhes de Implementação

### Normalização de Caminho

**Recomendação:** Implementar método `NormalizePath()` em `ProgramWorkflow.cs`

```csharp
private string NormalizePath(string path)
{
    // 1. Converter caminhos relativos para absolutos
    var fullPath = Path.GetFullPath(path);

    // 2. Remover trailing slash
    fullPath = fullPath.TrimEnd(
        Path.DirectorySeparatorChar,
        Path.AltDirectorySeparatorChar
    );

    return fullPath;
}
```

**Por quê?**
- `Path.GetFullPath()` resolve `../downloads` → `/home/user/downloads`
- `TrimEnd()` remove `/` ou `\` no final para evitar `//videos`

### Suporte a Caminhos Relativos

**Decisão:** ✅ Sim, deve funcionar

**Exemplos:**
- `../downloads` → `/home/user/downloads`
- `./videos` → `/home/user/projeto/videos`
- `videos` → `/home/user/projeto/videos`

**Implementação:** `Path.GetFullPath()` resolve automaticamente

### Trailing Slash

**Decisão:** ✅ Sim, deve normalizar

**Exemplos:**
- `/tmp/videos/` → `/tmp/videos`
- `C:\Videos\` → `C:\Videos`

**Implementação:** `TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)`

### Validação de Caminho

**Decisão:** ✅ Sim, validar antes de usar

**Validações:**
1. Diretório existe? → Oferecer criar
2. Tem permissão de escrita? → Erro se não
3. Caminho válido? → `Path.GetFullPath()` lança exceção se inválido

---

## 📊 Impacto

### Breaking Changes
- ✅ Menu agora tem 5 perguntas (não 4)
- ✅ IFileService tem 3 novos métodos
- ⚠️ Testes E2E podem precisar de update

### Backward Compatibility
- ✅ Diretório vazio = comportamento atual (diretório atual)
- ✅ Todos os códigos existentes continuam funcionando

### Performance
- ✅ Sem impacto significativo
- ⚠️ Teste de permissão cria arquivo temporário (custo ~1ms)

---

## 📝 Checklist

- [ ] Branch criada (`feature/custom-output-directory`)
- [ ] Task atualizada (`bd update Cutube-1yx --status in_progress`)
- [ ] Menu.cs modificado (OutputDirectory + 5ª pergunta)
- [ ] IMenuService.cs modificado (getter adicionado)
- [ ] IFileService.cs modificado (3 métodos adicionados)
- [ ] FileService.cs modificado (implementação completa)
- [ ] ProgramWorkflow.cs modificado (validação + uso do diretório)
- [ ] 5 testes adicionados em MenuTests.cs
- [ ] `dotnet test` passa (100%)
- [ ] `dotnet build` sem warnings
- [ ] Testes manuais executados (todos cenários)
- [ ] Commit com conventional commit
- [ ] Push para remoto
- [ ] PR criada no GitHub
- [ ] Task marcada como done (`bd close Cutube-1yx`)

---

## 🎓 Referências

- **Path.Combine()**: https://learn.microsoft.com/en-us/dotnet/api/system.io.path.combine
- **Directory.CreateDirectory()**: https://learn.microsoft.com/en-us/dotnet/api/system.io.directory.createdirectory
- **Path.GetFullPath()**: https://learn.microsoft.com/en-us/dotnet/api/system.io.path.getfullpath

---

**Autor:** OpenCode  
**Data:** 2026-02-02  
**Versão:** 1.0
