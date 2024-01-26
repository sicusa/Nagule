namespace Nagule;

using System.Collections.Immutable;

public interface INode<TChildNode>
{
    ImmutableList<TChildNode> Children { get; }
    ImmutableList<RFeatureBase> Features { get; }
    ImmutableDictionary<string, Dyn> Metadata { get; }
}