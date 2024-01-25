namespace Nagule.Graphics;

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using CommunityToolkit.HighPerformance.Buffers;
using Sia;

public class Camera3DRenderPipelineManager : ViewBase<TypeUnion<Camera3D>>
{
    [AllowNull] private IEntityQuery _pipelineQuery;
    
    public override void OnInitialize(World world)
    {
        base.OnInitialize(world);

        _pipelineQuery = world.Query<TypeUnion<RenderPipeline<Camera3D>>>();

        Listen((in EntityRef e, in Camera3D.SetDepth cmd) => {
            RegeneratePipelineSchedulerList();
        });
    }

    protected override void OnEntityAdded(in EntityRef inEntity)
    {
        var camera = inEntity;

        World.GetAddon<SimulationFramer>().Start(() => {
            var pipelineChain = SystemChain.Empty;

            foreach (var featureEntity in camera.GetFeatureNode().GetFeatures()) {
                ref var provider = ref featureEntity.GetStateOrNullRef<RenderPipelineProvider>();
                if (Unsafe.IsNullRef(ref provider)) {
                    continue;
                }
                if (provider.Instance != null) {
                    pipelineChain = provider.Instance.TransformPipeline(featureEntity, pipelineChain);
                }
            }

            var pipelineEntity = RenderPipeline<Camera3D>.CreateEntity(World, camera, pipelineChain);
            camera.ReferAsset(pipelineEntity);
            RegeneratePipelineSchedulerList();
        });
    }

    protected override void OnEntityRemoved(in EntityRef entity)
    {
        var pipeline = entity.Get<AssetMetadata>()
            .FindReferred<RenderPipeline<Camera3D>>()!.Value;
        pipeline.Get<RenderPipeline<Camera3D>>().World.Dispose();
        RegeneratePipelineSchedulerList();
    }

    private void RegeneratePipelineSchedulerList()
    {
        var count = _pipelineQuery.Count;
        if (count == 0) { return; }

        var mem = MemoryOwner<(int Depth, Scheduler Scheduler)>.Allocate(count);

        _pipelineQuery.Record(
            mem, static (in EntityRef entity, ref (int, Scheduler) result) => {
                ref var pipeline = ref entity.Get<RenderPipeline<Camera3D>>();
                result = (pipeline.CameraEntity.Get<Camera3D>().Depth, pipeline.Scheduler);
            });

        mem.Span.Sort((e1, e2) => e1.Depth.CompareTo(e2.Depth));

        var schedulers = World.GetAddon<Camera3DRenderer>().Schedulers;

        World.GetAddon<RenderFramer>().Start(mem, (framer, mem) => {
            schedulers.Clear();
            schedulers.EnsureCapacity(mem.Length);

            foreach (ref var entry in mem.Span) {
                schedulers.Add(entry.Scheduler);
            }

            mem.Dispose();
            return true;
        });
    }
}