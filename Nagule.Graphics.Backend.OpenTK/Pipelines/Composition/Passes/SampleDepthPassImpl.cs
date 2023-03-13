namespace Nagule.Graphics.Backend.OpenTK;

public class SampleDepthPassImpl : CompositionPassImplBase
{
    public override string? EntryPoint { get; } = "SampleDepth";
    public override string? Source { get; }
        = GraphicsHelper.LoadEmbededShader("nagule.pipeline.sample_depth.comp.glsl");
}