namespace Nagule.Reactive;

using System.Reactive;
using System.Reactive.Linq;
using CommunityToolkit.HighPerformance;
using Sia;
        
public record struct FrameInterval<TSource>(TSource Value, long Interval);

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

    public static IObservable<Unit> IntervalFrame(long interval)
        => interval <= 0
            ? EveryFrame
            : Observable.Create<Unit>(o => {
                var framer = Context<World>.Current!.GetAddon<SimulationFramer>();
                bool cancelled = false;
                long counter = 0;
                framer.Start(() => {
                    if (cancelled) { return true; }
                    if (++counter > interval) {
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
    
    public static IObservable<Unit> TimerFrame(long dueFrameCount, long period)
        => Observable.Create<Unit>(o => {
            var framer = Context<World>.Current!.GetAddon<SimulationFramer>();
            bool cancelled = false;
            long initFrame = framer.FrameCount;
            long lastFrame = initFrame;
            framer.Start(() => {
                if (cancelled) { return true; }
                var frame = framer.FrameCount;
                if (initFrame > 0 && frame - initFrame > dueFrameCount) {
                    o.OnNext(Unit.Default);
                    initFrame = -1;
                }
                else if (frame - lastFrame > period) {
                    lastFrame = frame;
                    o.OnNext(Unit.Default);
                }
                return false;
            });
            return () => { cancelled = true; };
        });

    public static IObservable<TSource> DelayFrameSubscription<TSource>(this IObservable<TSource> source, long delay)
        => Observable.Create<TSource>(o => {
            var framer = Context<World>.Current!.GetAddon<SimulationFramer>();
            bool cancelled = false;
            long counter = 0;
            IDisposable? disposable = null;
            object sync = new();
            framer.Start(() => {
                if (cancelled) { return true; }
                if (++counter > delay) {
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
    
    public static IObservable<TSource> DelayFrame<TSource>(this IObservable<TSource> source, long delay)
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
                if (framer.FrameCount - initFrame > delay) {
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

    public static IObservable<TSource> SampleFrame<TSource>(this IObservable<TSource> source, long period)
        => Observable.Create<TSource>(o => {
            var framer = Context<World>.Current!.GetAddon<SimulationFramer>();
            bool cancelled = false;
            long counter = 0;
            bool hasValue = false;
            TSource? lastValue = default;
            object sync = new();
            var disposable = source.Subscribe(
                value => {
                    lock (sync) { lastValue = value; }
                    hasValue = true;
                },
                e => o.OnError(e),
                () => o.OnCompleted());
            framer.Start(() => {
                if (cancelled) { return true; }
                if (++counter > period && hasValue) {
                    lock (sync)  {
                        o.OnNext(lastValue!);
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
    
    public static IObservable<TSource> BatchFrame<TSource>(this IObservable<TSource> source, long period)
        => Observable.Create<TSource>(o => {
            var framer = Context<World>.Current!.GetAddon<SimulationFramer>();
            long lastFrame = framer.FrameCount;
            var batch = new List<TSource>();
            var disposable = source.Subscribe(
                value => {
                    long frame = framer.FrameCount;
                    if (frame - lastFrame <= period) {
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
    
    public static IObservable<TSource> ThrottleFrame<TSource>(this IObservable<TSource> source, long period)
        => Observable.Create<TSource>(o => {
            var framer = Context<World>.Current!.GetAddon<SimulationFramer>();
            bool cancelled = false;
            long lastFrame = framer.FrameCount;
            bool hasValue = false;
            TSource? lastValue = default;
            object sync = new();
            var disposable = source.Subscribe(
                value => {
                    lock (sync) { lastValue = value; }
                    lastFrame = framer.FrameCount;
                    hasValue = true;
                },
                e => o.OnError(e),
                () => o.OnCompleted());
            framer.Start(() => {
                if (cancelled) { return true; }
                if (hasValue && framer.FrameCount - lastFrame > period) {
                    lock (sync) { o.OnNext(lastValue!); }
                    hasValue = false;
                }
                return false;
            });
            return () => {
                cancelled = true;
                disposable.Dispose();
            };
        });

    public static IObservable<TSource> ThrottleFirstFrame<TSource>(this IObservable<TSource> source, long period)
        => Observable.Create<TSource>(o => {
            var framer = Context<World>.Current!.GetAddon<SimulationFramer>();
            bool cancelled = false;
            long lastFrame = framer.FrameCount - period;
            bool hasValue = false;
            TSource? lastValue = default;
            object sync = new();
            var disposable = source.Subscribe(
                value => {
                    if (framer.FrameCount - lastFrame <= period) {
                        return;
                    }
                    lock (sync) { lastValue = value; }
                    lastFrame = framer.FrameCount;
                    hasValue = true;
                },
                e => o.OnError(e),
                () => o.OnCompleted());
            framer.Start(() => {
                if (cancelled) { return true; }
                if (hasValue) {
                    lock (sync) { o.OnNext(lastValue!); }
                    hasValue = false;
                }
                return false;
            });
            return () => {
                cancelled = true;
                disposable?.Dispose();
            };
        });

    public static IObservable<TSource> TimeoutFrame<TSource>(this IObservable<TSource> source, long dueFrameCount)
        => Observable.Create<TSource>(o => {
            var framer = Context<World>.Current!.GetAddon<SimulationFramer>();
            bool cancelled = false;
            long lastFrame = framer.FrameCount;
            object sync = new();
            IDisposable? disposable = source
                .Subscribe(
                    value => {
                        lastFrame = framer.FrameCount;
                        o.OnNext(value);
                    },
                    e => {
                        lock (sync) {
                            cancelled = true;
                            o.OnError(e);
                        }
                    },
                    () => {
                        lock (sync) {
                            cancelled = true;
                            o.OnCompleted();
                        }
                    }
                );
            framer.Start(() => {
                if (cancelled) { return true; }
                if (framer.FrameCount - lastFrame > dueFrameCount) {
                    lock (sync) {
                        if (!cancelled) {
                            disposable.Dispose();
                            disposable = null;
                            o.OnError(new TimeoutException());
                        }
                        return true;
                    }
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
                .Subscribe(
                    value => {
                        lastFrame = framer.FrameCount;
                        o.OnNext(value);
                    },
                    e => {
                        lock (sync) {
                            cancelled = true;
                            o.OnError(e);
                        }
                    },
                    () => {
                        lock (sync) {
                            cancelled = true;
                            o.OnCompleted();
                        }
                    }
                );
            framer.Start(() => {
                if (cancelled) { return true; }
                if (framer.FrameCount - lastFrame > dueFrameCount) {
                    lock (sync) {
                        if (!cancelled) {
                            disposable.Dispose();
                            disposable = other.Subscribe(o);
                        }
                        return true;
                    }
                }
                return false;
            });
            return () => {
                cancelled = true;
                disposable.Dispose();
            };
        });
}