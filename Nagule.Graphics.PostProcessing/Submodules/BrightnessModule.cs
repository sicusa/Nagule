namespace Nagule.Graphics.PostProcessing;

using Sia;

public partial class BrightnessManager
{
    public override string Source { get; }
        = EmbeddedAssets.LoadInternal<RText>("shaders.nagule.effects.brightness.comp.glsl");
    public override string EntryPoint { get; } = "Brightness";

    public override void OnInitialize(World world)
    {
        base.OnInitialize(world);
        RegisterProperty<Brightness.SetValue, float>((in Brightness e) => Dyn.From(e.Value));
    }
}

[NaAssetModule<RBrightness>(typeof(EffectManagerBase<,>))]
internal partial class BrightnessModule;