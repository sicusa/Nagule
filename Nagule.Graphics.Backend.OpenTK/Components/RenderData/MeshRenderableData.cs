namespace Nagule.Graphics.Backend.OpenTK;

public struct MeshRenderableData : IPooledComponent
{
    public Dictionary<Guid, int> Entries = new();
    public MeshRenderableData() {}
}