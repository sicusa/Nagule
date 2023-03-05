namespace Nagule;

public struct ReferencedResources : IHashComponent
{
    public readonly HashSet<Guid> Ids = new();
    public ReferencedResources() {}
}