namespace Nagule.Graphics.Backends.OpenTK;

using Sia;

public class GLShadowMapModule : AssetSystemModule
{
    public override void Initialize(World world, Scheduler scheduler)
    {
        base.Initialize(world, scheduler);
        AddAddon<ShadowMapLibrary>(world);
    }
}