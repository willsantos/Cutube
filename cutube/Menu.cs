using System.Diagnostics.CodeAnalysis;

namespace cutube;


[ExcludeFromCodeCoverage]
public static class Menu
{
    public static string Url { get; private set; } = string.Empty;
    public static string Start { get; private set; } = string.Empty;
    public static string End { get; private set; } = string.Empty;
    public static string CustomFileName { get; private set; } = string.Empty;
    public static string OutputDirectory { get; private set; } = string.Empty;
    public static bool AudioOnly { get; private set; } = false;

    internal static void Reset()
    {
        Url = string.Empty;
        Start = string.Empty;
        End = string.Empty;
        CustomFileName = string.Empty;
        OutputDirectory = string.Empty;
        AudioOnly = false;
    }

    public static void Show()
    {
        Console.WriteLine("Cutube - Um cortador de vídeos para o Youtube.");
        Console.WriteLine("Desenvolvido por: Wilson Santos");
        Console.WriteLine("-----------------------------------------------");
        Console.WriteLine("1 - Digite a url do vídeo.");
        Url =  Console.ReadLine() ?? throw new InvalidOperationException("❌ URL não pode ser vazia. Digite uma URL válida do YouTube (ex: https://youtube.com/watch?v=... ou https://youtu.be/...)");
        Console.WriteLine("2 - Digite o tempo de início (ex: 00:01:30, 1:30, 90s, 1h30m).");
        Start = Console.ReadLine() ?? throw new InvalidOperationException("❌ Tempo de início não pode ser vazio. Use formatos como: 00:01:30, 1:30, 90s, 1h30m.");
        Console.WriteLine("3 - Digite o tempo de fim (ex: 00:02:00, 2:00, 120s, 2m).");
        End = Console.ReadLine() ?? throw new InvalidOperationException("❌ Tempo de fim não pode ser vazio. Use formatos como: 00:02:00, 2:00, 120s, 2m.");
        Console.WriteLine("4 - Digite o nome do arquivo (opcional, pressione Enter para usar título)");
        var input = Console.ReadLine() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(input))
        {
            CustomFileName = Path.GetFileNameWithoutExtension(input.Trim());
        }

        Console.WriteLine("5 - Diretório de destino (opcional, Enter para usar atual): ");
        var dirInput = Console.ReadLine() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(dirInput))
        {
            OutputDirectory = dirInput.Trim();
        }

        Console.WriteLine("6 - Download completo ou áudio apenas?");
        Console.WriteLine("   1 - Vídeo + Áudio (MP4)");
        Console.WriteLine("   2 - Apenas Áudio (MP3)");
        var audioChoice = Console.ReadLine();
        AudioOnly = audioChoice == "2";
    }
}
