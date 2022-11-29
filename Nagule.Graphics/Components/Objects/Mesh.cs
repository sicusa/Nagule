namespace Nagule.Graphics;

using System.Runtime.Serialization;

[DataContract]
public struct Mesh : IResourceObject<MeshResource>
{
    public MeshResource Resource { get; set; } = MeshResource.Empty;
    
    public Mesh() {}
}