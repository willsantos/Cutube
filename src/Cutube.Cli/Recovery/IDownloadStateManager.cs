namespace Cutube.Cli.Recovery;

/// <summary>
/// Serviço de gerenciamento de estado de downloads com persistência
/// </summary>
public interface IDownloadStateManager
{
    /// <summary>
    /// Cria novo estado de download
    /// </summary>
    Task<string> CreateStateAsync(DownloadState state);

    /// <summary>
    /// Recupera estado por ID
    /// </summary>
    Task<DownloadState?> GetStateAsync(string stateId);

    /// <summary>
    /// Atualiza estado de forma atômica (lock)
    /// </summary>
    Task UpdateStateAsync(string stateId, Action<DownloadState> update);

    /// <summary>
    /// Lista todos os estados ativos (não completados)
    /// </summary>
    Task<List<DownloadState>> GetActiveStatesAsync();

    /// <summary>
    /// Marca estado como completado
    /// </summary>
    Task MarkCompletedAsync(string stateId);

    /// <summary>
    /// Marca estado como falho com mensagem de erro
    /// </summary>
    Task MarkFailedAsync(string stateId, string errorMessage);

    /// <summary>
    /// Marca estado como cancelado
    /// </summary>
    Task MarkCancelledAsync(string stateId);

    /// <summary>
    /// Remove estado do disco
    /// </summary>
    Task DeleteStateAsync(string stateId);

    /// <summary>
    /// Limpa estados antigos (baseado em idade)
    /// </summary>
    Task<int> CleanupOldStatesAsync(TimeSpan maxAge);

    /// <summary>
    /// Lista todos os estados (para debug/admin)
    /// </summary>
    Task<List<DownloadState>> GetAllStatesAsync();
}
