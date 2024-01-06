namespace Nagule;

public abstract record RFeatureAssetBase : AssetBase
{
    public bool Enabled { get; init; } = true;
}