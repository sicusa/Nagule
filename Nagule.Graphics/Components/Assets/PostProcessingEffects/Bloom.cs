namespace Nagule.Graphics;

using Sia;

[SiaTemplate(nameof(Bloom))]
public record BloomAsset : PostProcessingEffectAssetBase
{
    public float Threashold { get; init; } = 1f;
    public float Intensity { get; init; } = 2f;
    public float Radius { get; init; } = 0.05f;

    public Texture2DAsset? DirtTexture { get; init; } = null;
    public float DirtIntensity { get; init; } = 1f;
}