namespace cutube;


public static class Menu
{
    public static string Url { get; private set; } = string.Empty;
    public static string Start { get; private set; } = string.Empty;
    public static string End { get; private set; } = string.Empty;

    public static void Show()
    {
        Console.WriteLine("Cutube - Um cortador de vídeos para o Youtube.");
        Console.WriteLine("Desenvolvido por: Wilson Santos");
        Console.WriteLine("-----------------------------------------------");
        Console.WriteLine("1 - Digite a url do vídeo.");
        Url =  Console.ReadLine() ?? throw new InvalidOperationException("A url não pode ser vazia.");
        Console.WriteLine("2 - Digite o tempo de início (hh:mm:ss).");
        Start = Console.ReadLine() ?? throw new InvalidOperationException("O tempo de inicio não pode ser vazio.");
        Console.WriteLine("3 - Digite o tempo de fim (hh:mm:ss).");
        End = Console.ReadLine() ?? throw new InvalidOperationException("O tempo de fim não pode ser vazio.");
        
        
    }
}