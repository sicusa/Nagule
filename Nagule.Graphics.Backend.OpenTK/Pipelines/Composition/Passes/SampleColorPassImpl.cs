namespace Nagule.Graphics.Backend.OpenTK;

public class SampleColorPassImpl : CompositionPassImplBase
{
    public override string? EntryPoint { get; } = "SampleColor";
    public override string? Source { get; }
        = GraphicsHelper.LoadEmbededShader("nagule.pipeline.sample_color.comp.glsl");
}