namespace Nagule;

using System.Collections.Immutable;
using Sia;

public abstract record RNodeBase<TNode> : AssetBase
    where TNode : RNodeBase<TNode>
{
    public bool IsEnabled { get; init; } = true;

    [Sia(NoCommands = true)]
    public ImmutableList<TNode> Children { get; init; } = [];

    [Sia(Item = "Feature")]
    public ImmutableList<RFeatureBase> Features { get; init; } = [];

    [Sia(Item = "MetadataEntry")]
    public ImmutableDictionary<string, Dyn> Metadata { get; init; } = ImmutableDictionary<string, Dyn>.Empty;
}
