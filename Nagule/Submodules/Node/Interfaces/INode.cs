namespace Nagule;

using System.Collections.Immutable;

public interface INode<TChildNode> : IAsset
{
    ImmutableList<TChildNode> Children { get; }
    ImmutableList<RFeatureAssetBase> Features { get; }
    ImmutableDictionary<string, Dyn> Metadata { get; }
}