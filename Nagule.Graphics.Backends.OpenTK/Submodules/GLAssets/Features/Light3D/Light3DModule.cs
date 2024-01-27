namespace Nagule.Graphics.Backends.OpenTK;

using System.Numerics;
using CommunityToolkit.HighPerformance.Buffers;
using Sia;

public class Light3DTransformUpdateSystem()
    : RenderSystemBase(
        matcher: Matchers.Of<Light3D>(),
        trigger: EventUnion.Of<WorldEvents.Add, Feature.OnTransformChanged>())
{

    public override void Execute(World world, Scheduler scheduler, IEntityQuery query)
    {
        int count = query.Count;
        if (count == 0) { return; }

        var mem = MemoryOwner<Light3DUpdator.Entry>.Allocate(count);
        query.Record(mem, static (in EntityRef entity, ref Light3DUpdator.Entry value) => {
            var nodeTrans = entity.GetFeatureNode().Get<Transform3D>();
            value = new(entity.GetStateEntity(), nodeTrans.WorldPosition, -nodeTrans.WorldForward);
        });

        var updator = world.GetAddon<Light3DUpdator>();

        RenderFramer.Start(() => {
            int i = 0;
            foreach (ref var tuple in mem.Span) {
                updator.PendingDict[tuple.StateEntity] = (mem, i);
                i++;
            }
            return true;
        });
    }
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