namespace Nagule.Graphics.PostProcessing;

using Sia;

internal class EffectPipelineModule : AddonSystemBase
{
    public override void Initialize(World world, Scheduler scheduler)
    {
        base.Initialize(world, scheduler);
        AddAddon<EffectPipelineManager>(world);
    }
}