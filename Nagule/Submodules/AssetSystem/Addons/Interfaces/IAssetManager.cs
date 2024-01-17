namespace Nagule;

using Sia;

public interface IAssetManager<TAssetRecord>
{
    EntityRef Acquire(TAssetRecord record, AssetLife life = AssetLife.Persistent);
    EntityRef Acquire(TAssetRecord record, in EntityRef referrer, AssetLife life = AssetLife.Automatic);
}