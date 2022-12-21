namespace Nagule.Graphics;

using System.Runtime.Serialization;

public enum MeshRenderMode
{
    Instance,
    Variant
}

[DataContract]
public struct MeshRenderable : IReactiveComponent
{
    public readonly Dictionary<Mesh, MeshRenderMode> Meshes = new();
    
    public MeshRenderable() {}
}