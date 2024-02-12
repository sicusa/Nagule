namespace Nagule.Graphics.ShadowMapping;

using Sia;

public class ShadowMappingModule : AddonSystemBase
{
    public override void Initialize(World world, Scheduler scheduler)
    {
        base.Initialize(world, scheduler);
        AddAddon<ShadowMapLibrary>(world);
        AddAddon<Light3DShadowMapManager>(world);
    }
}