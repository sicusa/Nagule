namespace Nagule.Graphics;

using System.Collections.Immutable;

public record MeshRenderable : ResourceBase
{
    public static MeshRenderable Empty { get; } = new();

    public ImmutableDictionary<Mesh, MeshBufferMode> Meshes { get; init; }
        = ImmutableDictionary<Mesh, MeshBufferMode>.Empty;

    public MeshRenderable WithMesh(Mesh mesh, MeshBufferMode bufferMode = MeshBufferMode.Instance)
        => this with { Meshes = Meshes.SetItem(mesh, bufferMode) };
    public MeshRenderable WithMeshes(params KeyValuePair<Mesh, MeshBufferMode>[] meshes)
        => this with { Meshes = Meshes.SetItems(meshes) };
    public MeshRenderable WithMeshes(IEnumerable<KeyValuePair<Mesh, MeshBufferMode>> meshes)
        => this with { Meshes = Meshes.AddRange(meshes) };
    
    public MeshRenderable ConvertMeshes(Func<Mesh, Mesh> convert)
        => this with {
            Meshes = Meshes
                .Select(p => KeyValuePair.Create(convert(p.Key), p.Value))
                .ToImmutableDictionary()
        };
}