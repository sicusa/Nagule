namespace Nagule.Graphics;

using System.Numerics;

public record GraphNodeResource : ResourceBase
{
    public static readonly GraphNodeResource Empty = new() { Name = "Node" };

    public string Name = "";
    public Vector3 Position = Vector3.Zero;
    public Quaternion Rotation = Quaternion.Identity;
    public Vector3 Scale = Vector3.One;
    public MeshResource[]? Meshes;
    public LightResourceBase[]? Lights;
    public GraphNodeResource[]? Children;
    public Dictionary<string, object>? Metadata;

    public void Recurse(Action<GraphNodeResource> action)
    {
        action(this);

        if (Children != null) {
            foreach (var child in Children) {
                child.Recurse(action);
            }
        }
    }
}