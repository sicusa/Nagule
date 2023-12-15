namespace Nagule.Graphics;

using Sia;

[SiaTemplate(nameof(LightingEnvironment))]
[NaguleAsset<LightingEnvironment>]
public record LightingEnvironmentAsset : AssetBase
{
    public static LightingEnvironmentAsset Default { get; } = new();

    public int ShadowMapWidth { get; init; } = 1024;
    public int ShadowMapHeight { get; init; } = 1024;
}