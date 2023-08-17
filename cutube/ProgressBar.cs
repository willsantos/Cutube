namespace cutube;

public class ProgressBar : IDisposable, IProgress<int>
{
    private const int blockCount = 10;
    private readonly TimeSpan animationInterval = TimeSpan.FromSeconds(1.0 / 8);
    private const string animation = @"|/-\";

    private readonly Timer timer;
    
    private double currentProgress = 0;
    private string currentText = string.Empty;
    private bool disposed = false;
    private int animationIndex = 0;
    public string Message { get; set; } = string.Empty;

    public ProgressBar()
    {
        timer = new Timer(TimerHandler);
        if (!Console.IsOutputRedirected)
        {
            ResetTimer();
        }
        
    }
    
    private void ResetTimer()
    {
        timer.Change(animationInterval, TimeSpan.FromMilliseconds(-1));
    }
    private void TimerHandler(object state)
    {
        lock (timer)
        {
            if (disposed) return;

            int progressBlockCount = (int)(currentProgress / 100 * blockCount);
            int percent = (int)currentProgress;
            string text = string.Format("[{0}{1}] {2,3}% {3}",
                new string('#', progressBlockCount), new string('-', blockCount - progressBlockCount),
                percent,
                animation[animationIndex++ % animation.Length]);
            UpdateText(text);

            ResetTimer();
        }
    }
    
    private void UpdateText(string text)
    {
        // Obtém a posição atual do cursor
        int left = Console.CursorLeft;
        int top = Console.CursorTop;

        // Move o cursor para a esquerda e escreve o texto
        Console.CursorLeft = 0;
        Console.Write(text);

        // Preenche com espaços se o texto for menor que o anterior
        int length = currentText.Length - text.Length;
        if (length > 0)
        {
            Console.Write(new string(' ', length));
        }

        // Restaura a posição anterior do cursor
        Console.CursorLeft = left;
        Console.CursorTop = top;

        // Atualiza o texto atual
        currentText = text;
    }

    public void Report(int value)
    {
        value = Math.Max(0, Math.Min(100, value));
        Interlocked.Exchange(ref currentProgress, value);
        if (!Console.IsOutputRedirected)
        {
            ResetTimer();
        }
    }
    
    public void Dispose()
    {
        lock (timer)
        {
            disposed = true;
            UpdateText(string.Empty);
        }
    }
}