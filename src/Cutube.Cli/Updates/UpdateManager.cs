using Cutube.Cli.Configuration;
using System.Diagnostics.CodeAnalysis;

namespace Cutube.Cli.Updates;

/// <summary>
/// Orquestra a verificação de atualização: guardas (não-TTY, config, build de
/// dev), cache diário, prompt de confirmação, self-update e reinício do comando.
/// Nunca lança: qualquer falha do subsistema não pode derrubar a CLI
/// </summary>
public class UpdateManager
{
    private readonly IUpdateChecker _checker;
    private readonly ISelfUpdater _updater;
    private readonly IUpdateCheckCache _cache;
    private readonly IConsoleService _console;
    private readonly Func<bool> _isInputRedirected;
    private readonly Func<string?> _currentVersion;

    [ExcludeFromCodeCoverage]
    public UpdateManager()
    {
        var httpClient = new HttpClientService();
        _checker = new UpdateChecker(httpClient, new UpdateCheckCache());
        _updater = new SelfUpdater(httpClient, new ProcessService(), new EnvironmentService());
        _cache = new UpdateCheckCache();
        _console = new ConsoleService();
        _isInputRedirected = () => Console.IsInputRedirected;
        _currentVersion = VersionInfo.GetCurrent;
    }

    /// <summary>
    /// Construtor com dependências injetadas (testes)
    /// </summary>
    public UpdateManager(
        IUpdateChecker checker,
        ISelfUpdater updater,
        IUpdateCheckCache cache,
        IConsoleService console,
        Func<bool>? isInputRedirected = null,
        Func<string?>? currentVersion = null)
    {
        _checker = checker;
        _updater = updater;
        _cache = cache;
        _console = console;
        _isInputRedirected = isInputRedirected ?? (() => Console.IsInputRedirected);
        _currentVersion = currentVersion ?? VersionInfo.GetCurrent;
    }

    /// <summary>
    /// Comandos que devem pular a verificação automática: respostas instantâneas
    /// (--version/--help) e o próprio ciclo de update (cutube update)
    /// </summary>
    public static bool ShouldSkip(string[] args)
    {
        if (args.Length == 0)
            return false;

        return args[0] switch
        {
            "--version" or "--help" or "-h" or "help" or "update" => true,
            _ => false
        };
    }

    /// <summary>
    /// Fluxo automático no startup. Retorna null para "continuar o comando
    /// normalmente" ou o exit code do comando reiniciado na versão nova
    /// </summary>
    public async Task<int?> RunAsync(string[] args, bool checkForUpdates, CancellationToken ct = default)
    {
        try
        {
            _updater.CleanupOldBinary();

            if (!checkForUpdates || _isInputRedirected())
                return null;

            if (!VersionInfo.IsPublishedBuild(_currentVersion()))
                return null;

            if (!_cache.ShouldCheck(DateTime.UtcNow))
                return null;

            var check = await _checker.CheckAsync(ct);
            if (!check.HasUpdate)
                return null;

            _console.WriteLine($"\nNova versão disponível: {check.LatestVersion} (atual: {check.CurrentVersion})");
            _console.Write("Deseja atualizar agora? [S/n]: ");

            if (!IsConfirmed(_console.ReadLine()))
            {
                _console.WriteLine("Atualização ignorada. Continuando na versão atual.");
                return null;
            }

            return await ApplyUpdateAsync(check.LatestVersion!, args, ct);
        }
        catch
        {
            // Zero regressão: qualquer falha na verificação não pode afetar o comando
            return null;
        }
    }

    /// <summary>
    /// Fluxo do comando explícito 'cutube update': ignora o cache diário e,
    /// em contexto não-interativo, atualiza sem perguntar
    /// </summary>
    public async Task<int> RunForcedAsync(string[] args, CancellationToken ct = default)
    {
        try
        {
            _updater.CleanupOldBinary();

            var current = _currentVersion();
            if (!VersionInfo.IsPublishedBuild(current))
            {
                _console.WriteLine("⚠ Esta é uma build de desenvolvimento; o auto-update está indisponível.");
                _console.WriteLine($"  Instale a versão mais recente: {_updater.ManualInstallCommand}");
                return 1;
            }

            var check = await _checker.CheckAsync(ct);

            if (!check.HasUpdate)
            {
                _console.WriteLine(check.LatestVersion == null
                    ? "⚠ Não foi possível verificar atualizações agora. Verifique sua conexão."
                    : $"✓ Você já está na versão mais recente ({current}).");
                return check.LatestVersion == null ? 1 : 0;
            }

            var interactive = !_isInputRedirected();
            if (interactive)
            {
                _console.WriteLine($"\nNova versão disponível: {check.LatestVersion} (atual: {check.CurrentVersion})");
                _console.Write("Deseja atualizar agora? [S/n]: ");

                if (!IsConfirmed(_console.ReadLine()))
                {
                    _console.WriteLine("Atualização cancelada.");
                    return 0;
                }
            }

            var update = await _updater.UpdateAsync(check.LatestVersion!, ct);
            if (!update.Success)
            {
                _console.WriteLine($"⚠ Falha ao atualizar: {update.ErrorMessage}");
                return 1;
            }

            _console.WriteLine($"✅ Atualizado para {check.LatestVersion}.");

            var remainingArgs = args.Skip(1).ToArray();
            return remainingArgs.Length == 0
                ? 0
                : await _updater.RestartWithArgs(remainingArgs);
        }
        catch (OperationCanceledException)
        {
            _console.WriteLine("\n⚠️  Verificação de atualização cancelada.");
            return 1;
        }
        catch (Exception ex)
        {
            _console.WriteLine($"⚠ Não foi possível concluir a atualização: {ex.Message}");
            return 1;
        }
    }

    private async Task<int?> ApplyUpdateAsync(string latestTag, string[] args, CancellationToken ct)
    {
        var update = await _updater.UpdateAsync(latestTag, ct);

        if (!update.Success)
        {
            _console.WriteLine($"⚠ Não foi possível atualizar automaticamente: {update.ErrorMessage}");
            return null;
        }

        _console.WriteLine($"✅ Atualizado para {latestTag}. Reiniciando comando...");
        return await _updater.RestartWithArgs(args);
    }

    /// <summary>
    /// Confirmação: Enter (vazio) ou "s" aceitam; EOF/Ctrl+C ou qualquer
    /// outra resposta recusam
    /// </summary>
    private static bool IsConfirmed(string? answer)
        => answer != null
           && (string.IsNullOrWhiteSpace(answer)
               || answer.Trim().Equals("s", StringComparison.OrdinalIgnoreCase));
}
