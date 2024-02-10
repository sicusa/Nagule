namespace Nagule;

using Sia;

public interface IAssetManagerBase<TAsset> : IAddon
{
    void AddStates(in EntityRef entity, in TAsset asset, ref DynEntityRef stateEntity);
    CancellationToken? DestroyState(in EntityRef entity, in TAsset asset, EntityRef stateEntity);

    void LoadAsset(in EntityRef entity, ref TAsset asset, EntityRef stateEntity);
    void UnloadAsset(in EntityRef entity, in TAsset asset, EntityRef stateEntity);
}