namespace Nagule;

using Sia;

public struct AssetBundle<TAsset> : IComponentBundle
    where TAsset : notnull
{
    public AssetMetadata Metadata;
    public TAsset Asset;
    public State State;
}

public static class AssetBundle
{
    public static AssetBundle<TAsset> Create<TAsset>(
        in TAsset asset, AssetLife life = AssetLife.Automatic, IAssetRecord? record = null)
        where TAsset : struct
        => new() {
            Metadata = new() {
                AssetType = typeof(TAsset),
                AssetLife = life,
                AssetRecord = record
            },
            Asset = asset
        };
}