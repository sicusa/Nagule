namespace Nagule;

public abstract record FeatureAssetBase : AssetBase
{
    public bool Enabled { get; init; }
}