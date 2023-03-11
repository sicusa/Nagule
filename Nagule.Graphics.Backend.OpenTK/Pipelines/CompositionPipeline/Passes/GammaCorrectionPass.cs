namespace Nagule.Graphics.Backend.OpenTK;

public class GammaCorrectionPass : CompositionPassBase
{
    public override IEnumerable<MaterialProperty> Properties { get; } 

    public override string EntryPoint { get; } = "GammaCorrection";
    public override string Source { get; }
        = GraphicsHelper.LoadEmbededShader("nagule.pipeline.gamma_correction.comp.glsl");

    public GammaCorrectionPass(float gamma = 2.2f)
    {
        Properties = new MaterialProperty[] {
            new("GammaCorrection_Gamma", gamma)
        };
    }
}