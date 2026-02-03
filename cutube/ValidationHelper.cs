using System.Text.RegularExpressions;

namespace cutube;

public static class ValidationHelper
{
    private static readonly string[] ValidYouTubeHosts = 
    [
        "youtube.com",
        "www.youtube.com",
        "m.youtube.com",
        "youtu.be"
    ];

    public static bool IsValidUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return false;

        if (uri.Scheme != "http" && uri.Scheme != "https")
            return false;

        var host = uri.Host.ToLowerInvariant();
        return ValidYouTubeHosts.Contains(host);
    }

    public static void ValidateUrl(string url)
    {
        if (!IsValidUrl(url))
        {
            throw new ArgumentException(
                "❌ URL inválida. Use uma URL do YouTube (youtube.com ou youtu.be) que comece com http:// ou https://");
        }
    }
}
