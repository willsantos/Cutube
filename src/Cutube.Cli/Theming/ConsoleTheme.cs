using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Cutube.Cli.Theming;

/// <summary>
/// Paleta Oroborus do terminal e detecção de suporte a cor.
/// A cor só entra em terminal interativo sem NO_COLOR; em pipe, CI ou
/// saída redirecionada o texto sai exatamente como antes (spec FR-16).
/// </summary>
public static class ConsoleTheme
{
    public const string Reset = "\x1b[0m";
    public const string Primary = "\x1b[38;2;179;161;255m"; // #b3a1ff
    public const string Success = "\x1b[38;2;74;222;128m";  // #4ade80
    public const string Warning = "\x1b[38;2;251;191;36m";  // #fbbf24
    public const string Error = "\x1b[38;2;255;110;132m";   // #ff6e84

    static ConsoleTheme()
    {
        WindowsVtReady = !OperatingSystem.IsWindows();
        if (OperatingSystem.IsWindows())
            WindowsVtReady = WindowsVirtualTerminal.Enable();
    }

    /// <summary>Resultado do best-effort de ativação de VT no Windows; fora dele, sempre pronto</summary>
    private static readonly bool WindowsVtReady;

    public static bool IsColorEnabled(IConsoleService console)
    {
        if (console.IsOutputRedirected)
            return false;

        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("NO_COLOR")))
            return false;

        // FR-16: CI e terminais sem suporte recebem saída plana
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CI")))
            return false;

        if (OperatingSystem.IsWindows())
            return WindowsVtReady;

        // Unix: terminal de verdade sempre define TERM; ausência ou "dumb" = sem cor
        var term = Environment.GetEnvironmentVariable("TERM");
        return !string.IsNullOrEmpty(term) && !term.Equals("dumb", StringComparison.OrdinalIgnoreCase);
    }

    public static string Colorize(IConsoleService console, string ansiColor, string text)
        => IsColorEnabled(console) ? ansiColor + text + Reset : text;

    [ExcludeFromCodeCoverage]
    private static class WindowsVirtualTerminal
    {
        private const int StdOutputHandle = -11;
        private const uint EnableVirtualTerminalProcessing = 0x0004;
        private static readonly IntPtr InvalidHandleValue = new(-1);

        /// <summary>
        /// Ativa VT no conhost clássico do Windows (best-effort); terminais
        /// modernos já suportam ANSI. Retorna se o VT ficou disponível
        /// </summary>
        internal static bool Enable()
        {
            try
            {
                var handle = GetStdHandle(StdOutputHandle);
                if (handle == InvalidHandleValue || !GetConsoleMode(handle, out var mode))
                    return false;

                if ((mode & EnableVirtualTerminalProcessing) != 0)
                    return true;

                return SetConsoleMode(handle, mode | EnableVirtualTerminalProcessing);
            }
            catch
            {
                // Sem VT os códigos apareceriam como texto — cosmético, nunca bloqueia
                return false;
            }
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetStdHandle(int nStdHandle);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);
    }
}

/// <summary>
/// Saída semântica sobre IConsoleService: aplica a cor Oroborus quando o
/// terminal suporta e preserva o texto original nas demais condições — o
/// conteúdo das mensagens nunca muda (spec FR-17/FR-18).
/// </summary>
public static class ConsoleThemeExtensions
{
    /// <summary>Títulos, seções e etapas principais — primária/lavanda.</summary>
    public static void WriteTitle(this IConsoleService console, string message)
        => console.WriteLine(ConsoleTheme.Colorize(console, ConsoleTheme.Primary, message));

    /// <summary>Conclusão bem-sucedida — verde.</summary>
    public static void WriteSuccess(this IConsoleService console, string message)
        => console.WriteLine(ConsoleTheme.Colorize(console, ConsoleTheme.Success, message));

    /// <summary>Aviso — âmbar.</summary>
    public static void WriteWarning(this IConsoleService console, string message)
        => console.WriteLine(ConsoleTheme.Colorize(console, ConsoleTheme.Warning, message));

    /// <summary>Erro — coral.</summary>
    public static void WriteError(this IConsoleService console, string message)
        => console.WriteLine(ConsoleTheme.Colorize(console, ConsoleTheme.Error, message));

    /// <summary>Linha de progresso — primária com percentual em destaque.</summary>
    public static void WriteProgress(this IConsoleService console, string message)
        => console.WriteLine(ConsoleTheme.Colorize(console, ConsoleTheme.Primary, message));
}
