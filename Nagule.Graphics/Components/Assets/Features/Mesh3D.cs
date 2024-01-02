namespace Nagule.Graphics;

using System.Numerics;
using System.Collections.Immutable;
using Sia;

public record Mesh3DData
{
    public static readonly Mesh3DData Empty = new();

    public PrimitiveType PrimitiveType { get; init; } = PrimitiveType.Triangle;
    public ImmutableArray<Vector3> Vertices { get; init; } = [];
    public ImmutableArray<Vector3> Normals { get; init; } = [];
    public ImmutableArray<Vector3> TexCoords { get; init; } = [];
    public ImmutableArray<Vector3> Tangents { get; init; } = [];
    public ImmutableArray<uint> Indices { get; init; } = [];

    public Rectangle? BoundingBox { get; init; }
}

[SiaTemplate(nameof(Mesh3D))]
[NaAsset<Mesh3D>]
public record RMesh3D : FeatureAssetBase
{
    public static RMesh3D Empty { get; } = new();

    public Mesh3DData Data { get; init; } = Mesh3DData.Empty;
    public RMaterial Material { get; init; } = RMaterial.Default;
}
