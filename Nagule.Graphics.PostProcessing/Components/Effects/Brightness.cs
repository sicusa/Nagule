namespace Nagule.Graphics.PostProcessing;

using Sia;

[SiaTemplate(nameof(Brightness))]
[NaAsset<Brightness>]
public record RBrightness : REffectBase
{
    public float Value { get; }
}