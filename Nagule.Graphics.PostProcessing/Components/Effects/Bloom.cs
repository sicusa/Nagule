namespace Nagule.Graphics.PostProcessing;

using Sia;

[SiaTemplate(nameof(Bloom))]
[NaAsset<Bloom>]
public record RBloom : REffectBase
{
    public float Threshold { get; init; } = 1f;
    public float Intensity { get; init; } = 2f;
    public float Radius { get; init; } = 0.05f;

    public RTexture2D? DirtTexture { get; init; } = null;
    public float DirtIntensity { get; init; } = 1f;
}