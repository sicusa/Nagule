namespace Nagule.Graphics;

using System.Numerics;

public record NodeResource : IResource
{
    public string Name = "";
    public Vector3 Position;
    public Quaternion Rotation;
    public Vector3 Scale;
    public MeshResource[]? Meshes;
    public LightResourceBase[]? Lights;
    public NodeResource[]? Children;
    public readonly Dictionary<string, object> Metadata = new();
}