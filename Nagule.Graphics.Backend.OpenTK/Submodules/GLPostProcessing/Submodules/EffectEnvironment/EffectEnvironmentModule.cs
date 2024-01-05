namespace Nagule.Graphics.Backend.OpenTK;

using Sia;

internal class EffectEnvironmentModule : AddonSystemBase
{
    public override void Initialize(World world, Scheduler scheduler)
    {
        base.Initialize(world, scheduler);
        AddAddon<EffectEnvironmentManager>(world);
    }
}