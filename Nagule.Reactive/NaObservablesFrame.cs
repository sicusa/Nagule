namespace Nagule.Reactive;

using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using CommunityToolkit.HighPerformance;
using Sia;
        
public record struct FrameInterval<TSource>(TSource Value, long Interval);

public static class NaObservables
{
    public static IObservable<Unit> EveryFrame { get; } = Observable.Create<Unit>(o => {
        var framer = Context<World>.Current!.GetAddon<SimulationFramer>();
        bool cancelled = false;
        framer.Start(() => {
            if (cancelled) { return true; }
            o.OnNext(Unit.Default);
            return false;
        });
        return () => { cancelled = true; };
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
        return () => { cancelled = true; };
    });

    public static IObservable<Unit> IntervalFrame(long intervalFrameCount)
        => intervalFrameCount <= 0
            ? EveryFrame
            : Observable.Create<Unit>(o => {
                var framer = Context<World>.Current!.GetAddon<SimulationFramer>();
                bool cancelled = false;
                long counter = 0;
                framer.Start(() => {
                    if (cancelled) { return true; }
                    if (++counter > intervalFrameCount) {
                        o.OnNext(Unit.Default);
                        counter = 0;
                    }
                    return false;
                });
                return () => { cancelled = true; };
            });

    public static IObservable<Unit> TimerFrame(long dueFrameCount)
        => Observable.Create<Unit>(o => {
            var framer = Context<World>.Current!.GetAddon<SimulationFramer>();
            bool cancelled = false;
            long counter = 0;
            framer.Start(() => {
                if (cancelled) { return true; }
                if (++counter > dueFrameCount) {
                    o.OnNext(Unit.Default);
                    o.OnCompleted();
                    return true;
                }
                return false;
            });
            return () => { cancelled = true; };
        });
    
    public static IObservable<Unit> TimerFrame(long dueTimeFrameCount, long periodFrameCount)
        => Observable.Create<Unit>(o => {
            var framer = Context<World>.Current!.GetAddon<SimulationFramer>();
            bool cancelled = false;
            long counter = 0;
            long periodCounter = 0;
            framer.Start(() => {
                if (cancelled) { return true; }
                if (++counter > dueTimeFrameCount) {
                    o.OnNext(Unit.Default);
                    o.OnCompleted();
                    return true;
                }
                if (++periodCounter > periodFrameCount) {
                    periodCounter = 0;
                    o.OnNext(Unit.Default);
                }
                return false;
            });
            return () => { cancelled = true; };
        });

    public static IObservable<TSource> DelayFrameSubscription<TSource>(this IObservable<TSource> source, long frameCount)
        => Observable.Create<TSource>(o => {
            var framer = Context<World>.Current!.GetAddon<SimulationFramer>();
            bool cancelled = false;
            long counter = 0;
            IDisposable? disposable = null;
            object sync = new();
            framer.Start(() => {
                if (cancelled) { return true; }
                if (++counter > frameCount) {
                    lock (sync) {
                        if (cancelled) { return true; }
                        disposable = source.Subscribe(o);
                    }
                    return true;
                }
                return false;
            });
            return () => {
                cancelled = true;
                lock (sync) {
                    disposable?.Dispose();
                }
            };
        });
    
    public static IObservable<TSource> DelayFrame<TSource>(this IObservable<TSource> source, long delayFrameCount)
        => Observable.Create<TSource>(o => {
            var framer = Context<World>.Current!.GetAddon<SimulationFramer>();
            bool cancelled = false;
            bool delayFinished = false;
            long initFrame = framer.FrameCount;
            List<TSource>? delayed = null;
            object sync = new();
            var disposable = source.Subscribe(
                value => {
                    if (delayFinished) {
                        o.OnNext(value);
                        return;
                    }
                    lock (sync) {
                        if (delayFinished) {
                            o.OnNext(value);
                        }
                        else {
                            delayed ??= [];
                            delayed.Add(value);
                        }
                    }
                },
                e => o.OnError(e),
                () => o.OnCompleted());
            framer.Start(() => {
                if (cancelled) { return true; }
                if (framer.FrameCount - initFrame > delayFrameCount) {
                    lock (sync) {
                        if (delayed != null) {
                            foreach (ref var delayedValue in delayed.AsSpan()) {
                                o.OnNext(delayedValue);
                            }
                            delayed = null;
                        }
                        delayFinished = true;
                    }
                    return true;
                }
                return false;
            });
            return () => {
                cancelled = true;
                disposable.Dispose();
            };
        });

    public static IObservable<TSource> SampleFrame<TSource>(this IObservable<TSource> source, long periodFrameCount)
        => Observable.Create<TSource>(o => {
            var framer = Context<World>.Current!.GetAddon<SimulationFramer>();
            bool cancelled = false;
            long counter = 0;
            TSource? lastValue = default;
            object sync = new();
            var disposable = source.Subscribe(
                value => {
                    lock (sync) { lastValue = value; }
                },
                e => o.OnError(e),
                () => o.OnCompleted());
            framer.Start(() => {
                if (cancelled) { return true; }
                if (++counter > periodFrameCount) {
                    if (lastValue != null) {
                        lock (sync)  {
                            o.OnNext(lastValue);
                        }
                    }
                    counter = 0;
                }
                return false;
            });
            return () => {
                cancelled = true;
                disposable.Dispose();
            };
        });
    
    public static IObservable<FrameInterval<TSource>> FrameInterval<TSource>(this IObservable<TSource> source)
        => Observable.Create<FrameInterval<TSource>>(o => {
            var framer = Context<World>.Current!.GetAddon<SimulationFramer>();
            long lastFrame = framer.FrameCount;
            return source
                .Select(value => {
                    long frame = framer.FrameCount;
                    long interval = frame - lastFrame;
                    lastFrame = frame;
                    return new FrameInterval<TSource>(value, interval);
                })
                .Subscribe(o);
        });
    
    public static IObservable<TSource> BatchFrame<TSource>(this IObservable<TSource> source, long periodFrameCount)
        => Observable.Create<TSource>(o => {
            var framer = Context<World>.Current!.GetAddon<SimulationFramer>();
            long lastFrame = framer.FrameCount;
            var batch = new List<TSource>();
            var disposable = source.Subscribe(
                value => {
                    long frame = framer.FrameCount;
                    if (frame - lastFrame <= periodFrameCount) {
                        batch.Add(value);
                        return;
                    }
                    else {
                        lastFrame = frame;
                        foreach (ref var batchedValue in batch.AsSpan()) {
                            o.OnNext(batchedValue);
                        }
                        batch.Clear();
                        o.OnNext(value);
                    }
                },
                e => o.OnError(e),
                () => o.OnCompleted());
            return () => disposable.Dispose();
        });
    
    public static IObservable<TSource> ThrottleFrame<TSource>(this IObservable<TSource> source, long periodFrameCount)
        => Observable.Create<TSource>(o => {
            var framer = Context<World>.Current!.GetAddon<SimulationFramer>();
            bool cancelled = false;
            long lastFrame = framer.FrameCount;
            TSource? lastValue = default;
            object sync = new();
            var disposable = source.Subscribe(
                value => {
                    lastFrame = framer.FrameCount;
                    lock (sync) { lastValue = value; }
                },
                e => o.OnError(e),
                () => o.OnCompleted());
            framer.Start(() => {
                if (cancelled) { return true; }
                if (lastValue != null && framer.FrameCount - lastFrame > periodFrameCount) {
                    lock (sync) {
                        o.OnNext(lastValue);
                        lastValue = default;
                    }
                }
                return false;
            });
            return () => {
                cancelled = true;
                disposable.Dispose();
            };
        });

    public static IObservable<TSource> ThrottleFirstFrame<TSource>(this IObservable<TSource> source, long periodFrameCount)
        => Observable.Create<TSource>(o => {
            var framer = Context<World>.Current!.GetAddon<SimulationFramer>();
            bool cancelled = false;
            long lastFrame = framer.FrameCount + periodFrameCount;
            TSource? lastValue = default;
            object sync = new();
            var disposable = source.Subscribe(
                value => {
                    lastFrame = framer.FrameCount;
                    lock (sync) { lastValue = value; }
                },
                e => o.OnError(e),
                () => o.OnCompleted());
            framer.Start(() => {
                if (cancelled) { return true; }
                if (lastValue != null && framer.FrameCount - lastFrame > periodFrameCount) {
                    lock (sync) {
                        o.OnNext(lastValue);
                        lastValue = default;
                    }
                }
                return false;
            });
            return () => {
                cancelled = true;
                disposable?.Dispose();
            };
        });

    public static IObservable<TSource> TimeoutFrame<TSource>(this IObservable<TSource> source, long frameCount)
        => Observable.Create<TSource>(o => {
            var framer = Context<World>.Current!.GetAddon<SimulationFramer>();
            bool cancelled = false;
            long lastFrame = framer.FrameCount;
            object sync = new();
            IDisposable? disposable = source
                .Do(value => lastFrame = framer.FrameCount)
                .Subscribe(o);
            framer.Start(() => {
                if (cancelled) { return true; }
                if (framer.FrameCount - lastFrame > frameCount) {
                    disposable.Dispose();
                    disposable = null;
                    o.OnError(new TimeoutException());
                }
                return false;
            });
            return () => {
                cancelled = true;
                disposable?.Dispose();
            };
        });

    public static IObservable<TSource> TimeoutFrame<TSource>(this IObservable<TSource> source, long dueFrameCount, IObservable<TSource> other)
        => Observable.Create<TSource>(o => {
            var framer = Context<World>.Current!.GetAddon<SimulationFramer>();
            bool cancelled = false;
            long lastFrame = framer.FrameCount;
            object sync = new();
            var disposable = source
                .Do(value => lastFrame = framer.FrameCount)
                .Subscribe(o);
            framer.Start(() => {
                if (cancelled) { return true; }
                if (framer.FrameCount - lastFrame > dueFrameCount) {
                    disposable.Dispose();
                    disposable = other.Subscribe(o);
                }
                return false;
            });
            return () => {
                cancelled = true;
                disposable.Dispose();
            };
        });
    
    public static IObservable<TSource> TakeUntilDestroy<TSource>(this IObservable<TSource> source, EntityRef entity)
        => source.TakeUntil(_ => !entity.Valid);

    public static IObservable<TSource> RepeatUntilDestroy<TSource>(this IObservable<TSource> source, EntityRef entity)
        => Observable.Create<TSource>(o => {
            IDisposable? disposable = null;
            disposable = source.Repeat().Subscribe(
                value => {
                    if (!entity.Valid) {
                        disposable?.Dispose();
                        disposable = source.Subscribe();
                        return;
                    }
                    o.OnNext(value);
                },
                e => o.OnError(e),
                () => o.OnCompleted()
            );
            return () => {
                disposable?.Dispose();
            };
        });
}