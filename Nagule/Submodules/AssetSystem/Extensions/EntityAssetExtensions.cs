namespace Nagule;

using System.Diagnostics.CodeAnalysis;
using Sia;

public static class EntityAssetExtensions
{
    public static void Refer(this EntityRef entity, in EntityRef target)
        => entity.Modify(new AssetMetadata.Refer(target));

    public static void Unrefer(this EntityRef entity, in EntityRef asset)
        => entity.Modify(new AssetMetadata.Unrefer(asset));
    
    public static EntityRef? FindReferrer<TAsset>(this EntityRef entity, bool recurse = false)
        where TAsset : struct
        => entity.Get<AssetMetadata>().FindReferrer<TAsset>(recurse);

    public static EntityRef GetReferrer<TAsset>(this EntityRef entity, bool recurse = false)
        where TAsset : struct
        => entity.Get<AssetMetadata>().FindReferrer<TAsset>(recurse)
            ?? ThrowAssetNotFound<TAsset>();

    public static IEnumerable<EntityRef> FindReferrers<TAsset>(this EntityRef entity, bool recurse = false)
        where TAsset : struct
        => entity.Get<AssetMetadata>().FindReferrers<TAsset>(recurse);

    public static EntityRef? FindReferred<TAsset>(this EntityRef entity, bool recurse = false)
        where TAsset : struct
        => entity.Get<AssetMetadata>().FindReferred<TAsset>(recurse);

    public static EntityRef GetReferred<TAsset>(this EntityRef entity, bool recurse = false)
        where TAsset : struct
        => entity.Get<AssetMetadata>().FindReferred<TAsset>(recurse)
            ?? ThrowAssetNotFound<TAsset>();

    public static IEnumerable<EntityRef> FindAllReferred<TAsset>(this EntityRef entity, bool recurse = false)
        where TAsset : struct
        => entity.Get<AssetMetadata>().FindAllReferred<TAsset>(recurse);
    
    [DoesNotReturn]
    private static EntityRef ThrowAssetNotFound<TAsset>()
        => throw new AssetNotFoundException("Asset not found: " + typeof(TAsset));
}