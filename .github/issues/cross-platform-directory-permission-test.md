# [Improvement] Fix cross-platform test for DirectoryWithoutWritePermission validation

**Status:** 🔴 Backlog
**Created:** 2026-02-10
**Priority:** P3 (Low)
**Labels:** Testing, Cross-platform, Improvement
**Project:** Cutube

## Problem

The test `DirectoryWithoutWritePermission_ReturnsFailure` in `Cutube.Domain.Tests` is currently skipped because it doesn't work on Unix-like systems (Linux/macOS).

Current implementation uses `File.SetAttributes(path, FileAttributes.ReadOnly)` which:
- ✅ Works on Windows
- ❌ Doesn't work on Linux/macOS for directories (Unix permissions work differently)

## Impact

- **Code coverage:** The `UnauthorizedAccessException` catch block in `ValidationService.ValidateDirectory()` is not covered by automated tests
- **Risk:** If this logic is accidentally removed during refactoring, tests won't catch it

## Root Cause

Unix-like systems use permission modes (`rwxrwxrwx`) instead of `FileAttributes.ReadOnly`. The .NET `FileAttributes.ReadOnly` doesn't properly translate to Unix directory permissions.

## Technical Details

### Current Implementation (Broken on Unix)

```csharp
[Fact(Skip = "Requires special OS permissions")]
[Trait("Category", "RequiresRoot")]
public void DirectoryWithoutWritePermission_ReturnsFailure()
{
    if (!OperatingSystem.IsLinux() && !OperatingSystem.IsMacOS())
    {
        return;
    }

    var readOnlyDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    Directory.CreateDirectory(readOnlyDir);

    try
    {
        File.SetAttributes(readOnlyDir, FileAttributes.ReadOnly); // ❌ Doesn't work on Unix

        var result = _validator.ValidateDirectory(readOnlyDir);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Be("Sem permissão de escrita no diretório");
    }
    finally
    {
        try
        {
            File.SetAttributes(readOnlyDir, FileAttributes.Normal);
            Directory.Delete(readOnlyDir);
        }
        catch { }
    }
}
```

### Proposed Solution (Cross-platform)

```csharp
[Fact]
public void DirectoryWithoutWritePermission_ReturnsFailure()
{
    var readOnlyDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    Directory.CreateDirectory(readOnlyDir);

    try
    {
        // Remove write permissions using platform-specific approach
        if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
        {
            // Unix: Use chmod to remove all permissions
            var chmod = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "/bin/chmod",
                    Arguments = $"000 \"{readOnlyDir}\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            chmod.Start();
            chmod.WaitForExit();
        }
        else
        {
            // Windows: FileAttributes.ReadOnly works fine
            File.SetAttributes(readOnlyDir, FileAttributes.ReadOnly);
        }

        // Now test validation
        var result = _validator.ValidateDirectory(readOnlyDir);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Be("Sem permissão de escrita no diretório");
    }
    finally
    {
        // Restore permissions before cleanup
        if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
        {
            var chmod = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "/bin/chmod",
                    Arguments = $"755 \"{readOnlyDir}\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            chmod.Start();
            chmod.WaitForExit();
        }
        else
        {
            File.SetAttributes(readOnlyDir, FileAttributes.Normal);
        }

        try { Directory.Delete(readOnlyDir); }
        catch { }
    }
}
```

## Acceptance Criteria

- [ ] Test runs successfully on Linux (GitHub Actions Ubuntu runner)
- [ ] Test runs successfully on macOS
- [ ] Test runs successfully on Windows
- [ ] `UnauthorizedAccessException` catch block in `ValidationService.ValidateDirectory()` is covered by tests
- [ ] No tests skipped in CI/CD pipeline for ValidationService
- [ ] Code coverage for `ValidationService.cs` remains at 100%

## Implementation Notes

1. **Why `chmod 000`?** Removes all permissions (read, write, execute) for owner, group, and others
2. **Why `chmod 755` in cleanup?** Restores standard directory permissions (rwxr-xr-x)
3. **CI/CD Consideration:** GitHub Actions runners on Ubuntu have chmod available by default
4. **Alternative Approach:** Could also use `Mono.Unix` nuget package for native Unix permission handling

## Related Files

- `tests/Cutube.Domain.Tests/Services/ValidationServiceTests.cs` (lines 228-258)
- `src/Cutube.Domain/Services/ValidationService.cs` (lines 108-109)

## Related Work

- Epic: Cutube-2i6 (Épico 2: Arquitetura Híbrida CLI + API)
- Phase: 2.3 - WebSocket para Progresso em Tempo Real (completed)

## Estimated Effort

1-2 hours (implementation + testing + verification on multiple platforms)
