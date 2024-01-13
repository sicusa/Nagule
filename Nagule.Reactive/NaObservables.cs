namespace Nagule.Reactive;

using System.Numerics;
using System.Reactive;
using System.Reactive.Linq;
using CommunityToolkit.HighPerformance;
using Sia;

public record struct EventPair<TEvent>(EntityRef Entity, TEvent Event);

public record struct DegreeInterval<TSource, TDegree>(
    TSource Value, TDegree Interval);

public delegate TDegree DegreeProvider<TDegree>(SimulationFramer framer);
        
public static partial class NaObservables
{
    public static IDisposable Subscribe<TCommand>(this IObservable<TCommand> source, EntityRef entity)
        where TCommand : ICommand
        => source.Subscribe(cmd => entity.Modify(cmd));

    public static IObservable<EventPair<TEvent>> FromEvent<TEvent>()
        where TEvent : IEvent
        => Observable.Create<EventPair<TEvent>>(o => {
            var dispatcher = Context<World>.Current!.Dispatcher;
            bool cancelled = false;
            object sync = new();
            bool onEvent(in EntityRef entity, in TEvent e) {
                lock (sync) {
                    if (cancelled) { return true; }
                    o.OnNext(new(entity, e));
                }
                return false;
            }
            dispatcher.Listen<TEvent>(onEvent);
            return () => {
                lock (sync) {
                    cancelled = true;
                }
            };
        });

    public static IObservable<TSource> TakeUntilDestroy<TSource>(this IObservable<TSource> source, EntityRef entity)
        => source.TakeUntil(_ => !entity.Valid);

    public static IObservable<TSource> RepeatUntilDestroy<TSource>(this IObservable<TSource> source, EntityRef entity)
        => source.Repeat().TakeUntil(_ => !entity.Valid);

    public static IObservable<Unit> Interval<TDegree>(
        TDegree interval, DegreeProvider<TDegree> provider)
        where TDegree :
            IAdditiveIdentity<TDegree, TDegree>,
            IComparisonOperators<TDegree, TDegree, bool>,
            ISubtractionOperators<TDegree, TDegree, TDegree>
        => interval <= TDegree.AdditiveIdentity
            ? EveryFrame
            : Observable.Create<Unit>(o => {
                var framer = Context<World>.Current!.GetAddon<SimulationFramer>();
                var lastDegree = provider(framer);
                bool cancelled = false;
                framer.Start(() => {
                    if (cancelled) { return true; }
                    var degree = provider(framer);
                    if (degree - lastDegree > interval) {
                        o.OnNext(Unit.Default);
                        lastDegree = degree;
                    }
                    return false;
                });
                return () => cancelled = true;
            });

    public static IObservable<Unit> Timer<TDegree>(
        TDegree dueTime, DegreeProvider<TDegree> provider)
        where TDegree :
            IComparisonOperators<TDegree, TDegree, bool>,
            ISubtractionOperators<TDegree, TDegree, TDegree>
        => Observable.Create<Unit>(o => {
            var framer = Context<World>.Current!.GetAddon<SimulationFramer>();
            var lastDegree = provider(framer);
            bool cancelled = false;
            framer.Start(() => {
                if (cancelled) { return true; }
                var degree = provider(framer);
                if (degree - lastDegree > dueTime) {
                    o.OnNext(Unit.Default);
                    o.OnCompleted();
                    return true;
                }
                return false;
            });
            return () => { cancelled = true; };
        });
    
    public static IObservable<Unit> Timer<TDegree>(
        TDegree dueTime, TDegree period, DegreeProvider<TDegree> provider)
        where TDegree :
            IComparisonOperators<TDegree, TDegree, bool>,
            ISubtractionOperators<TDegree, TDegree, TDegree>
        => Observable.Create<Unit>(o => {
            var framer = Context<World>.Current!.GetAddon<SimulationFramer>();
            var initDegree = provider(framer);
            var lastDegree = initDegree;
            bool cancelled = false;
            framer.Start(() => {
                if (cancelled) { return true; }
                var degree = provider(framer);
                if (degree - initDegree > dueTime) {
                    o.OnCompleted();
                    return true;
                }
                if (degree - lastDegree > period) {
                    o.OnNext(Unit.Default);
                    lastDegree = degree;
                }
                return false;
            });
            return () => cancelled = true;;
        });

    public static IObservable<TSource> DelaySubscription<TSource, TDegree>(
        this IObservable<TSource> source, TDegree delay, DegreeProvider<TDegree> provider)
        where TDegree :
            IComparisonOperators<TDegree, TDegree, bool>,
            ISubtractionOperators<TDegree, TDegree, TDegree>
        => Observable.Create<TSource>(o => {
            var framer = Context<World>.Current!.GetAddon<SimulationFramer>();
            var initDegree = provider(framer);
            bool cancelled = false;
            IDisposable? disposable = null;
            object sync = new();
            framer.Start(() => {
                if (cancelled) { return true; }
                if (provider(framer) - initDegree <= delay) {
                    return false;
                }
                lock (sync) {
                    if (cancelled) { return true; }
                    disposable = source.Subscribe(o);
                }
                return true;
            });
            return () => {
                lock (sync) {
                    cancelled = true;
                    disposable?.Dispose();
                }
            };
        });

    public static IObservable<DegreeInterval<TSource, TDegree>> WithInterval<TSource, TDegree>(
        this IObservable<TSource> source, DegreeProvider<TDegree> provider)
        where TDegree : ISubtractionOperators<TDegree, TDegree, TDegree>
        => Observable.Create<DegreeInterval<TSource, TDegree>>(o => {
            var framer = Context<World>.Current!.GetAddon<SimulationFramer>();
            var lastDegree = provider(framer);
            return source
                .Select(value => {
                    var degree = provider(framer);
                    var interval = degree - lastDegree;
                    lastDegree = degree;
                    return new DegreeInterval<TSource, TDegree>(value, interval);
                })
                .Subscribe(o);
        });

    private static Action Connect<TSource>(
        IObservable<TSource> source, IObserver<TSource> target,
        Action<SimulationFramer, TSource, object> receiver,
        Predicate<SimulationFramer> predicate,
        Predicate<SimulationFramer> handler)
        => Connect(source, target, static framer => {}, receiver, predicate, handler);
    
    private static Action Connect<TSource>(
        IObservable<TSource> source, IObserver<TSource> target,
        Action<SimulationFramer> initializer,
        Action<SimulationFramer, TSource, object> receiver,
        Predicate<SimulationFramer> predicate,
        Predicate<SimulationFramer> handler)
    {
        var framer = Context<World>.Current!.GetAddon<SimulationFramer>();
        initializer(framer);

        bool cancelled = false;
        bool completed = false;
        object sync = new();

        var disposable = source.Subscribe(
            value => receiver(framer, value, sync),
            e => {
                lock (sync) {
                    if (cancelled) { return; }
                    cancelled = true;
                    target.OnError(e);
                }
            },
            () => {
                completed = true;
            });

        framer.Start(() => {
            if (cancelled) { return true; }
            if (completed) {
                target.OnCompleted();
                return true;
            }
            if (!predicate(framer)) { return false; }
            lock (sync) {
                if (cancelled || handler(framer)) {
                    return true;
                }
            }
            return false;
        });
        
        return () => {
            lock (sync) {
                cancelled = true;
                disposable.Dispose();
            }
        };
    }
    
    public static IObservable<TSource> Delay<TSource, TDegree>(
        this IObservable<TSource> source, TDegree delay, DegreeProvider<TDegree> provider)
        where TDegree :
            IComparisonOperators<TDegree, TDegree, bool>,
            ISubtractionOperators<TDegree, TDegree, TDegree>
        => Observable.Create<TSource>(o => {
            var framer = Context<World>.Current!.GetAddon<SimulationFramer>();
            var queue = new SwappingQueue<TSource>();
            var initDegree = provider(framer);
            bool delayFinished = false;
            return Connect(source, o,
                (framer, value, sync) => queue.Add(value),
                framer => {
                    if (delayFinished) { return true; }
                    if (provider(framer) - initDegree <= delay) {
                        return false;
                    }
                    delayFinished = true;
                    return true;
                },
                framer => {
                    var values = queue.Swap();
                    foreach (ref var value in values.AsSpan()) {
                        o.OnNext(value);
                    }
                    values.Clear();
                    return false;
                }
            );
        });

    public static IObservable<TSource> Sample<TSource, TDegree>(
        this IObservable<TSource> source, TDegree period, DegreeProvider<TDegree> provider)
        where TDegree :
            IComparisonOperators<TDegree, TDegree, bool>,
            ISubtractionOperators<TDegree, TDegree, TDegree>
        => Observable.Create<TSource>(o => {
            TSource? lastValue = default;
            bool hasValue = false;
            var lastDegree = default(TDegree)!;
            return Connect(source, o,
                framer => lastDegree = provider(framer),
                (framer, value, sync) => {
                    lock (sync) {
                        lastValue = value;
                        hasValue = true;
                    }
                },
                framer => {
                    var degree = provider(framer);
                    if (degree - lastDegree <= period) {
                        return false;
                    }
                    lastDegree = degree;
                    return hasValue;
                },
                framer => {
                    o.OnNext(lastValue!);
                    lastValue = default;
                    return false;
                }
            );
        });
    
    public static IObservable<TSource> Batch<TSource, TDegree>(
        this IObservable<TSource> source, TDegree period, DegreeProvider<TDegree> provider)
        where TDegree :
            IComparisonOperators<TDegree, TDegree, bool>,
            ISubtractionOperators<TDegree, TDegree, TDegree>
        => Observable.Create<TSource>(o => {
            var queue = new SwappingQueue<TSource>();
            var lastDegree = default(TDegree)!;
            return Connect(source, o,
                framer => lastDegree = provider(framer),
                (framer, value, sync) => queue.Add(value),
                framer => {
                    var degree = provider(framer);
                    if (degree - lastDegree <= period) {
                        return false;
                    }
                    lastDegree = degree;
                    return true;
                },
                framer => {
                    var values = queue.Swap();
                    foreach (ref var value in values.AsSpan()) {
                        o.OnNext(value);
                    }
                    values.Clear();
                    return false;
                });
        });
    
    public static IObservable<TSource> Throttle<TSource, TDegree>(
        this IObservable<TSource> source, TDegree period, DegreeProvider<TDegree> provider)
        where TDegree :
            IComparisonOperators<TDegree, TDegree, bool>,
            ISubtractionOperators<TDegree, TDegree, TDegree>
        => Observable.Create<TSource>(o => {
            TSource? lastValue = default;
            bool hasValue = false;
            var lastDegree = default(TDegree)!;
            return Connect(source, o,
                framer => lastDegree = provider(framer),
                (framer, value, sync) => {
                    lock (sync) {
                        lastValue = value;
                        hasValue = true;
                    }
                },
                framer => {
                    if (!hasValue) {
                        return false;
                    }
                    var degree = provider(framer);
                    if (degree - lastDegree <= period) {
                        return false;
                    }
                    lastDegree = degree;
                    return true;
                },
                framer => {
                    o.OnNext(lastValue!);
                    lastValue = default;
                    hasValue = false;
                    return false;
                });
        });

    public static IObservable<TSource> ThrottleFirst<TSource, TDegree>(
        this IObservable<TSource> source, TDegree period, DegreeProvider<TDegree> provider)
        where TDegree :
            IComparisonOperators<TDegree, TDegree, bool>,
            ISubtractionOperators<TDegree, TDegree, TDegree>
        => Observable.Create<TSource>(o => {
            TSource? lastValue = default;
            bool hasValue = false;
            var lastDegree = default(TDegree)!;
            return Connect(source, o,
                framer => lastDegree = provider(framer) - period,
                (framer, value, sync) => {
                    lock (sync) {
                        lastValue = value;
                        hasValue = true;
                    }
                },
                framer => {
                    if (!hasValue) {
                        return false;
                    }
                    var degree = provider(framer);
                    if (degree - lastDegree <= period) {
                        return false;
                    }
                    lastDegree = degree;
                    return true;
                },
                framer => {
                    o.OnNext(lastValue!);
                    lastValue = default;
                    hasValue = false;
                    return false;
                });
        });

    public static IObservable<TSource> Timeout<TSource, TDegree>(
        this IObservable<TSource> source, TDegree dueTime, DegreeProvider<TDegree> provider)
        where TDegree :
            IComparisonOperators<TDegree, TDegree, bool>,
            ISubtractionOperators<TDegree, TDegree, TDegree>
        => Observable.Create<TSource>(o => {
            TSource? lastValue = default;
            bool hasValue = false;
            bool timeout = false;
            var lastDegree = default(TDegree)!;
            return Connect(source, o,
                framer => lastDegree = provider(framer),
                (framer, value, sync) => {
                    lock (sync) {
                        lastValue = value;
                        hasValue = true;
                    }
                },
                framer => {
                    var degree = provider(framer);
                    if (degree - lastDegree > dueTime) {
                        timeout = true;
                        return true;
                    }
                    lastDegree = degree;
                    return hasValue;
                },
                framer => {
                    if (timeout) {
                        o.OnError(new TimeoutException());
                        return true;
                    }
                    o.OnNext(lastValue!);
                    lastValue = default;
                    hasValue = false;
                    return false;
                });
        });

    public static IObservable<TSource> Timeout<TSource, TDegree>(
        this IObservable<TSource> source, TDegree dueTime, IObservable<TSource> other,
        DegreeProvider<TDegree> provider)
        where TDegree :
            IComparisonOperators<TDegree, TDegree, bool>,
            ISubtractionOperators<TDegree, TDegree, TDegree>
        => Observable.Create<TSource>(o => {
            var framer = Context<World>.Current!.GetAddon<SimulationFramer>();
            bool cancelled = false;
            bool completed = false;
            TSource? lastValue = default;
            bool hasValue = false;
            var lastDegree = default(TDegree)!;
            object sync = new();
            IDisposable disposable;
            disposable = source
                .Subscribe(
                    value => {
                        lock (sync) {
                            lastValue = value;
                            hasValue = true;
                        }
                    },
                    e => {
                        lock (sync) {
                            cancelled = true;
                            o.OnError(e);
                        }
                    },
                    () => {
                        completed = true;
                    }
                );
            framer.Start(() => {
                if (cancelled) { return true; }
                if (completed) {
                    o.OnCompleted();
                    return true;
                }
                var degree = provider(framer);
                if (degree - lastDegree > dueTime) {
                    lock (sync) {
                        if (cancelled) { return true; }
                        disposable.Dispose();
                        disposable = other.Subscribe(o);
                    }
                }
                if (!hasValue) { return false; }
                lock (sync) {
                    if (cancelled) { return true; }
                    o.OnNext(lastValue!);
                    lastValue = default;
                    hasValue = false;
                }
                lastDegree = degree;
                return false;
            });
            return () => {
                lock (sync) {
                    cancelled = true;
                    disposable.Dispose();
                }
            };
        });
}