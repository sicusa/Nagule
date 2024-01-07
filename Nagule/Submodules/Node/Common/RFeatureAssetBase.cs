namespace Nagule;

public abstract record RFeatureBase : AssetBase
{
    public bool Enabled { get; init; } = true;
}