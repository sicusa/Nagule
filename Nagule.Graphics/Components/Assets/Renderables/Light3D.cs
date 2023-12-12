namespace Nagule.Graphics;

using System.Numerics;

using Sia;

[SiaTemplate(nameof(Light3D))]
[NaguleAsset<Light3D>]
public record Light3DAsset : FeatureAssetBase
{
    public LightingEnvironmentAsset Environment { get; init; } = LightingEnvironmentAsset.Default;

    public LightType Type { get; init; } = LightType.Point;
    public Vector4 Color { get; init; } = Vector4.One;
    public bool IsShadowEnabled { get; init; }

    public float Range { get; init; } = 1f;

    public float InnerConeAngle { get; init; }
    public float OuterConeAngle { get; init; }
}