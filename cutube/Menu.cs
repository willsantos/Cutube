namespace cutube;


public static class Menu
{
    public static string Url { get; set; } = String.Empty;
    public static string Start { get; set; } = String.Empty;
    public static string End { get; set; } = String.Empty;

    public static void Show()
    {
        Console.WriteLine("Cutube - Um cortador de vídeos para o Youtube.");
        Console.WriteLine("Desenvolvido por: Wilson Santos");
        Console.WriteLine("-----------------------------------------------");
        Console.WriteLine("1 - Digite a url do vídeo.");
        Url = Console.ReadLine();
        Console.WriteLine("2 - Digite o tempo de início (hh:mm:ss).");
        Start = Console.ReadLine();
        Console.WriteLine("3 - Digite o tempo de fim (hh:mm:ss).");
        End = Console.ReadLine();

    }
}