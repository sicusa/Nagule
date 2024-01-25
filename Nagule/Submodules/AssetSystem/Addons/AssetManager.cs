namespace Nagule;

using Sia;

public class AssetManager<TAsset, TAssetRecord>
    : AssetManagerBase<TAsset, TAssetRecord>
    where TAsset : struct, IAsset<TAssetRecord>
    where TAssetRecord : IAssetRecord
{
    protected sealed override void OnEntityAdded(in EntityRef entity)
        => LoadAsset(entity, ref entity.Get<TAsset>());

    protected sealed override void OnEntityRemoved(in EntityRef entity)
    {
        base.OnEntityRemoved(entity);
        UnloadAsset(entity, ref entity.Get<TAsset>());
    }

    protected virtual void LoadAsset(EntityRef entity, ref TAsset asset) {}
    protected virtual void UnloadAsset(EntityRef entity, ref TAsset asset) {}
}

public class AssetManager<TAsset, TAssetRecord, TAssetState>
    : AssetManagerBase<TAsset, TAssetRecord, TAssetState>
    where TAsset : struct, IAsset<TAssetRecord>
    where TAssetRecord : IAssetRecord
    where TAssetState : struct
{
    protected sealed override void OnEntityAdded(in EntityRef entity)
    {
        ref var asset = ref entity.Get<TAsset>();
        var stateEntity = CreateState(entity, asset);
        LoadAsset(entity, ref asset, stateEntity);
    }

    protected sealed override void OnEntityRemoved(in EntityRef entity)
    {
        base.OnEntityRemoved(entity);

        ref var asset = ref entity.Get<TAsset>();
        ref var state = ref entity.Get<State>();

        UnloadAsset(entity, ref asset, state.Entity);
        DestroyState(entity, asset, ref state);
    }

    protected virtual void LoadAsset(EntityRef entity, ref TAsset asset, EntityRef stateEntity) {}
    protected virtual void UnloadAsset(EntityRef entity, ref TAsset asset, EntityRef stateEntity) {}
}