namespace Nagule.Graphics;

using System.Numerics;

using Sia;

[SiaTemplate(nameof(Light3D))]
[NaAsset]
public record RLight3D : RFeatureBase
{
    public LightType Type { get; init; } = LightType.Point;
    public Vector4 Color { get; init; } = Vector4.One;
    public float Range { get; init; } = 1f;

    public float InnerConeAngle { get; init; }
    public float OuterConeAngle { get; init; }

    public bool IsShadowEnabled { get; init; }
    public float ShadowStrength { get; init; }
    public float ShadowNearPlane { get; init; }
}