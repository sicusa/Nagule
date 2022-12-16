namespace Nagule.Graphics;

using System.Numerics;
using System.Collections.Immutable;

public record MeshResource : ResourceBase
{
    public static readonly MeshResource Empty = new();

    public ImmutableArray<Vector3> Vertices { get; init; } = ImmutableArray<Vector3>.Empty;
    public ImmutableArray<Vector3> TexCoords { get; init; } = ImmutableArray<Vector3>.Empty;
    public ImmutableArray<Vector3> Normals { get; init; } = ImmutableArray<Vector3>.Empty;
    public ImmutableArray<Vector3> Tangents { get; init; } = ImmutableArray<Vector3>.Empty;
    public ImmutableArray<int> Indeces { get; init; } = ImmutableArray<int>.Empty;
    public Rectangle BoundingBox { get; init; }
    public MaterialResource Material { get; init; } = MaterialResource.Default;

    public bool IsOccluder { get; init; }
}