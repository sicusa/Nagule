namespace Nagule.Graphics.PostProcessing;

using Sia;

[SiaTemplate(nameof(CubemapSkybox))]
[NaAsset]
public record RCubemapSkybox : REffectBase
{
    public RCubemap Cubemap { get; init; } = RCubemap.Empty;
    public float Exposure { get; init; } = 1f;
}