namespace Nagule;

using Sia;

public interface IAssetManager<TAssetRecord>
{
    EntityRef this[TAssetRecord record] { get; }

    EntityRef Acquire(TAssetRecord record, AssetLife life = AssetLife.Persistent);
    EntityRef Acquire(TAssetRecord record, in EntityRef referrer, AssetLife life = AssetLife.Automatic);

    bool TryGet(TAssetRecord record, out EntityRef entity);
}