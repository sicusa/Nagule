namespace Nagule;

using System.Runtime.CompilerServices;
using Sia;

public static class EntityAssetStateExtensions
{
    public static EntityRef GetStateEntity(this EntityRef entity)
        => entity.Get<AssetState>().Entity.Current;

    public static void AddState<TState>(this EntityRef entity, in TState initial = default!)
    {
        ref var state = ref entity.Get<AssetState>();
        if (state.IsLocked) {
            throw new InvalidOperationException("Failed to add new state component: state has been locked");
        }
        state.Entity.Add(initial);
    }

    public static ref TState GetState<TState>(this EntityRef entity)
        => ref entity.Get<AssetState>().Entity.Get<TState>();

    public static ref TState GetStateOrNullRef<TState>(this EntityRef entity)
    {
        ref var state = ref entity.GetOrNullRef<AssetState>();
        if (Unsafe.IsNullRef(ref state) || !state.Entity.Valid) {
            return ref Unsafe.NullRef<TState>();
        }
        return ref state.Entity.GetOrNullRef<TState>();
    }
}