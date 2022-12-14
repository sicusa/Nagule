namespace Nagule.Graphics;

using System.Numerics;

public enum LightType
{
    Directional,
    Ambient,
    Point,
    Spot,
    Area
}

public record Light : ResourceBase
{
    public LightType Type { get; init; }

    public Vector4 Color { get; init; }
    public bool IsShadowEnabled { get; init; }

    public float AttenuationConstant { get; init; } = 1f;
    public float AttenuationLinear { get; init; } = 0f;
    public float AttenuationQuadratic { get; init; } = 1f;

    public float InnerConeAngle { get; init; }
    public float OuterConeAngle { get; init; }

    public Vector2 AreaSize { get; init; }
}