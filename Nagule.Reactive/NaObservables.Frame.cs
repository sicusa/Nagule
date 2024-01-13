namespace Nagule.Reactive;

using System.Reactive;
using System.Reactive.Linq;
using Sia;

public static partial class NaObservables
{
    public static IObservable<Unit> EveryFrame { get; } = Observable.Create<Unit>(o => {
        var framer = Context<World>.Current!.GetAddon<SimulationFramer>();
        bool cancelled = false;
        framer.Start(() => {
            if (cancelled) { return true; }
            o.OnNext(Unit.Default);
            return false;
        });
        return () => cancelled = true;
    });

    public static IObservable<Unit> NextFrame { get; } = Observable.Create<Unit>(o => {
        var framer = Context<World>.Current!.GetAddon<SimulationFramer>();
        bool cancelled = false;
        framer.Start(() => {
            if (cancelled) { return true; }
            o.OnNext(Unit.Default);
            o.OnCompleted();
            return true;
        });
        return () => cancelled = true;
    });

    private static long FrameProvider(SimulationFramer framer)
        => framer.FrameCount;

    public static IObservable<Unit> IntervalFrame(long interval)
        => Interval(interval, FrameProvider);

    public static IObservable<Unit> TimerFrame(long dueTime)
        => Interval(dueTime, FrameProvider);

    public static IObservable<Unit> TimerFrame(long dueTime, long period)
        => Timer(dueTime, period, FrameProvider);

    public static IObservable<TSource> DelayFrameSubscription<TSource>(this IObservable<TSource> source, long delay)
        => DelaySubscription(source, delay, FrameProvider);

    public static IObservable<DegreeInterval<TSource, long>> WithFrameInterval<TSource>(this IObservable<TSource> source)
        => WithInterval(source, FrameProvider);

    public static IObservable<TSource> DelayFrame<TSource>(this IObservable<TSource> source, long delay)
        => Delay(source, delay, FrameProvider);

    public static IObservable<TSource> SampleFrame<TSource>(this IObservable<TSource> source, long period)
        => Sample(source, period, FrameProvider);

    public static IObservable<TSource> BatchFrame<TSource>(this IObservable<TSource> source, long period)
        => Batch(source, period, FrameProvider);

    public static IObservable<TSource> ThrottleFrame<TSource>(this IObservable<TSource> source, long period)
        => Throttle(source, period, FrameProvider);

    public static IObservable<TSource> ThrottleFirstFrame<TSource>(this IObservable<TSource> source, long period)
        => ThrottleFirst(source, period, FrameProvider);
    
    public static IObservable<TSource> TimeoutFrame<TSource>(this IObservable<TSource> source, long dueTime)
        => Timeout(source, dueTime, FrameProvider);

    public static IObservable<TSource> TimeoutFrame<TSource>(this IObservable<TSource> source, long dueTime, IObservable<TSource> other)
        => Timeout(source, dueTime, other, FrameProvider);
}