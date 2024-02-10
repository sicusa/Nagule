namespace Nagule.Graphics.Backends.OpenTK;

using Sia;

public class Light3DUpdateSystem()
    : RenderSystemBase(
        matcher: Matchers.Of<Light3D>(),
        trigger: EventUnion.Of<
            WorldEvents.Add,
            Feature.OnIsEnabledChanged,
            Feature.OnNodeTransformChanged>())
{
    public override void Execute(World world, Scheduler scheduler, IEntityQuery query)
        => world.GetAddon<Light3DUpdator>().Record(query);
}

[NaAssetModule<RLight3D, Light3DState>(typeof(GraphicsAssetManagerBase<,>))]
internal partial class Light3DModule()
    : AssetModuleBase(
        children: SystemChain.Empty
            .Add<Light3DUpdateSystem>())
{
    public override void Initialize(World world, Scheduler scheduler)
    {
        AddAddon<Light3DLibrary>(world);
        AddAddon<Light3DUpdator>(world);
        base.Initialize(world, scheduler);
    }
}