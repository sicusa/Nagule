namespace Nagule;

using Sia;

public record struct Node3DState : IAssetState
{
    public readonly bool Loaded => FeaturesRaw != null;

    public readonly IReadOnlyList<EntityRef> Features => FeaturesRaw;

    internal List<EntityRef> FeaturesRaw;
}