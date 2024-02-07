namespace Nagule;

using Sia;

public struct AssetState(DynEntityRef entity)
{
    public bool IsLocked { get; internal set; }
    internal DynEntityRef Entity = entity;
}