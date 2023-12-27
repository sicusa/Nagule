namespace Nagule.Graphics.Backend.OpenTK;

using System.Runtime.CompilerServices;
using Sia;

public class Light3DCullingPass : RenderPassSystemBase
{
    public override void Initialize(World world, Scheduler scheduler)
    {
        base.Initialize(world, scheduler);

        var buffer = Pipeline.AcquireAddon<Light3DClustersBuffer>();
        
        RenderFrame.Start(() => {
            ref var cameraState = ref Camera.GetState<Camera3DState>();
            if (!cameraState.Loaded) {
                return ShouldStop;
            }
            buffer.Load(world, cameraState);
            return true;
        });

        RenderFrame.Start(() => {
            ref var cameraState = ref Camera.GetState<Camera3DState>();
            if (!cameraState.Loaded) {
                return ShouldStop;
            }
            if (cameraState.ParametersVersion != buffer.CameraParametersVersion) {
                buffer.Update(cameraState);
            }
            buffer.CullLights(cameraState);
            return ShouldStop;
        });
    }
}