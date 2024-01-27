namespace Nagule.Graphics.PostProcessing;

using Sia;

public partial class DepthOfFieldManager
{
    public override string Source { get; }
        = EmbeddedAssets.LoadInternal<RText>("shaders.effects.dof.glsl");
    public override string EntryPoint { get; } = "DepthOfField";

    public override void OnInitialize(World world)
    {
        base.OnInitialize(world);
        RegisterProperty<DepthOfField.SetBlurSize, float>((in DepthOfField e) => Dyn.From(e.BlurSize));
        RegisterProperty<DepthOfField.SetRadiusScale, float>((in DepthOfField e) => Dyn.From(e.RadiusScale));
        RegisterProperty<DepthOfField.SetFocus, float>((in DepthOfField e) => Dyn.From(e.Focus));
        RegisterProperty<DepthOfField.SetFocusScale, float>((in DepthOfField e) => Dyn.From(e.FocusScale));
    }
}

[NaAssetModule<RDepthOfField>(typeof(EffectManagerBase<,>))]
internal partial class DepthOfFieldModule;