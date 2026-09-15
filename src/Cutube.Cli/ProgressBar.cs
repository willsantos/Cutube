using Cutube.Cli.Theming;

namespace Cutube.Cli;

public class ProgressBar : IDisposable, IProgress<int>
{
    private const int BlockCount = 100;
    private readonly TimeSpan _animationInterval = TimeSpan.FromSeconds(1.0 / 8);
    private const string Animation = @"|/-\";

    private readonly ITimer _timer;
    private readonly IConsoleService _consoleService;
    private readonly object _sync = new();
    
    private double _currentProgress;
    private string _currentText = string.Empty;
    private bool _disposed;
    private int _animationIndex;
    public string Message { get; set; } = string.Empty;

    public ProgressBar() : this(new ConsoleService(), new SystemTimerFactory())
    {
    }

    public ProgressBar(IConsoleService consoleService, ITimerFactory timerFactory)
    {
        _consoleService = consoleService;
        _timer = timerFactory.Create(TimerHandler);
        if (!_consoleService.IsOutputRedirected)
        {
            ResetTimer();
        }
        
    }
    
    private void ResetTimer()
    {
        _timer.Change(_animationInterval, TimeSpan.FromMilliseconds(-1));
    }
    private void TimerHandler(object? state)
    {
        lock (_sync)
        {
            if (_disposed) return;

            var progressBlockCount = (int)(_currentProgress / 100 * BlockCount);
            var percent = (int)_currentProgress;
            var text =
                $"[{new string('#', progressBlockCount)}{new string('-', BlockCount - progressBlockCount)}] {percent,3}% {Animation[_animationIndex++ % Animation.Length]}";
            UpdateText(Colorize(text), text);

            ResetTimer();
        }
    }

    /// <summary>
    /// Aplica a primária Oroborus à barra quando o terminal suporta cor;
    /// em saída redirecionada o texto permanece plano (spec FR-16/FR-19)
    /// </summary>
    private string Colorize(string text)
        => text.Length == 0
            ? text
            : ConsoleTheme.Colorize(_consoleService, ConsoleTheme.Primary, text);

    private void UpdateText(string styledText, string plainText)
    {
        // Obtém a posição atual do cursor
        var left = _consoleService.CursorLeft;
        var top = _consoleService.CursorTop;

        // Move o cursor para a esquerda e escreve o texto
        _consoleService.CursorLeft = 0;
        _consoleService.Write(styledText);

        // Preenche com espaços se o texto for menor que o anterior
        // (comprimento comparado no texto puro: códigos ANSI não contam)
        var length = _currentText.Length - plainText.Length;
        if (length > 0)
        {
            _consoleService.Write(new string(' ', length));
        }

        // Restaura a posição anterior do cursor
        _consoleService.CursorLeft = left;
        _consoleService.CursorTop = top;

        // Atualiza o texto atual
        _currentText = plainText;
    }

    public void Report(int value)
    {
        value = Math.Max(0, Math.Min(100, value));
        Interlocked.Exchange(ref _currentProgress, value);
        if (!_consoleService.IsOutputRedirected)
        {
            lock (_sync)
            {
                if (_disposed) return;
                ResetTimer();
            }
        }
    }
    
    public void Dispose()
    {
        lock (_sync)
        {
            _disposed = true;
            UpdateText(Colorize(string.Empty), string.Empty);
            _timer.Dispose();
        }
    }
}
