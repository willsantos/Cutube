namespace Cutube.Cli;

public interface IHttpClientService : System.IDisposable
{
    Task<string> GetStringAsync(string url);
    Task<byte[]> GetByteArrayAsync(string url);

    /// <summary>
    /// GET que retorna null em qualquer falha (rede, timeout, HTTP) em vez de lançar
    /// </summary>
    Task<string?> TryGetStringAsync(string url, TimeSpan timeout);

    /// <summary>
    /// Download binário que retorna null em qualquer falha (rede, timeout, HTTP) em vez de lançar
    /// </summary>
    Task<byte[]?> TryGetByteArrayAsync(string url, TimeSpan timeout);

    void DefaultRequestHeadersAdd(string key, string value);
}
