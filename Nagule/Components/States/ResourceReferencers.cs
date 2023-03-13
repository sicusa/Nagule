namespace Nagule;

public struct ResourceReferencers : IHashComponent
{
    public readonly HashSet<uint> Ids = new();

    public ResourceReferencers() {}
}