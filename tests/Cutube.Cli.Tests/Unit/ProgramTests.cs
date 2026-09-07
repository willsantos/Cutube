using FluentAssertions;
using System.IO;
using System.Reflection;
using Cutube.Cli;
using Xunit;

namespace Cutube.Tests.Unit;

[Collection("Sequential")]
public class ProgramTests
{
    [Fact]
    public async Task Main_VersionFlag_PrintsAssemblyInformationalVersion()
    {
        using var sw = new StringWriter();
        var originalOut = Console.Out;
        Console.SetOut(sw);

        try
        {
            await Program.Main(new[] { "--version" });
        }
        finally
        {
            Console.SetOut(originalOut);
        }

        var expected = typeof(Program).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "unknown";

        sw.ToString().Trim().Should().Be($"cutube {expected}");
    }
}
