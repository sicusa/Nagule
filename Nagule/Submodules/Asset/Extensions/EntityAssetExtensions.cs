namespace Nagule;

using Sia;

public static class EntityAssetExtensions
{
    public static void ReferAsset(this EntityRef entity, in EntityRef target)
        => entity.Modify(new AssetMetadata.Refer(target));

    public static void UnreferAsset(this EntityRef entity, in EntityRef asset)
        => entity.Modify(new AssetMetadata.Unrefer(asset));
}