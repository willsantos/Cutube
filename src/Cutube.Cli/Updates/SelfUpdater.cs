using System.Diagnostics;
using System.IO.Compression;

namespace Cutube.Cli.Updates;

/// <summary>
/// Resultado de uma tentativa de self-update
/// </summary>
public record SelfUpdateResult(bool Success, string? ErrorMessage)
{
    public static SelfUpdateResult Ok() => new(true, null);

    public static SelfUpdateResult Fail(string message) => new(false, message);
}

public interface ISelfUpdater
{
    /// <summary>
    /// Comando manual de instalação para a plataforma atual (fallback do self-update)
    /// </summary>
    string ManualInstallCommand { get; }

    /// <summary>
    /// Nome do asset de release para a plataforma/arquitetura atuais; null se não houver
    /// </summary>
    string? GetAssetName();

    Task<SelfUpdateResult> UpdateAsync(string tag, CancellationToken ct = default);

    /// <summary>
    /// Executa o binário recém-atualizado com os mesmos argumentos e aguarda o exit code
    /// </summary>
    Task<int> RestartWithArgs(IReadOnlyList<string> args);

    /// <summary>
    /// Remove binário .old residual de atualizações anteriores no Windows (best-effort)
    /// </summary>
    void CleanupOldBinary();
}

/// <summary>
/// Baixa o binário da release, extrai e substitui o executável em execução
/// (substituição atômica no mesmo diretório; no Windows o exe atual é
/// renomeado para .old e removido na próxima inicialização)
/// </summary>
public class SelfUpdater : ISelfUpdater
{
    public const string GitHubRepo = "willsantos/Cutube";

    public static readonly TimeSpan DownloadTimeout = TimeSpan.FromMinutes(5);

    private const string InstallScriptUrl = "https://willsantos.github.io/Cutube";

    private readonly IHttpClientService _httpClient;
    private readonly IProcessService _processService;
    private readonly IEnvironmentService _environmentService;
    private readonly string? _processPath;

    public SelfUpdater(
        IHttpClientService httpClient,
        IProcessService processService,
        IEnvironmentService environmentService)
        : this(httpClient, processService, environmentService, Environment.ProcessPath)
    {
    }

    /// <summary>
    /// Caminho do executável injetável para testes
    /// </summary>
    public SelfUpdater(
        IHttpClientService httpClient,
        IProcessService processService,
        IEnvironmentService environmentService,
        string? processPath)
    {
        _httpClient = httpClient;
        _processService = processService;
        _environmentService = environmentService;
        _processPath = processPath;
    }

    /// <inheritdoc/>
    public string ManualInstallCommand => _environmentService.IsWindows()
        ? $"iwr -useb {InstallScriptUrl}/install.ps1 | iex"
        : $"curl -fsSL {InstallScriptUrl}/install.sh | bash";

    /// <inheritdoc/>
    public string? GetAssetName()
    {
        var arch = _environmentService.OSArchitecture switch
        {
            "X64" => "amd64",
            "Arm64" => "arm64",
            _ => null
        };

        if (arch == null)
            return null;

        if (_environmentService.IsWindows())
            return arch == "amd64" ? "cutube-windows-amd64.exe.zip" : null;

        if (_environmentService.IsLinux())
            return $"cutube-linux-{arch}.tar.gz";

        if (_environmentService.IsMacOS())
            return $"cutube-macos-{arch}.tar.gz";

        return null;
    }

    /// <inheritdoc/>
    public async Task<SelfUpdateResult> UpdateAsync(string tag, CancellationToken ct = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_processPath))
                return FailWithManualHint("não foi possível determinar o executável em execução");

            var hostName = Path.GetFileNameWithoutExtension(_processPath);
            if (hostName.Equals("dotnet", StringComparison.OrdinalIgnoreCase))
                return FailWithManualHint(
                    "executando via 'dotnet run'; instale o binário publicado para permitir auto-update");

            var assetName = GetAssetName();
            if (assetName == null)
                return FailWithManualHint("não há pacote do Cutube para esta plataforma/arquitetura");

            var url = $"https://github.com/{GitHubRepo}/releases/download/{tag}/{assetName}";
            var tempDir = Path.Combine(Path.GetTempPath(), $"cutube-update-{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);

            try
            {
                ct.ThrowIfCancellationRequested();

                var bytes = await _httpClient.TryGetByteArrayAsync(url, DownloadTimeout);
                if (bytes == null)
                    return FailWithManualHint($"falha ao baixar {assetName} da release {tag}");

                var archivePath = Path.Combine(tempDir, assetName);
                await File.WriteAllBytesAsync(archivePath, bytes, ct);

                var newBinaryPath = Extract(archivePath, tempDir);
                if (newBinaryPath == null)
                    return FailWithManualHint($"binário não encontrado no pacote {assetName}");

                ReplaceBinary(newBinaryPath, _processPath);
                return SelfUpdateResult.Ok();
            }
            finally
            {
                TryDeleteDirectory(tempDir);
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return FailWithManualHint($"erro durante a atualização: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<int> RestartWithArgs(IReadOnlyList<string> args)
    {
        if (string.IsNullOrWhiteSpace(_processPath))
            return 1;

        var startInfo = new ProcessStartInfo
        {
            FileName = _processPath,
            UseShellExecute = false
        };

        foreach (var arg in args)
            startInfo.ArgumentList.Add(arg);

        var process = _processService.Start(startInfo);
        return await process.WaitForExitAsync();
    }

    /// <inheritdoc/>
    public void CleanupOldBinary()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_processPath))
                return;

            var oldPath = _processPath + ".old";
            if (File.Exists(oldPath))
                File.Delete(oldPath);
        }
        catch
        {
            // best-effort: .old residual não pode atrapalhar a execução
        }
    }

    /// <summary>
    /// Extrai o pacote no diretório temporário e retorna o caminho do binário
    /// </summary>
    private string? Extract(string archivePath, string tempDir)
    {
        var binaryName = _environmentService.IsWindows() ? "cutube.exe" : "cutube";
        var binaryPath = Path.Combine(tempDir, binaryName);

        if (_environmentService.IsWindows())
        {
            ZipFile.ExtractToDirectory(archivePath, tempDir, overwriteFiles: true);
        }
        else
        {
            var tar = _processService.Start(new ProcessStartInfo
            {
                FileName = "tar",
                Arguments = $"-xzf \"{archivePath}\" -C \"{tempDir}\"",
                UseShellExecute = false,
                CreateNoWindow = true
            });

            if (tar.WaitForExitAsync().GetAwaiter().GetResult() != 0)
                return null;
        }

        return File.Exists(binaryPath) ? binaryPath : null;
    }

    /// <summary>
    /// Substituição atômica: copia o novo binário para o diretório de destino
    /// (mesmo filesystem) e renomea sobre o existente
    /// </summary>
    private void ReplaceBinary(string newBinaryPath, string targetPath)
    {
        var stagedPath = targetPath + ".new";
        File.Copy(newBinaryPath, stagedPath, overwrite: true);

        if (_environmentService.IsWindows())
        {
            var oldPath = targetPath + ".old";
            if (File.Exists(oldPath))
                File.Delete(oldPath);

            File.Move(targetPath, oldPath);
            try
            {
                File.Move(stagedPath, targetPath);
            }
            catch
            {
                File.Move(oldPath, targetPath);
                throw;
            }
        }
        else
        {
            if (!OperatingSystem.IsWindows())
            {
                // 755 (rwxr-xr-x) sem depender de binário chmod externo
                File.SetUnixFileMode(stagedPath,
                    UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
                    UnixFileMode.GroupRead | UnixFileMode.GroupExecute |
                    UnixFileMode.OtherRead | UnixFileMode.OtherExecute);
            }

            File.Move(stagedPath, targetPath, overwrite: true);
        }
    }

    private SelfUpdateResult FailWithManualHint(string reason)
        => SelfUpdateResult.Fail($"{reason}.\n  Atualize manualmente: {ManualInstallCommand}");

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
                Directory.Delete(path, recursive: true);
        }
        catch
        {
            // best-effort: temp residual é limpo pelo sistema operacional
        }
    }
}
