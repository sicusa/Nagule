namespace Nagule.Graphics;

public abstract record CompositionPass
{
    public record SampleColor : CompositionPass;
    public record SampleDepth : CompositionPass;

    public record BlitToDisplay : CompositionPass;
    public record BlitToRenderTexture(RenderTexture RenderTexture) : CompositionPass;

    public record ACESToneMapping : CompositionPass;
    public record GammaCorrection(float Gamma = 2.2f) : CompositionPass;

    public record Brightness(float Value = 1f) : CompositionPass;

    public record Bloom(
        float Threshold = 1f,
        float Intensity = 2f,
        float Radius = 0.05f,
        Texture? DirtTexture = null,
        float DirtIntensity = 1f) : CompositionPass;
}