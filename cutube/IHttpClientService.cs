namespace cutube;

public interface IHttpClientService
{
    Task<string> GetStringAsync(string url);
    Task<byte[]> GetByteArrayAsync(string url);
    void DefaultRequestHeadersAdd(string key, string value);
}
