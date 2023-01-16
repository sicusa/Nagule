namespace Nagule;

public struct ResourceReferencers : IPooledComponent
{
    public readonly HashSet<Guid> Ids = new();

    public ResourceReferencers() {}
}