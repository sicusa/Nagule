namespace Nagule;

using System.Runtime.CompilerServices;
using Sia;

public static class EntityStateExtensions
{
    public static ref TState GetState<TState>(this EntityRef entity)
        => ref entity.Get<State>().Entity.Get<TState>();

    public static ref TState GetStateOrNullRef<TState>(this EntityRef entity)
    {
        var stateEntity = entity.Get<State>().Entity;
        if (stateEntity == default) {
            return ref Unsafe.NullRef<TState>();
        }
        return ref stateEntity.GetOrNullRef<TState>();
    }
}