namespace Nagule.Graphics.Backend.OpenTK;

public class BlitDepthPass : CompositionPassBase
{
    public override string EntryPoint { get; } = "BlitDepth";
    public override string Source { get; }
        = GraphicsHelper.LoadEmbededShader("nagule.pipeline.blit_depth.comp.glsl");
}