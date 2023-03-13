namespace Nagule;

public struct ReferencedResources : IHashComponent
{
    public readonly HashSet<uint> Ids = new();
    public ReferencedResources() {}
}