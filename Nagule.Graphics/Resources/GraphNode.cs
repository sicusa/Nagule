namespace Nagule.Graphics;

using System.Numerics;
using System.Collections.Immutable;

public record GraphNode : ResourceBase
{
    public static readonly GraphNode Empty = new() { Name = "Node" };

    public string Name { get; init; } = "";
    public Vector3 Position { get; init; } = Vector3.Zero;
    public Quaternion Rotation { get; init; } = Quaternion.Identity;
    public Vector3 Scale { get; init; } = Vector3.One;

    public ImmutableList<Mesh> Meshes { get; init; } = ImmutableList<Mesh>.Empty;
    public ImmutableList<Light> Lights { get; init; } = ImmutableList<Light>.Empty;
    public ImmutableList<GraphNode> Children { get; init; } = ImmutableList<GraphNode>.Empty;
    public ImmutableDictionary<string, object> Metadata { get; init; } = ImmutableDictionary<string, object>.Empty;

    public GraphNode Recurse(
        Func<Func<GraphNode, GraphNode>, GraphNode, GraphNode> mapper)
    {
        GraphNode DoRecurse(GraphNode node) => mapper(DoRecurse, node);
        return mapper(DoRecurse, this);
    }

    public GraphNode Recurse<TArg>(
        Func<Func<GraphNode, TArg, GraphNode>, GraphNode, TArg, GraphNode> mapper, TArg initial)
    {
        GraphNode DoRecurse(GraphNode node, TArg arg) => mapper(DoRecurse, node, arg);
        return mapper(DoRecurse, this, initial);
    }

    public GraphNode WithMesh(Mesh mesh)
        => this with { Meshes = Meshes.Add(mesh) };
    public GraphNode WithMeshes(params Mesh[] meshes)
        => this with { Meshes = Meshes.AddRange(meshes) };
    public GraphNode WithMeshes(IEnumerable<Mesh> meshes)
        => this with { Meshes = Meshes.AddRange(meshes) };

    public GraphNode WithLight(Light light)
        => this with { Lights = Lights.Add(light) };
    public GraphNode WithLights(params Light[] lights)
        => this with { Lights = Lights.AddRange(lights) };
    public GraphNode WithLights(IEnumerable<Light> lights)
        => this with { Lights = Lights.AddRange(lights) };
        
    public GraphNode WithChild(GraphNode child)
        => this with { Children = Children.Add(child) };
    public GraphNode WithChildren(params GraphNode[] children)
        => this with { Children = Children.AddRange(children) };
    public GraphNode WithChildren(IEnumerable<GraphNode> children)
        => this with { Children = Children.AddRange(children) };

    public GraphNode WithMetadataEntry(string key, object value)
        => this with { Metadata = Metadata.SetItem(key, value) };
    public GraphNode WithMetadataEntries(params KeyValuePair<string, object>[] entries)
        => this with { Metadata = Metadata.SetItems(entries) };
    public GraphNode WithMetadataEntries(IEnumerable<KeyValuePair<string, object>> entries)
        => this with { Metadata = Metadata.SetItems(entries) };
}