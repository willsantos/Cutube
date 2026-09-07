using System.Text.Json;

namespace Cutube.Cli.Updates;

/// <summary>
/// Resultado da verificação de atualização
/// </summary>
public record UpdateCheckResult(
    bool HasUpdate,
    string? CurrentVersion,
    string? LatestVersion);

public interface IUpdateChecker
{
    Task<UpdateCheckResult> CheckAsync(CancellationToken ct = default);
}

/// <summary>
/// Consulta a última release no GitHub e compara com a versão atual.
/// Qualquer falha (rede, timeout, rate limit, resposta inválida) resulta em
/// HasUpdate = false, sem lançar exceção
/// </summary>
public class UpdateChecker : IUpdateChecker
{
    public const string LatestReleaseApiUrl =
        "https://api.github.com/repos/willsantos/Cutube/releases/latest";

    public static readonly TimeSpan CheckTimeout = TimeSpan.FromSeconds(3);

    private readonly IHttpClientService _httpClient;
    private readonly IUpdateCheckCache _cache;

    public UpdateChecker(IHttpClientService httpClient, IUpdateCheckCache cache)
    {
        _httpClient = httpClient;
        _cache = cache;
    }

    /// <inheritdoc/>
    public async Task<UpdateCheckResult> CheckAsync(CancellationToken ct = default)
    {
        var current = VersionInfo.GetCurrent();

        try
        {
            _httpClient.DefaultRequestHeadersAdd("User-Agent", "Cutube");
            var json = await _httpClient.TryGetStringAsync(LatestReleaseApiUrl, CheckTimeout);
            _cache.MarkChecked(DateTime.UtcNow);

            if (json == null)
                return new UpdateCheckResult(false, current, null);

            using var doc = JsonDocument.Parse(json);
            var tag = doc.RootElement.GetProperty("tag_name").GetString();

            var latest = TryParseTag(tag);
            var currentCore = TryParseTag(current);

            if (tag == null || latest == null || currentCore == null)
                return new UpdateCheckResult(false, current, null);

            return IsNewer(latest.Value, currentCore.Value)
                ? new UpdateCheckResult(true, current, tag)
                : new UpdateCheckResult(false, current, tag);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return new UpdateCheckResult(false, current, null);
        }
    }

    /// <summary>
    /// Faz parse de tag semver ("v1.2.3", "1.2.3", "1.2.3-rc.1", "1.2.3+sha").
    /// Retorna null se não for semver válido
    /// </summary>
    public static (int Major, int Minor, int Patch)? TryParseTag(string? tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
            return null;

        var core = tag.Trim().TrimStart('v', 'V');

        var separatorIndex = core.IndexOfAny(['+', '-']);
        if (separatorIndex >= 0)
            core = core[..separatorIndex];

        var parts = core.Split('.');
        if (parts.Length != 3)
            return null;

        if (parts.Any(p => p.Length == 0 || !p.All(char.IsDigit)))
            return null;

        return (int.Parse(parts[0]), int.Parse(parts[1]), int.Parse(parts[2]));
    }

    private static bool IsNewer((int Major, int Minor, int Patch) candidate, (int Major, int Minor, int Patch) current)
    {
        if (candidate.Major != current.Major)
            return candidate.Major > current.Major;
        if (candidate.Minor != current.Minor)
            return candidate.Minor > current.Minor;
        return candidate.Patch > current.Patch;
    }
}
