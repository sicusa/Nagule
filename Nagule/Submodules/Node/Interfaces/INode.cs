namespace Nagule;

using System.Collections.Immutable;

public interface INode<TChildNode> : IAssetRecord
{
    ImmutableList<TChildNode> Children { get; }
    ImmutableList<RFeatureBase> Features { get; }
    ImmutableDictionary<string, Dyn> Metadata { get; }
}