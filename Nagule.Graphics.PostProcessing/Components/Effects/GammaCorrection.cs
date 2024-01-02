namespace Nagule.Graphics.PostProcessing;

using Sia;

[SiaTemplate(nameof(GammaCorrection))]
[NaAsset<GammaCorrection>]
public record RGammaCorrection : REffectBase
{
    public float Gamma { get; init; } = 2.2f;
}