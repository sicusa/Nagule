namespace Nagule.Graphics;

using System.Runtime.Serialization;
using System.Collections.Immutable;

public enum MeshRenderMode
{
    Instance,
    Variant
}

[DataContract]
public struct MeshRenderable : IReactiveComponent
{
    public ImmutableDictionary<MeshResource, MeshRenderMode> Meshes =
        ImmutableDictionary<MeshResource, MeshRenderMode>.Empty;
    
    public MeshRenderable() {}
}