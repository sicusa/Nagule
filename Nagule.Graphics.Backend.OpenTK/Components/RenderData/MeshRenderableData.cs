namespace Nagule.Graphics.Backend.OpenTK;

public struct MeshRenderableData : IHashComponent
{
    public Dictionary<Guid, int> Entries = new();
    public MeshRenderableData() {}
}