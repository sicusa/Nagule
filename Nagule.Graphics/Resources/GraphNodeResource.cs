namespace Nagule.Graphics;

using System.Numerics;
using System.Collections.Immutable;

public record GraphNodeResource : ResourceBase
{
    public static readonly GraphNodeResource Empty = new() { Name = "Node" };

    public string Name { get; init; } = "";
    public Vector3 Position { get; init; } = Vector3.Zero;
    public Quaternion Rotation { get; init; } = Quaternion.Identity;
    public Vector3 Scale { get; init; } = Vector3.One;

    public ImmutableList<MeshResource> Meshes { get; init; } = ImmutableList<MeshResource>.Empty;
    public ImmutableList<LightResourceBase> Lights { get; init; } = ImmutableList<LightResourceBase>.Empty;
    public ImmutableList<GraphNodeResource> Children { get; init; } = ImmutableList<GraphNodeResource>.Empty;
    public ImmutableDictionary<string, object> Metadata { get; init; } = ImmutableDictionary<string, object>.Empty;

    public GraphNodeResource Recurse(
        Func<Func<GraphNodeResource, GraphNodeResource>, GraphNodeResource, GraphNodeResource> mapper)
    {
        GraphNodeResource DoRecurse(GraphNodeResource node) => mapper(DoRecurse, node);
        return mapper(DoRecurse, this);
    }

    public GraphNodeResource Recurse<TArg>(
        Func<Func<GraphNodeResource, TArg, GraphNodeResource>, GraphNodeResource, TArg, GraphNodeResource> mapper, TArg initial)
    {
        GraphNodeResource DoRecurse(GraphNodeResource node, TArg arg) => mapper(DoRecurse, node, arg);
        return mapper(DoRecurse, this, initial);
    }

    public GraphNodeResource WithMesh(MeshResource mesh)
        => this with { Meshes = Meshes.Add(mesh) };
    public GraphNodeResource WithMeshes(params MeshResource[] meshes)
        => this with { Meshes = Meshes.AddRange(meshes) };
    public GraphNodeResource WithMeshes(IEnumerable<MeshResource> meshes)
        => this with { Meshes = Meshes.AddRange(meshes) };

    public GraphNodeResource WithLight(LightResourceBase light)
        => this with { Lights = Lights.Add(light) };
    public GraphNodeResource WithLights(params LightResourceBase[] lights)
        => this with { Lights = Lights.AddRange(lights) };
    public GraphNodeResource WithLights(IEnumerable<LightResourceBase> lights)
        => this with { Lights = Lights.AddRange(lights) };
        
    public GraphNodeResource WithChild(GraphNodeResource child)
        => this with { Children = Children.Add(child) };
    public GraphNodeResource WithChildren(params GraphNodeResource[] children)
        => this with { Children = Children.AddRange(children) };
    public GraphNodeResource WithChildren(IEnumerable<GraphNodeResource> children)
        => this with { Children = Children.AddRange(children) };

    public GraphNodeResource WithMetadataEntry(string key, object value)
        => this with { Metadata = Metadata.SetItem(key, value) };
    public GraphNodeResource WithMetadataEntries(params KeyValuePair<string, object>[] entries)
        => this with { Metadata = Metadata.SetItems(entries) };
    public GraphNodeResource WithMetadataEntries(IEnumerable<KeyValuePair<string, object>> entries)
        => this with { Metadata = Metadata.SetItems(entries) };
}