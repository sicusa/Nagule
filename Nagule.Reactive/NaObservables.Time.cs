namespace Nagule.Reactive;

using System.Reactive;
        
public static partial class NaObservables
{
    private static float TimeProvider(SimulationFramer framer)
        => framer.Time;

    public static IObservable<Unit> Interval(float interval)
        => Interval(interval, TimeProvider);

    public static IObservable<Unit> Timer(float dueTime)
        => Interval(dueTime, TimeProvider);

    public static IObservable<Unit> Timer(float dueTime, float period)
        => Timer(dueTime, period, TimeProvider);

    public static IObservable<TSource> DelaySubscription<TSource>(this IObservable<TSource> source, float delay)
        => DelaySubscription(source, delay, TimeProvider);

    public static IObservable<DegreeInterval<TSource, float>> WithInterval<TSource>(this IObservable<TSource> source)
        => WithInterval(source, TimeProvider);

    public static IObservable<TSource> Delay<TSource>(this IObservable<TSource> source, float delay)
        => Delay(source, delay, TimeProvider);

    public static IObservable<TSource> Sample<TSource>(this IObservable<TSource> source, float period)
        => Sample(source, period, TimeProvider);

    public static IObservable<TSource> Batch<TSource>(this IObservable<TSource> source, float period)
        => Batch(source, period, TimeProvider);

    public static IObservable<TSource> Throttle<TSource>(this IObservable<TSource> source, float period)
        => Throttle(source, period, TimeProvider);

    public static IObservable<TSource> ThrottleFirst<TSource>(this IObservable<TSource> source, float period)
        => ThrottleFirst(source, period, TimeProvider);
    
    public static IObservable<TSource> Timeout<TSource>(this IObservable<TSource> source, float dueTime)
        => Timeout(source, dueTime, TimeProvider);

    public static IObservable<TSource> Timeout<TSource>(this IObservable<TSource> source, float dueTime, IObservable<TSource> other)
        => Timeout(source, dueTime, other, TimeProvider);
}