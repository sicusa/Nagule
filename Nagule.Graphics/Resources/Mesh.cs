namespace Nagule.Graphics;

using System.Numerics;
using System.Collections.Immutable;

public record Mesh : ResourceBase
{
    public static readonly Mesh Empty = new();

    public PrimitiveType PrimitiveType { get; init; } = PrimitiveType.Triangle;
    public ImmutableArray<Vector3> Vertices { get; init; } = ImmutableArray<Vector3>.Empty;
    public ImmutableArray<Vector3> Normals { get; init; } = ImmutableArray<Vector3>.Empty;
    public ImmutableArray<Vector3> TexCoords { get; init; } = ImmutableArray<Vector3>.Empty;
    public ImmutableArray<Vector3> Tangents { get; init; } = ImmutableArray<Vector3>.Empty;
    public ImmutableArray<Vector3> Bitangents { get; init; } = ImmutableArray<Vector3>.Empty;
    public ImmutableArray<int> Indices { get; init; } = ImmutableArray<int>.Empty;
    public Rectangle BoundingBox { get; init; }
    public Material Material { get; init; } = Material.Default;

    public bool IsOccluder { get; init; }
}