namespace Nagule.Graphics.Backends.OpenTK;

using Sia;

public class Light3DTransformUpdateSystem()
    : RenderSystemBase(
        matcher: Matchers.Of<Light3D>(),
        trigger: EventUnion.Of<WorldEvents.Add, Feature.OnTransformChanged>())
{
    public override void Execute(World world, Scheduler scheduler, IEntityQuery query)
        => world.GetAddon<Light3DUpdator>().Record(query);
}

[NaAssetModule<RLight3D, Light3DState>(typeof(GraphicsAssetManager<,,>))]
internal partial class Light3DModule()
    : AssetModuleBase(
        children: SystemChain.Empty
            .Add<Light3DTransformUpdateSystem>())
{
    public override void Initialize(World world, Scheduler scheduler)
    {
        AddAddon<Light3DLibrary>(world);
        AddAddon<Light3DUpdator>(world);
        base.Initialize(world, scheduler);
    }
}