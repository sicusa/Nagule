namespace Nagule.Graphics;

using System.Numerics;

public abstract record LightResourceBase : ResourceBase
{
    public Vector4 Color { get; init; }
    public bool IsShadowEnabled { get; init; }
}

public record DirectionalLightResource : LightResourceBase
{
}

public record AmbientLightResource : LightResourceBase
{
}

public abstract record AttenuateLightResourceBase : LightResourceBase
{
    public float AttenuationConstant { get; init; } = 1f;
    public float AttenuationLinear { get; init; } = 0f;
    public float AttenuationQuadratic { get; init; } = 1f;
}

public record PointLightResource : AttenuateLightResourceBase
{
}

public record SpotLightResource : AttenuateLightResourceBase
{
    public float InnerConeAngle { get; init; }
    public float OuterConeAngle { get; init; }
}

public record AreaLightResource : AttenuateLightResourceBase
{
    public Vector2 AreaSize { get; init; }
}