namespace Nagule;

using Sia;

public record struct Node3DState : IAssetState
{
    public readonly bool Loaded => Features != null;

    public List<EntityRef> Features { get; internal set; }
}