namespace Nagule.Graphics;

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Sia;

public class Camera3DPipelineInitializeSystem()
    : SystemBase(
        matcher: Matchers.Of<Camera3D>(),
        trigger: EventUnion.Of<WorldEvents.Add>())
{
    public override void Execute(World world, Scheduler scheduler, IEntityQuery query)
    {
        var data = (
            world,
            lib: world.GetAddon<RenderPipelineLibrary>()
        );

        query.ForEach(data, static (d, entity) => {
            ref var entry = ref CollectionsMarshal.GetValueRefOrAddDefault(
                d.lib.EntriesRaw, entity, out bool exists);
            if (exists) {
                return;
            }

            var pipelineScheduler = new RenderPipelineScheduler(entity);
            var pipelineChain = SystemChain.Empty;

            foreach (var featureEntity in entity.GetFeatureNode().GetFeatures()) {
                ref var provider = ref featureEntity.GetStateOrNullRef<RenderPipelineProvider>();
                if (Unsafe.IsNullRef(ref provider)) {
                    continue;
                }
                pipelineChain = provider.Instance?.TransformPipeline(featureEntity, pipelineChain) ?? pipelineChain;
            }

            entry = new(pipelineScheduler, pipelineChain.RegisterTo(d.world, pipelineScheduler));
        });
    }
}