namespace Nagule.Graphics;

using Sia;

[SiaTemplate(nameof(LightingEnvironment))]
[NaAsset]
public record RLightingEnvironment : AssetBase
{
    public static RLightingEnvironment Default { get; } = new();

    public int ShadowMapWidth { get; init; } = 1024;
    public int ShadowMapHeight { get; init; } = 1024;
}