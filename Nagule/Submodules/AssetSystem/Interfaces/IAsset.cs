namespace Nagule;

using Sia;

public interface IAsset<in TAssetRecord>
{
    abstract static EntityRef CreateEntity(World world, TAssetRecord record, AssetLife life = AssetLife.Automatic);
    abstract static EntityRef CreateEntity<TComponentBundle>(
        World world, TAssetRecord record, in TComponentBundle bundle, AssetLife life = AssetLife.Automatic)
        where TComponentBundle : struct, IComponentBundle;

    abstract static EntityRef CreateEntity(World world, TAssetRecord record, EntityRef parent, AssetLife life = AssetLife.Automatic);
}