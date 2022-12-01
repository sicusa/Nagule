namespace Nagule.Graphics;

using System.Numerics;

public abstract record LightResourceBase : IResource
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
    public float AttenuationConstant = 1f;
    public float AttenuationLinear = 0f;
    public float AttenuationQuadratic = 1f;
}

public record PointLightResource : AttenuateLightResourceBase
{
}

public record SpotLightResource : AttenuateLightResourceBase
{
    public float InnerConeAngle;
    public float OuterConeAngle;
}

public record AreaLightResource : AttenuateLightResourceBase
{
    public Vector2 AreaSize;
}