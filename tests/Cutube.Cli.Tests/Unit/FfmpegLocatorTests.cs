using Cutube.Infrastructure;
using FluentAssertions;
using Xunit;

namespace Cutube.Cli.Tests.Unit;

public class FfmpegLocatorTests
{
    [Fact]
    public void GetDownloadUrl_OnNonWindows_ReturnsNull()
    {
        if (OperatingSystem.IsWindows())
            return; // assert abaixo é específico de não-Windows

        FfmpegLocator.GetDownloadUrl().Should().BeNull();
    }

    [Fact]
    public void GetDownloadUrl_OnWindows_ReturnsBtbNLatestAsset()
    {
        if (!OperatingSystem.IsWindows())
            return; // asset é específico de Windows

        var url = FfmpegLocator.GetDownloadUrl();

        url.Should().NotBeNull();
        url.Should().BeOneOf(
            "https://github.com/BtbN/FFmpeg-Builds/releases/latest/download/ffmpeg-master-latest-win64-gpl.zip",
            "https://github.com/BtbN/FFmpeg-Builds/releases/latest/download/ffmpeg-master-latest-winarm64-gpl.zip");
    }

    [Fact]
    public async Task DownloadAndExtractAsync_ExtractsBinariesFromNestedZipFolders()
    {
        // A extração é genérica (zip com ffmpeg.exe/ffprobe.exe aninhados),
        // então o teste roda em qualquer SO
        var handler = new FakeZipHandler(CreateZipWithNestedBinaries());
        using var httpClient = new HttpClient(handler);

        var targetDir = Path.Combine(Path.GetTempPath(), $"cutube-ffmpeg-test-{Guid.NewGuid():N}");

        try
        {
            await FfmpegLocator.DownloadAndExtractAsync(
                httpClient,
                "https://example.com/ffmpeg.zip",
                targetDir);

            File.Exists(Path.Combine(targetDir, "ffmpeg.exe")).Should().BeTrue();
            File.Exists(Path.Combine(targetDir, "ffprobe.exe")).Should().BeTrue();
            // Entradas irrelevantes (docs/ pastas) não são extraídas
            Directory.GetFiles(targetDir).Length.Should().Be(2);
        }
        finally
        {
            if (Directory.Exists(targetDir))
                Directory.Delete(targetDir, true);
        }
    }

    /// <summary>
    /// Cria um zip real com estrutura aninhada igual ao BtbN: uma pasta-raiz
    /// com bin/ffmpeg.exe e bin/ffprobe.exe e um arquivo de licença
    /// </summary>
    private static byte[] CreateZipWithNestedBinaries()
    {
        var workDir = Path.Combine(Path.GetTempPath(), $"cutube-zip-src-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(workDir, "root", "bin"));
        File.WriteAllText(Path.Combine(workDir, "root", "bin", "ffmpeg.exe"), "fake ffmpeg");
        File.WriteAllText(Path.Combine(workDir, "root", "bin", "ffprobe.exe"), "fake ffprobe");
        File.WriteAllText(Path.Combine(workDir, "root", "LICENSE.txt"), "fake license");

        var zipPath = Path.Combine(workDir, "asset.zip");
        System.IO.Compression.ZipFile.CreateFromDirectory(
            Path.Combine(workDir, "root"), zipPath);
        var bytes = File.ReadAllBytes(zipPath);
        Directory.Delete(workDir, true);
        return bytes;
    }

    private sealed class FakeZipHandler(byte[] content) : HttpClientHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(content)
            });
        }
    }
}
