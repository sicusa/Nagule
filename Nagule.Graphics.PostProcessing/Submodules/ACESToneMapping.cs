namespace Nagule.Graphics.PostProcessing;

using Sia;

public class ACESToneMappingManager : EffectManagerBase<ACESToneMapping, RACESToneMapping>
{
    public override string Source { get; }
        = EmbeddedAssets.LoadInternal<RText>("Shaders.nagule.effects.aces_tone_mapping.comp.glsl");
    public override string EntryPoint { get; } = "ACESToneMapping";
}

internal class ACESToneMappingModule : AddonSystemBase
{
    public override void Initialize(World world, Scheduler scheduler)
    {
        base.Initialize(world, scheduler);
        AddAddon<ACESToneMappingManager>(world);
    }
}
