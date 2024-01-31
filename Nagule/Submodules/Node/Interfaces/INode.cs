namespace Nagule;

using System.Collections.Immutable;

public interface INode
{
    bool IsEnabled { get; }
    ImmutableList<RFeatureBase> Features { get; }
    ImmutableDictionary<string, Dyn> Metadata { get; }
}