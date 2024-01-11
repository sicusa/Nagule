using System.Reactive.Linq;

namespace Nagule.Reactive;

using System.Reactive;
using System.Reactive.Linq;
using CommunityToolkit.HighPerformance;
using Sia;
        
public static partial class NaObservables
{
    public static IObservable<Unit> Interval(float interval)
        => interval <= 0
            ? EveryFrame
            : Observable.Create<Unit>(o => {
                var framer = Context<World>.Current!.GetAddon<SimulationFramer>();
                bool cancelled = false;
                var lastTime = framer.Time;
                framer.Start(() => {
                    if (cancelled) { return true; }
                    var time = framer.Time;
                    if (time - lastTime > interval) {
                        o.OnNext(Unit.Default);
                        lastTime = time;
                    }
                    return false;
                });
                return () => { cancelled = true; };
            });

    public static IObservable<Unit> Timer(float dueTime)
        => Observable.Create<Unit>(o => {
            var framer = Context<World>.Current!.GetAddon<SimulationFramer>();
            bool cancelled = false;
            var initTime = framer.Time;
            framer.Start(() => {
                if (cancelled) { return true; }
                if (framer.Time - initTime > dueTime) {
                    o.OnNext(Unit.Default);
                    o.OnCompleted();
                    return true;
                }
                return false;
            });
            return () => { cancelled = true; };
        });

    public static IObservable<Unit> TimerFrame(float dueTime, float period)
        => Observable.Create<Unit>(o => {
            var framer = Context<World>.Current!.GetAddon<SimulationFramer>();
            bool cancelled = false;
            var initTime = framer.Time;
            var lastTime = initTime;
            framer.Start(() => {
                if (cancelled) { return true; }
                var time = framer.Time;
                if (initTime > 0f && time - initTime > dueTime) {
                    o.OnNext(Unit.Default);
                    initTime = -1f;
                }
                else if (time - lastTime > period) {
                    lastTime = time;
                    o.OnNext(Unit.Default);
                }
                return false;
            });
            return () => { cancelled = true; };
        });

    public static IObservable<TSource> DelayFrameSubscription<TSource>(this IObservable<TSource> source, float delay)
        => Observable.Create<TSource>(o => {
            var framer = Context<World>.Current!.GetAddon<SimulationFramer>();
            bool cancelled = false;
            var initTime = framer.Time;
            IDisposable? disposable = null;
            object sync = new();
            framer.Start(() => {
                if (cancelled) { return true; }
                if (framer.Time - initTime > delay) {
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
    
    public static IObservable<TSource> DelayFrame<TSource>(this IObservable<TSource> source, float delay)
        => Observable.Create<TSource>(o => {
            var framer = Context<World>.Current!.GetAddon<SimulationFramer>();
            bool cancelled = false;
            bool delayFinished = false;
            var initTime = framer.Time;
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
                if (framer.Time - initTime > delay) {
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

    public static IObservable<TSource> Sample<TSource>(this IObservable<TSource> source, float period)
        => Observable.Create<TSource>(o => {
            var framer = Context<World>.Current!.GetAddon<SimulationFramer>();
            bool cancelled = false;
            var lastTime = framer.Time;
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
                var time = framer.Time;
                if (time - lastTime > period && hasValue) {
                    lock (sync)  {
                        o.OnNext(lastValue!);
                    }
                    lastTime = time;
                }
                return false;
            });
            return () => {
                cancelled = true;
                disposable.Dispose();
            };
        });
    
    public static IObservable<TimeInterval<TSource>> TimeInterval<TSource>(this IObservable<TSource> source)
        => Observable.Create<TimeInterval<TSource>>(o => {
            var framer = Context<World>.Current!.GetAddon<SimulationFramer>();
            var lastTime = framer.Time;
            return source
                .Select(value => {
                    var time = framer.Time;
                    var interval = framer.Time - lastTime;
                    lastTime = time;
                    return new TimeInterval<TSource>(value, TimeSpan.FromSeconds(interval));
                })
                .Subscribe(o);
        });
    
    public static IObservable<TSource> Batch<TSource>(this IObservable<TSource> source, float period)
        => Observable.Create<TSource>(o => {
            var framer = Context<World>.Current!.GetAddon<SimulationFramer>();
            var lastTime = framer.Time;
            var batch = new List<TSource>();
            var disposable = source.Subscribe(
                value => {
                    var time = framer.Time;
                    if (time - lastTime <= period) {
                        batch.Add(value);
                        return;
                    }
                    else {
                        lastTime = time;
                        foreach (ref var batchedValue in batch.AsSpan<TSource>()) {
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
    
    public static IObservable<TSource> Throttle<TSource>(this IObservable<TSource> source, float period)
        => Observable.Create<TSource>(o => {
            var framer = Context<World>.Current!.GetAddon<SimulationFramer>();
            bool cancelled = false;
            var lastTime = framer.Time;
            bool hasValue = false;
            TSource? lastValue = default;
            object sync = new();
            var disposable = source.Subscribe(
                value => {
                    lock (sync) { lastValue = value; }
                    lastTime = framer.Time;
                    hasValue = true;
                },
                e => o.OnError(e),
                () => o.OnCompleted());
            framer.Start(() => {
                if (cancelled) { return true; }
                if (hasValue && framer.Time - lastTime > period) {
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

    public static IObservable<TSource> ThrottleFirst<TSource>(this IObservable<TSource> source, float period)
        => Observable.Create<TSource>(o => {
            var framer = Context<World>.Current!.GetAddon<SimulationFramer>();
            bool cancelled = false;
            var lastTime = framer.Time - period;
            bool hasValue = false;
            TSource? lastValue = default;
            object sync = new();
            var disposable = source.Subscribe(
                value => {
                    if (framer.Time - lastTime <= period) {
                        return;
                    }
                    lock (sync) { lastValue = value; }
                    lastTime = framer.Time;
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

    public static IObservable<TSource> Timeout<TSource>(this IObservable<TSource> source, float dueTime)
        => Observable.Create<TSource>(o => {
            var framer = Context<World>.Current!.GetAddon<SimulationFramer>();
            bool cancelled = false;
            var lastTime = framer.Time;
            object sync = new();
            IDisposable? disposable = source
                .Subscribe(
                    value => {
                        lastTime = framer.Time;
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
                if (framer.Time - lastTime > dueTime) {
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

    public static IObservable<TSource> Timeout<TSource>(this IObservable<TSource> source, float dueTime, IObservable<TSource> other)
        => Observable.Create<TSource>(o => {
            var framer = Context<World>.Current!.GetAddon<SimulationFramer>();
            bool cancelled = false;
            float lastTime = framer.Time;
            object sync = new();
            var disposable = source
                .Subscribe(
                    value => {
                        lastTime = framer.Time;
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
                if (framer.Time - lastTime > dueTime) {
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