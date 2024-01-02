namespace Nagule.Graphics.PostProcessing;

using Sia;

public class GammaCorrectionManager : EffectManagerBase<GammaCorrection, RGammaCorrection>
{
    public override string Source { get; }
        = EmbeddedAssets.LoadInternalText("Shaders.nagule.effects.gamma_correction.comp.glsl");
    public override string EntryPoint { get; } = "GammaCorrection";

    public override void OnInitialize(World world)
    {
        base.OnInitialize(world);
        RegisterProperty<GammaCorrection.SetGamma, float>((in GammaCorrection e) => Dyn.From(e.Gamma));
    }
}

internal class GammaCorrectionModule : AddonSystemBase
{
    public override void Initialize(World world, Scheduler scheduler)
    {
        base.Initialize(world, scheduler);
        AddAddon<GammaCorrectionManager>(world);
    }
}
