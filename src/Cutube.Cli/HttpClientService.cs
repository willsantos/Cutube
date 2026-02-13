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

    public void DefaultRequestHeadersAdd(string key, string value) => 
        _httpClient.DefaultRequestHeaders.Add(key, value);

    public void Dispose() => _httpClient.Dispose();
}
