namespace Nagule;

using Sia;

public interface IAssetManager<TAssetTemplate>
{
    EntityRef Acquire(TAssetTemplate template, AssetLife life = AssetLife.Persistent);
    EntityRef Acquire(TAssetTemplate template, in EntityRef referrer, AssetLife life = AssetLife.Automatic);

    bool TryGet(TAssetTemplate template, out EntityRef entity);
}