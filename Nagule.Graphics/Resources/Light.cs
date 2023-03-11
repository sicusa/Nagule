namespace Nagule.Graphics;

using System.Numerics;

public struct LightProps : IHashComponent
{
    public ReactiveObject<LightType> Type { get; } = new();

    public ReactiveObject<Vector4> Color { get; } = new();
    public ReactiveObject<bool> IsShadowEnabled { get; } = new();
    
    public ReactiveObject<float> Range { get; } = new();

    public ReactiveObject<float> InnerConeAngle { get; } = new();
    public ReactiveObject<float> OuterConeAngle { get; } = new();

    public ReactiveObject<Vector2> AreaSize { get; } = new();

    public LightProps() {}

    public void Set(Light resource)
    {
        Type.Value = resource.Type;

        Color.Value = resource.Color;
        IsShadowEnabled.Value = resource.IsShadowEnabled;

        Range.Value = resource.Range;

        InnerConeAngle.Value = resource.InnerConeAngle;
        OuterConeAngle.Value = resource.OuterConeAngle;

        AreaSize.Value = resource.AreaSize;
    }
}

public record Light : ResourceBase<LightProps>
{
    public LightingEnvironment Environment { get; init; } = LightingEnvironment.Default;

    public LightType Type { get; init; } = LightType.Point;
    public Vector4 Color { get; init; }
    public bool IsShadowEnabled { get; init; }

    public float Range { get; init; } = 1f;

    public float InnerConeAngle { get; init; }
    public float OuterConeAngle { get; init; }

    public Vector2 AreaSize { get; init; }
}