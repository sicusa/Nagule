namespace Nagule;

using Sia;

public struct AssetBundle<TAsset> : IComponentBundle
    where TAsset : notnull, IAsset
{
    public Sid<Guid> Id;
    public Sid<Name> Name;
    public AssetMetadata Metadata;
    public TAsset Asset;
    public State State;
}

public static class AssetBundle
{
    public static AssetBundle<TAsset> Create<TAsset>(in TAsset asset, AssetLife life = AssetLife.Automatic)
        where TAsset : struct, IAsset
        => new() {
            Id = Sid.From(asset.Id ?? Guid.Empty),
            Name = Sid.From(new Name(asset.Name ?? "")),
            Metadata = new() {
                AssetLife = life,
                AssetType = typeof(TAsset)
            },
            Asset = asset
        };
}