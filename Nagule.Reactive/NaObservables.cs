
namespace Nagule.Reactive;

using System.Reactive.Linq;
using Sia;

public record struct EventPair<TEvent>(EntityRef Entity, TEvent Event);
        
public static partial class NaObservables
{
    public static IDisposable Subscribe<TCommand>(this IObservable<TCommand> source, EntityRef entity)
        where TCommand : ICommand
        => source.Subscribe(cmd => entity.Modify(cmd));

    public static IObservable<EventPair<TEvent>> FromEvent<TEvent>()
        where TEvent : IEvent
        => Observable.Create<EventPair<TEvent>>(o => {
            bool cancelled = false;
            var dispatcher = Context<World>.Current!.Dispatcher;
            bool onEvent(in EntityRef entity, in TEvent e) {
                if (cancelled) { return true; }
                o.OnNext(new(entity, e));
                return false;
            }
            dispatcher.Listen<TEvent>(onEvent);
            return () => cancelled = true;
        });

    public static IObservable<TSource> TakeUntilDestroy<TSource>(this IObservable<TSource> source, EntityRef entity)
        => source.TakeUntil(_ => !entity.Valid);

    public static IObservable<TSource> RepeatUntilDestroy<TSource>(this IObservable<TSource> source, EntityRef entity)
        => source.Repeat().TakeUntil(_ => !entity.Valid);
}