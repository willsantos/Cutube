namespace cutube;

public class ProgressBar : IDisposable, IProgress<int>
{
    private const int BlockCount = 100;
    private readonly TimeSpan _animationInterval = TimeSpan.FromSeconds(1.0 / 8);
    private const string Animation = @"|/-\";

    private readonly Timer _timer;
    
    private double _currentProgress;
    private string _currentText = string.Empty;
    private bool _disposed;
    private int _animationIndex;
    public string Message { get; set; } = string.Empty;

    public ProgressBar()
    {
        _timer = new Timer(TimerHandler!);
        if (!Console.IsOutputRedirected)
        {
            ResetTimer();
        }
        
    }
    
    private void ResetTimer()
    {
        _timer.Change(_animationInterval, TimeSpan.FromMilliseconds(-1));
    }
    private void TimerHandler(object state)
    {
        lock (_timer)
        {
            if (_disposed) return;

            var progressBlockCount = (int)(_currentProgress / 100 * BlockCount);
            var percent = (int)_currentProgress;
            var text =
                $"[{new string('#', progressBlockCount)}{new string('-', BlockCount - progressBlockCount)}] {percent,3}% {Animation[_animationIndex++ % Animation.Length]}";
            UpdateText(text);

            ResetTimer();
        }
    }
    
    private void UpdateText(string text)
    {
        // Obtém a posição atual do cursor
        var left = Console.CursorLeft;
        var top = Console.CursorTop;

        // Move o cursor para a esquerda e escreve o texto
        Console.CursorLeft = 0;
        Console.Write(text);

        // Preenche com espaços se o texto for menor que o anterior
        var length = _currentText.Length - text.Length;
        if (length > 0)
        {
            Console.Write(new string(' ', length));
        }

        // Restaura a posição anterior do cursor
        Console.CursorLeft = left;
        Console.CursorTop = top;

        // Atualiza o texto atual
        _currentText = text;
    }

    public void Report(int value)
    {
        value = Math.Max(0, Math.Min(100, value));
        Interlocked.Exchange(ref _currentProgress, value);
        if (!Console.IsOutputRedirected)
        {
            ResetTimer();
        }
    }
    
    public void Dispose()
    {
        lock (_timer)
        {
            _disposed = true;
            UpdateText(string.Empty);
        }
    }
}