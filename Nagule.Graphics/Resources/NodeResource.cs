namespace Nagule.Graphics;

using System.Numerics;

public record GraphNodeResource : IResource
{
    public static readonly GraphNodeResource Empty = new() { Name = "Node" };

    public string Name = "";
    public Vector3 Position;
    public Quaternion Rotation;
    public Vector3 Scale;
    public MeshResource[]? Meshes;
    public LightResourceBase[]? Lights;
    public GraphNodeResource[]? Children;
    public Dictionary<string, object>? Metadata;
}