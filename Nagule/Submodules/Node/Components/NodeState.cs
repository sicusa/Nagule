namespace Nagule;

using System.Collections.Immutable;
using Sia;

public record struct NodeState : IAssetState
{
    public readonly bool Loaded => FeaturesRaw != null;

    public readonly IReadOnlySet<EntityRef> Features =>
        (IReadOnlySet<EntityRef>?)FeaturesRaw ?? ImmutableHashSet<EntityRef>.Empty;

    internal HashSet<EntityRef>? FeaturesRaw;
    internal List<(EntityRef Entity, RFeatureBase Record)>? AssetFeatures;
}