using Sia;

namespace Nagule;

public interface IAsset
{
    string? Name { get; }
    Guid? Id { get; }
}

public interface IAsset<TAssetTemplate> : IAsset
{
    abstract static EntityRef CreateEntity(World world, TAssetTemplate template, AssetLife life = AssetLife.Automatic);
    abstract static EntityRef CreateEntity<TComponentBundle>(
        World world, TAssetTemplate template, in TComponentBundle bundle, AssetLife life = AssetLife.Automatic)
        where TComponentBundle : struct, IComponentBundle;

    abstract static EntityRef CreateEntity(World world, TAssetTemplate template, EntityRef referrer, AssetLife life = AssetLife.Automatic);
}