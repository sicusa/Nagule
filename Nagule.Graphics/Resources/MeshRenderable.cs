namespace Nagule.Graphics;

using System.Collections.Immutable;

public struct MeshRenderableProps : IHashComponent
{
    public ReactiveHashSet<Mesh> Meshes { get; } = new();

    public MeshRenderableProps() {}

    public void Set(MeshRenderable value)
    {
        Meshes.Clear();
        Meshes.UnionWith(value.Meshes);
    }
}

public record MeshRenderable : ResourceBase<MeshRenderableProps>
{
    public static MeshRenderable Empty { get; } = new();

    public ImmutableHashSet<Mesh> Meshes { get; init; } = ImmutableHashSet<Mesh>.Empty;

    public MeshRenderable WithMesh(Mesh mesh)
        => this with { Meshes = Meshes.Add(mesh) };
    public MeshRenderable WithMeshes(params Mesh[] meshes)
        => this with { Meshes = Meshes.Union(meshes) };
    public MeshRenderable WithMeshes(IEnumerable<Mesh> meshes)
        => this with { Meshes = Meshes.Union(meshes) };
    
    public MeshRenderable ConvertMeshes(Func<Mesh, Mesh> convert)
        => this with {
            Meshes = Meshes
                .Select(convert)
                .ToImmutableHashSet()
        };
}