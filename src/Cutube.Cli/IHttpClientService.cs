namespace cutube;

public interface IHttpClientService : System.IDisposable
{
    Task<string> GetStringAsync(string url);
    Task<byte[]> GetByteArrayAsync(string url);
    void DefaultRequestHeadersAdd(string key, string value);
}
