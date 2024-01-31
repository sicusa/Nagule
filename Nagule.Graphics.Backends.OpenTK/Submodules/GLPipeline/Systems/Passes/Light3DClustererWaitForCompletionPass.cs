namespace Nagule.Graphics.Backends.OpenTK;

using Sia;

public class Light3DClustererWaitForCompletionPass : RenderPassBase
{
    public override void Execute(World world, Scheduler scheduler, IEntityQuery query)
    {
        ref var cameraStateEntity = ref CameraState.Get<Camera3DState>();
        if (!cameraStateEntity.Loaded) { return; }

        var clusterer = world.GetAddon<Light3DClusterer>();
        clusterer.WaitForTasksCompleted();
    }
}