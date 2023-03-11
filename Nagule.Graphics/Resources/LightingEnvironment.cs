namespace Nagule.Graphics;

public class LightingEnvironmentProps : IHashComponent
{
    public ReactiveObject<int> ShadowMapWidth { get; } = new();
    public ReactiveObject<int> ShadowMapHeight { get; } = new();

    public void Set(LightingEnvironment resource)
    {
        ShadowMapWidth.Value = resource.ShadowMapWidth;
        ShadowMapHeight.Value = resource.ShadowMapHeight;
    }
}

public record LightingEnvironment : ResourceBase<LightingEnvironmentProps>
{
    public static LightingEnvironment Default { get; } = new();

    public int ShadowMapWidth { get; init; } = 2048;
    public int ShadowMapHeight { get; init; } = 2048;
}