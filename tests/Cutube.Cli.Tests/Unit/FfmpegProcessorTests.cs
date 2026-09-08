using Cutube.Infrastructure;
using FluentAssertions;
using Xunit;

namespace Cutube.Cli.Tests.Unit;

public class FfmpegProcessorTests
{
    [Fact]
    public void IsAvailable_WithInvalidFfmpegPath_ReturnsFalse()
    {
        var processor = new FfmpegProcessor("definitely-not-a-real-ffmpeg-binary");

        processor.IsAvailable().Should().BeFalse();
    }

    [Fact]
    public void IsAvailable_WithSystemFfmpeg_MatchesProbe()
    {
        // Não falha em máquinas sem ffmpeg: apenas valida consistência
        var ffmpegOnPath = ProbeFfmpegOnPath();

        var processor = new FfmpegProcessor();
        processor.IsAvailable().Should().Be(ffmpegOnPath);
    }

    private static bool ProbeFfmpegOnPath()
    {
        try
        {
            using var process = System.Diagnostics.Process.Start(
                new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = "-version",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                });

            if (process == null)
                return false;

            if (!process.WaitForExit(5000))
            {
                try { process.Kill(entireProcessTree: true); } catch { /* já saiu */ }
                return false;
            }

            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}
