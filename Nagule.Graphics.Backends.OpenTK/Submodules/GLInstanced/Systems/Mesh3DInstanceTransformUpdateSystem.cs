namespace Nagule.Graphics.Backends.OpenTK;

using CommunityToolkit.HighPerformance.Buffers;
using Sia;

public class Mesh3DInstanceTransformUpdateSystem()
    : RenderSystemBase(
        matcher: Matchers.Of<Mesh3D>(),
        trigger: EventUnion.Of<Feature.OnTransformChanged>())
{
    public override void Execute(World world, Scheduler scheduler, IEntityQuery query)
    {
        int queryCount = query.Count;
        if (queryCount == 0) { return; }

        var mem = MemoryOwner<GLMesh3DInstanceUpdator.Entry>.Allocate(queryCount);
        query.Record(mem, static (in EntityRef entity, ref GLMesh3DInstanceUpdator.Entry value) => {
            var node = entity.GetFeatureNode();
            value = new(entity, node.Get<Transform3D>().World);
        });

        var updator = world.GetAddon<GLMesh3DInstanceUpdator>();
        RenderFramer.Start(() => {
            int i = 0;
            foreach (ref var tuple in mem.Span) {
                updator.PendingDict[tuple.Entity] = (mem, i);
                i++;
            }
            return true;
        });
    }
}