namespace Nagule;

public struct ResourceReferencers : IHashComponent
{
    public readonly HashSet<Guid> Ids = new();

    public ResourceReferencers() {}
}