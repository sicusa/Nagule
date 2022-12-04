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
    public readonly Dictionary<MeshResource, MeshRenderMode> Meshes = new();
    
    public MeshRenderable() {}
}