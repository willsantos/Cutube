using System.Diagnostics.CodeAnalysis;

namespace cutube;


[ExcludeFromCodeCoverage]
public static class Menu
{
    public static string Url { get; private set; } = string.Empty;
    public static string Start { get; private set; } = string.Empty;
    public static string End { get; private set; } = string.Empty;
    public static string CustomFileName { get; private set; } = string.Empty;

    internal static void Reset()
    {
        Url = string.Empty;
        Start = string.Empty;
        End = string.Empty;
        CustomFileName = string.Empty;
    }

    public static void Show()
    {
        Console.WriteLine("Cutube - Um cortador de vídeos para o Youtube.");
        Console.WriteLine("Desenvolvido por: Wilson Santos");
        Console.WriteLine("-----------------------------------------------");
        Console.WriteLine("1 - Digite a url do vídeo.");
        Url =  Console.ReadLine() ?? throw new InvalidOperationException("A url não pode ser vazia.");
        Console.WriteLine("2 - Digite o tempo de início (ex: 00:01:30, 1:30, 90s, 1h30m).");
        Start = Console.ReadLine() ?? throw new InvalidOperationException("O tempo de inicio não pode ser vazio.");
        Console.WriteLine("3 - Digite o tempo de fim (ex: 00:02:00, 2:00, 120s, 2m).");
        End = Console.ReadLine() ?? throw new InvalidOperationException("O tempo de fim não pode ser vazio.");
        Console.WriteLine("4 - Digite o nome do arquivo (opcional, pressione Enter para usar título)");
        var input = Console.ReadLine() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(input))
        {
            CustomFileName = Path.GetFileNameWithoutExtension(input.Trim());
        }


    }
}
