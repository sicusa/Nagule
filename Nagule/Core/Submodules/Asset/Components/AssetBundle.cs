namespace Nagule;

using Sia;

public struct AssetBundle<TAsset> : IComponentBundle
    where TAsset : notnull, IAsset
{
    public TAsset Asset;
    public AssetMetadata Metadata;
    public Sid<Guid> Id;
    public Sid<Name> Name;
}

public static class AssetBundle
{
    public static AssetBundle<TAsset> Create<TAsset>(in TAsset asset, AssetLife life = AssetLife.Automatic)
        where TAsset : struct, IAsset
        => new() {
            Asset = asset,
            Metadata = new() {
                AssetLife = life,
                AssetType = typeof(TAsset)
            },
            Id = Sid.From(asset.Id ?? Guid.Empty),
            Name = Sid.From(new Name(asset.Name ?? ""))
        };
}