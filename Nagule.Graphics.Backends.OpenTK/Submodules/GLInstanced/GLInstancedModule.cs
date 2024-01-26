namespace Nagule.Graphics.Backends.OpenTK;

using Sia;

public class GLInstancedModule()
    : AddonSystemBase(
        children: SystemChain.Empty
            .Add<Mesh3DInstanceTransformUpdateSystem>()
            .Add<Mesh3DInstanceGroupSystem>())
{
    public override void Initialize(World world, Scheduler scheduler)
    {
        base.Initialize(world, scheduler);
        AddAddon<GLMesh3DInstanceLibrary>(world);
        AddAddon<GLMesh3DInstanceCleaner>(world);
        AddAddon<GLMesh3DInstanceUpdator>(world);
    }
}