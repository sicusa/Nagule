namespace Nagule;

using System.Collections.Immutable;
using Sia;

public abstract record RNodeBase<TChildNode> : AssetBase, INode<TChildNode>
{
    [SiaProperty(NoCommands = true)]
    public ImmutableList<TChildNode> Children { get; init; } = [];

    [SiaProperty(Item = "Feature")]
    public ImmutableList<RFeatureBase> Features { get; init; } = [];

    [SiaProperty(Item = "MetadataEntry")]
    public ImmutableDictionary<string, Dyn> Metadata { get; init; } = ImmutableDictionary<string, Dyn>.Empty;
}
