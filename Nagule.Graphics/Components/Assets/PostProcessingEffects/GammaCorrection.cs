namespace Nagule.Graphics;

using Sia;

[SiaTemplate(nameof(GammaCorrection))]
public record GammaCorrectionAsset : PostProcessingEffectAssetBase
{
    public float Gamma { get; init; } = 2.2f;
}