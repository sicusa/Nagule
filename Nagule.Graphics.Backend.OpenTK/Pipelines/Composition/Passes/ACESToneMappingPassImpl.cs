namespace Nagule.Graphics.Backend.OpenTK;

public class ACESToneMappingPassImpl : CompositionPassImplBase
{
    public override string? EntryPoint { get; } = "ACESToneMapping";
    public override string? Source { get; }
        = GraphicsHelper.LoadEmbededShader("nagule.pipeline.aces_tone_mapping.comp.glsl");
}