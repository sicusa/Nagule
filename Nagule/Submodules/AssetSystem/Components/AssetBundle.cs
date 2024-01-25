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
    public static AssetBundle<TAsset> Create<TAsset>(in TAsset asset, AssetLife life = AssetLife.Automatic)
        where TAsset : struct
        => new() {
            Metadata = new() {
                AssetLife = life,
                AssetType = typeof(TAsset)
            },
            Asset = asset
        };
}