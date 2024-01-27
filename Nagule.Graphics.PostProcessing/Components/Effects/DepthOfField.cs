namespace Nagule.Graphics.PostProcessing;

using Sia;

[SiaTemplate(nameof(DepthOfField))]
[NaAsset]
public record RDepthOfField : REffectBase
{
    public float BlurSize { get; init; } = 16f;
    public float RadiusScale { get; init; } = 1.5f;

    public float Focus { get; init; } = 1.9f;
    public float FocusScale { get; init; } = 2f;
}