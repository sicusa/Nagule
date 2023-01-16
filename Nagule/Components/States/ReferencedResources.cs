namespace Nagule;

public struct ReferencedResources<TResource> : IPooledComponent
    where TResource : IResource
{
    public readonly HashSet<Guid> Ids = new();

    public ReferencedResources() {}
}