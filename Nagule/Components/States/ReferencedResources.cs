namespace Nagule;

public struct ReferencedResources : IPooledComponent
{
    public readonly HashSet<Guid> Ids = new();
    public ReferencedResources() {}
}