using System.Reflection;

namespace Cutube.Cli.Updates;

/// <summary>
/// Obtém e valida a versão corrente do assembly da CLI
/// </summary>
public static class VersionInfo
{
    /// <summary>
    /// InformationalVersion do assembly (ex.: "1.2.3" ou "1.2.3+sha")
    /// </summary>
    public static string? GetCurrent()
        => typeof(VersionInfo).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion;

    /// <summary>
    /// Build publicada = versão com core semver válido. O SDK embute "+sha"
    /// (SourceRevisionId) mesmo em builds publicadas, então o sufixo é ignorado;
    /// null, "unknown" ou formato inválido indicam build não publicada
    /// </summary>
    public static bool IsPublishedBuild(string? version)
        => UpdateChecker.TryParseTag(version) != null;
}
