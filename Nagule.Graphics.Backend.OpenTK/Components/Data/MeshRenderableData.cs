namespace Nagule.Graphics.Backend.OpenTK;

public struct MeshRenderableData : IHashComponent
{
    public Dictionary<uint, int> Entries = new();
    public MeshRenderableData() {}
}