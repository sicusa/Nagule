namespace Nagule.Graphics;

using System.Numerics;
using System.Collections.Immutable;

using Sia;

public record Mesh3DData
{
    public static readonly Mesh3DData Empty = new();

    public PrimitiveType PrimitiveType { get; init; } = PrimitiveType.Triangle;
    public ImmutableArray<Vector3> Vertices { get; init; } = ImmutableArray<Vector3>.Empty;
    public ImmutableArray<Vector3> Normals { get; init; } = ImmutableArray<Vector3>.Empty;
    public ImmutableArray<Vector3> TexCoords { get; init; } = ImmutableArray<Vector3>.Empty;
    public ImmutableArray<Vector3> Tangents { get; init; } = ImmutableArray<Vector3>.Empty;
    public ImmutableArray<uint> Indices { get; init; } = ImmutableArray<uint>.Empty;

    public Rectangle? BoundingBox { get; init; }
}

[SiaTemplate(nameof(Mesh3D))]
[NaguleAsset<Mesh3D>]
public record Mesh3DAsset : FeatureAssetBase
{
    public static Mesh3DAsset Empty { get; } = new();

    public Mesh3DData Data { get; init; } = Mesh3DData.Empty;
    public MaterialAsset Material { get; init; } = MaterialAsset.Default;
}
