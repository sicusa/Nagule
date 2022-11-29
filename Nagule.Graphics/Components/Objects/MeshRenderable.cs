namespace Nagule.Graphics;

using System.Runtime.Serialization;

[DataContract]
public struct MeshRenderable : IReactiveComponent
{
    public MeshResource Mesh;

    [DataMember] public bool IsVariant;
}