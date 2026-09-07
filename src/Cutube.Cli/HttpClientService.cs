using System.Diagnostics.CodeAnalysis;

namespace Cutube.Cli;

[ExcludeFromCodeCoverage]
public class HttpClientService : IHttpClientService, IDisposable
{
    private readonly HttpClient _httpClient = new();

    public Task<string> GetStringAsync(string url) =>
        _httpClient.GetStringAsync(url);

    public Task<byte[]> GetByteArrayAsync(string url) =>
        _httpClient.GetByteArrayAsync(url);

    public async Task<string?> TryGetStringAsync(string url, TimeSpan timeout)
    {
        try
        {
            using var cts = new CancellationTokenSource(timeout);
            return await _httpClient.GetStringAsync(url, cts.Token);
        }
        catch
        {
            return null;
        }
    }

    public async Task<byte[]?> TryGetByteArrayAsync(string url, TimeSpan timeout)
    {
        try
        {
            using var cts = new CancellationTokenSource(timeout);
            return await _httpClient.GetByteArrayAsync(url, cts.Token);
        }
        catch
        {
            return null;
        }
    }

    public void DefaultRequestHeadersAdd(string key, string value) =>
        _httpClient.DefaultRequestHeaders.Add(key, value);

    public void Dispose() => _httpClient.Dispose();
}
