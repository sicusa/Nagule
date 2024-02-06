namespace Nagule;

using System.Collections.Immutable;

public interface INode<TNodeRecord> : IAsset<TNodeRecord>
    where TNodeRecord : IAssetRecord
{
    bool IsEnabled { get; }
    ImmutableList<RFeatureBase> Features { get; }
    ImmutableDictionary<string, Dyn> Metadata { get; }
}