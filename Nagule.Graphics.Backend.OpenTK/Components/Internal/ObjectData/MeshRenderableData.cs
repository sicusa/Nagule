namespace Nagule.Graphics.Backend.OpenTK.Graphics;

using Nagule.Graphics;

public struct MeshRenderableData : IPooledComponent
{
    public Dictionary<Guid, int> Entries = new();

    public MeshRenderableData() {}
}