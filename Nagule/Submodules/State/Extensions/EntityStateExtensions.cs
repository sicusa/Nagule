namespace Nagule;

using System.Runtime.CompilerServices;
using Sia;

public static class EntityStateExtensions
{
    public static EntityRef GetStateEntity(this EntityRef entity)
        => entity.Get<State>().Entity;

    public static ref TState GetState<TState>(this EntityRef entity)
        => ref entity.Get<State>().Entity.Get<TState>();

    public static ref TState GetStateOrNullRef<TState>(this EntityRef entity)
    {
        ref var state = ref entity.GetOrNullRef<State>();
        if (Unsafe.IsNullRef(ref state) || !state.Entity.Valid) {
            return ref Unsafe.NullRef<TState>();
        }
        return ref state.Entity.GetOrNullRef<TState>();
    }
}