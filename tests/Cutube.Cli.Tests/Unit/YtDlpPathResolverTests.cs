using System.Net;
using Cutube.Infrastructure;
using FluentAssertions;

namespace Cutube.Cli.Tests.Unit;

public class YtDlpPathResolverTests
{
    [Fact]
    public void GetDownloadUrl_ReturnsRealOfficialAssetName()
    {
        var url = YtDlpPathResolver.GetDownloadUrl();

        url.Should().NotBeNull();
        url.Should().StartWith("https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp");

        // Nomes que não existem na release oficial (causa do bug do 404)
        url.Should().NotContain("_x64.exe");
        url.Should().NotContain("_macos_arm64");

        if (OperatingSystem.IsWindows())
            url.Should().BeOneOf(
                "https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp.exe",
                "https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp_x86.exe",
                "https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp_arm64.exe");

        if (OperatingSystem.IsLinux())
            url.Should().BeOneOf(
                "https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp_linux",
                "https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp_linux_aarch64");

        if (OperatingSystem.IsMacOS())
            url.Should().Be("https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp_macos");
    }

    [Fact]
    public async Task DownloadToFileAsync_WritesBinaryToTargetPath()
    {
        var handler = new MockHttpMessageHandler(new byte[] { 1, 2, 3 });
        using var httpClient = new HttpClient(handler);
        var targetPath = Path.Combine(Path.GetTempPath(), $"yt-dlp-test-{Guid.NewGuid()}");

        try
        {
            await YtDlpPathResolver.DownloadToFileAsync(httpClient, "https://example.com/yt-dlp", targetPath);

            File.Exists(targetPath).Should().BeTrue();
            (await File.ReadAllBytesAsync(targetPath)).Should().BeEquivalentTo(new byte[] { 1, 2, 3 });
        }
        finally
        {
            if (File.Exists(targetPath))
                File.Delete(targetPath);
        }
    }

    private sealed class MockHttpMessageHandler(byte[] content) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(content)
            });
        }
    }
}
