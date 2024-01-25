namespace Nagule.Graphics;

using System.Runtime.CompilerServices;
using Sia;

public class Camera3DRenderPipelineCreator : ViewBase<TypeUnion<Camera3D>>
{
    protected override void OnEntityAdded(in EntityRef inEntity)
    {
        var camera = inEntity;

        World.GetAddon<SimulationFramer>().Start(() => {
            var pipelineWorld = new World();
            var pipelineScheduler = new RenderPipelineScheduler(camera);
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

            var info = pipelineWorld.AcquireAddon<PipelineInfo>();
            info.MainWorld = World;
            info.CameraState = camera.GetStateEntity();

            pipelineChain.RegisterTo(pipelineWorld, pipelineScheduler);
            var pipelineEntity = World.CreateInSparseHost(
                AssetBundle.Create<RenderPipeline<Camera3D>>(
                    new(camera, pipelineWorld, pipelineScheduler)));
            camera.ReferAsset(pipelineEntity);
            return true;
        });
    }

    protected override void OnEntityRemoved(in EntityRef entity)
    {
        var pipeline = entity.Get<AssetMetadata>().FindReferred<RenderPipeline<Camera3D>>()!.Value;
        pipeline.Get<RenderPipeline<Camera3D>>().World.Dispose();
    }
}