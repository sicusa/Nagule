namespace Nagule.Graphics.Backend.OpenTK;

using Nagule.Graphics;

public struct MeshRenderableData : IPooledComponent
{
    public Dictionary<Guid, int> Entries = new();

    public MeshRenderableData() {}
}