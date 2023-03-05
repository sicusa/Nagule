namespace Nagule.Graphics;

using System.Numerics;
using System.Collections.Immutable;

public struct GraphNodeProps : IHashComponent
{
    public ReactiveObject<Vector3> Position { get; } = new();
    public ReactiveObject<Quaternion> Rotation { get; } = new();
    public ReactiveObject<Vector3> Scale { get; } = new();

    public ReactiveObject<MeshRenderable?> MeshRenderable { get; } = new();
    public ReactiveHashSet<Light> Lights { get; } = new();
    public ReactiveHashSet<GraphNode> Children { get; } = new();
    public ReactiveDictionary<string, Dyn> Metadata { get; } = new();

    public GraphNodeProps() {}

    public void Set(GraphNode resource)
    {
        Position.Value = resource.Position;
        Rotation.Value = resource.Rotation;
        Scale.Value = resource.Scale;

        MeshRenderable.Value = resource.MeshRenderable;

        Lights.Clear();
        Lights.UnionWith(resource.Lights);

        Children.Clear();
        Children.UnionWith(resource.Children);

        Metadata.Clear();
        foreach (var (k, v) in resource.Metadata) {
            Metadata[k] = v;
        }
    }
}

public record GraphNode : ResourceBase<GraphNodeProps>
{
    public static GraphNode Empty { get; } = new() { Name = "GraphNode" };

    public Vector3 Position { get; init; } = Vector3.Zero;
    public Quaternion Rotation { get; init; } = Quaternion.Identity;
    public Vector3 Scale { get; init; } = Vector3.One;

    public MeshRenderable? MeshRenderable { get; init; } = null;
    public ImmutableList<Light> Lights { get; init; } = ImmutableList<Light>.Empty;
    public ImmutableList<GraphNode> Children { get; init; } = ImmutableList<GraphNode>.Empty;
    public ImmutableDictionary<string, Dyn> Metadata { get; init; } = ImmutableDictionary<string, Dyn>.Empty;

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

    public GraphNode MakeOccluder()
        => Recurse((rec, node) =>
            node with {
                MeshRenderable = node.MeshRenderable?
                    .ConvertMeshes(m => m with { IsOccluder = true }),
                Children = node.Children.ConvertAll(rec)
            });

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

    public GraphNode WithMetadataEntry(string key, Dyn value)
        => this with { Metadata = Metadata.SetItem(key, value) };
    public GraphNode WithMetadataEntries(params KeyValuePair<string, Dyn>[] entries)
        => this with { Metadata = Metadata.SetItems(entries) };
    public GraphNode WithMetadataEntries(IEnumerable<KeyValuePair<string, Dyn>> entries)
        => this with { Metadata = Metadata.SetItems(entries) };
}