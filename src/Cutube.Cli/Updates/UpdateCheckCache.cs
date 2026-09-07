using System.Text.Json;
using System.Text.Json.Serialization;

namespace Cutube.Cli.Updates;

public interface IUpdateCheckCache
{
    /// <summary>
    /// Indica se já passou o intervalo mínimo (24h) desde a última verificação
    /// </summary>
    bool ShouldCheck(DateTime nowUtc);

    /// <summary>
    /// Grava o timestamp da última verificação (best-effort, nunca lança)
    /// </summary>
    void MarkChecked(DateTime nowUtc);
}

/// <summary>
/// Cache da última verificação de atualização (regra: no máximo 1x por dia),
/// persistido em LocalApplicationData/Cutube/update-check.json
/// </summary>
public class UpdateCheckCache : IUpdateCheckCache
{
    internal static readonly TimeSpan CheckInterval = TimeSpan.FromHours(24);

    private readonly string _cachePath;

    public UpdateCheckCache() : this(GetDefaultCachePath())
    {
    }

    /// <summary>
    /// Caminho customizável para testes (mesmo padrão do ConfigService)
    /// </summary>
    public UpdateCheckCache(string cachePath)
    {
        _cachePath = cachePath;
    }

    private static string GetDefaultCachePath()
        => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Cutube",
            "update-check.json");

    /// <inheritdoc/>
    public bool ShouldCheck(DateTime nowUtc)
    {
        var last = ReadLastCheckUtc();

        if (last == null)
            return true;

        // Relógio no passado / timestamp no futuro: re-verifica imediatamente
        if (last.Value > nowUtc)
            return true;

        return nowUtc - last.Value >= CheckInterval;
    }

    /// <inheritdoc/>
    public void MarkChecked(DateTime nowUtc)
    {
        try
        {
            var directory = Path.GetDirectoryName(_cachePath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            var state = new CacheState { LastCheckUtc = nowUtc };
            File.WriteAllText(_cachePath, JsonSerializer.Serialize(state));
        }
        catch
        {
            // O cache é uma otimização; falha de escrita não pode afetar a CLI
        }
    }

    private DateTime? ReadLastCheckUtc()
    {
        try
        {
            if (!File.Exists(_cachePath))
                return null;

            var json = File.ReadAllText(_cachePath);
            var state = JsonSerializer.Deserialize<CacheState>(json);
            return state?.LastCheckUtc;
        }
        catch
        {
            // Corrompido/ilegível: tratar como ausente e re-verificar
            return null;
        }
    }

    private sealed class CacheState
    {
        [JsonPropertyName("lastCheckUtc")]
        public DateTime LastCheckUtc { get; set; }
    }
}
