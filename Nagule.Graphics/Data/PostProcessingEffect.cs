namespace Nagule.Graphics;

public abstract record PostProcessingEffect
{
    public record ACESToneMapping : PostProcessingEffect;
    public record GammaCorrection(float Gamma = 2.2f) : PostProcessingEffect;

    public record Brightness(float Value = 1f) : PostProcessingEffect;

    public record Bloom(
        float Threshold = 1f,
        float Intensity = 2f,
        float Radius = 0.05f,
        Texture2DAsset? DirtTexture = null,
        float DirtIntensity = 1f) : PostProcessingEffect;
}