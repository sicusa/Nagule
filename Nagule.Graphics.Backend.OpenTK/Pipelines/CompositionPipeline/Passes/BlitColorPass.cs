namespace Nagule.Graphics.Backend.OpenTK;

public class BlitColorPass : CompositionPassBase
{
    public override string? EntryPoint { get; } = "BlitColor";
    public override string? Source { get; }
        = GraphicsHelper.LoadEmbededShader("nagule.pipeline.blit_color.comp.glsl");
}