namespace Nagule.Graphics.PostProcessing;

using Sia;

public partial class GammaCorrectionManager
{
    public override string Source { get; }
        = EmbeddedAssets.LoadInternal<RText>("shaders.effects.gamma_correction.glsl");
    public override string EntryPoint { get; } = "GammaCorrection";

    public override void OnInitialize(World world)
    {
        base.OnInitialize(world);
        RegisterProperty<GammaCorrection.SetGamma, float>((in GammaCorrection e) => Dyn.From(e.Gamma));
    }
}

[NaAssetModule<RGammaCorrection>(typeof(EffectManagerBase<,>))]
internal partial class GammaCorrectionModule;