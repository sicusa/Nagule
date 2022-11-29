namespace Nagule;

public struct DirtyTransforms : ISingletonComponent
{
    public readonly HashSet<Guid> Ids = new();

    public DirtyTransforms() {}
}