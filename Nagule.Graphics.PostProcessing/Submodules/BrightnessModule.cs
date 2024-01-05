namespace Nagule.Graphics.PostProcessing;

using Sia;

public class BrightnessManager : EffectManagerBase<Brightness, RBrightness>
{
    public override string Source { get; }
        = EmbeddedAssets.LoadInternal<RText>("Shaders.nagule.effects.brightness.comp.glsl");
    public override string EntryPoint { get; } = "Brightness";

    public override void OnInitialize(World world)
    {
        base.OnInitialize(world);
        RegisterProperty<Brightness.SetValue, float>((in Brightness e) => Dyn.From(e.Value));
    }
}

internal class BrightnessModule : AddonSystemBase
{
    public override void Initialize(World world, Scheduler scheduler)
    {
        base.Initialize(world, scheduler);
        AddAddon<BrightnessManager>(world);
    }
}
