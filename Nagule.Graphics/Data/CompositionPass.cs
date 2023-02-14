namespace Nagule.Graphics;

public abstract record CompositionPass
{
    public record BlitColor : CompositionPass;
    public record BlitDepth : CompositionPass;

    public record ACESToneMapping : CompositionPass;
    public record GammaCorrection(float Gamma = 2.2f) : CompositionPass;

    public record Bloom(
        float Threshold = 1f,
        float Intensity = 1f,
        float Radius = 0.05f,
        Texture? DirtTexture = null,
        float DirtIntensity = 1f) : CompositionPass;
}