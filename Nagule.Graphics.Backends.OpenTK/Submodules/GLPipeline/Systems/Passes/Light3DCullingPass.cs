namespace Nagule.Graphics.Backends.OpenTK;

using Sia;

public class Light3DCullingPass : RenderPassBase
{
    public override void Execute(World world, Scheduler scheduler, IEntityQuery query)
    {
        var buffer = world.AcquireAddon<Light3DClustersBuffer>();

        ref var cameraState = ref CameraState.Get<Camera3DState>();
        if (!cameraState.Loaded) { return; }

        if (cameraState.ParametersVersion != buffer.CameraParametersVersion) {
            buffer.Update(cameraState);
        }
        buffer.CullLights(cameraState);
    }
}