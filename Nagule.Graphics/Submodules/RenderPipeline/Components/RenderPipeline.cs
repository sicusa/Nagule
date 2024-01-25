namespace Nagule.Graphics;

using Sia;

public record struct RenderPipeline<TTargetCamera>(
    EntityRef CameraEntity, World World, Scheduler Scheduler)
{
    public static EntityRef CreateEntity(
        World world, EntityRef cameraEntity, SystemChain pipelineChain)
    {
        var pipelineWorld = new World();
        var pipelineScheduler = new Scheduler();

        var info = pipelineWorld.AcquireAddon<PipelineInfo>();
        info.MainWorld = world;
        info.CameraState = cameraEntity.GetStateEntity();

        pipelineChain.RegisterTo(pipelineWorld, pipelineScheduler);
        return world.CreateInSparseHost(
            AssetBundle.Create<RenderPipeline<TTargetCamera>>(
                new(cameraEntity, pipelineWorld, pipelineScheduler)));
    }
}
