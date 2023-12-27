namespace Nagule.Graphics;

using Sia;

[SiaTemplate(nameof(Brightness))]
public record BrightnessAsset : PostProcessingEffectAssetBase
{
    public float Value { get; }
}