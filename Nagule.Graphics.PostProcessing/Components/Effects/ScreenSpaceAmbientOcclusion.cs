namespace Nagule.Graphics.PostProcessing;

using Sia;

[SiaTemplate(nameof(ScreenSpaceAmbientOcclusion))]
[NaAsset]
public record RScreenSpaceAmbientOcclusion : REffectBase
{
    public int Samples { get; init; } = 8;
    public float Radius { get; init; } = 0.5f;
}