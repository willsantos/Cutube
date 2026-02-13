using cutube;
using ITimer = cutube.ITimer;

namespace Cutube.Tests.Helpers;

public class FakeTimer : ITimer
{
    private readonly TimerCallback _callback;

    public FakeTimer(TimerCallback callback)
    {
        _callback = callback;
    }

    public int ChangeCalls { get; private set; }
    public TimeSpan? LastDueTime { get; private set; }
    public TimeSpan? LastPeriod { get; private set; }

    public void Change(TimeSpan dueTime, TimeSpan period)
    {
        ChangeCalls++;
        LastDueTime = dueTime;
        LastPeriod = period;
    }

    public void Trigger(object? state = null) => _callback(state);

    public void Dispose()
    {
    }
}

public class FakeTimerFactory : ITimerFactory
{
    public FakeTimer? LastTimer { get; private set; }

    public ITimer Create(TimerCallback callback)
    {
        LastTimer = new FakeTimer(callback);
        return LastTimer;
    }
}
