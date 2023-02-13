namespace Nagule.Graphics;

public abstract record CompositionPass
{
    public record BlitColor : CompositionPass;
    public record BlitDepth : CompositionPass;

    public record ACESToneMapping : CompositionPass;
    public record GammaCorrection(float Gamma = 2.2f) : CompositionPass;

    public record Bloom(float Threshold = 0.2f, float Blur = 0.02f);
}