namespace Nagule;

using Sia;

public abstract record RFeatureBase : AssetBase
{
    [SiaProperty(NoCommands = true)]
    public bool IsEnabled { get; init; } = true;
}