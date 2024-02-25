namespace Nagule;

using Sia;

public abstract record RFeatureBase : AssetBase
{
    [Sia(NoCommands = true)]
    public bool IsEnabled { get; init; } = true;
}