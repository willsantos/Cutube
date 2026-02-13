using System.Diagnostics.CodeAnalysis;

namespace Cutube.Cli;

public interface ITimer
{
    void Change(TimeSpan dueTime, TimeSpan period);
    void Dispose();
}

public interface ITimerFactory
{
    ITimer Create(TimerCallback callback);
}

[ExcludeFromCodeCoverage]
public class SystemTimer : ITimer
{
    private readonly Timer _timer;

    public SystemTimer(TimerCallback callback)
    {
        _timer = new Timer(callback);
    }

    public void Change(TimeSpan dueTime, TimeSpan period) => _timer.Change(dueTime, period);

    public void Dispose() => _timer.Dispose();
}

[ExcludeFromCodeCoverage]
public class SystemTimerFactory : ITimerFactory
{
    public ITimer Create(TimerCallback callback) => new SystemTimer(callback);
}
