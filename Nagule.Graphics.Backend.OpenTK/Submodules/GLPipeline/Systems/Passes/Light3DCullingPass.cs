namespace Nagule.Graphics.Backend.OpenTK;

using System.Runtime.CompilerServices;
using Sia;

public class Light3DCullingPass : RenderPassSystemBase
{
    public override void Initialize(World world, Scheduler scheduler)
    {
        base.Initialize(world, scheduler);

        var cameraManager = world.GetAddon<Camera3DManager>();
        var buffer = Pipeline.AcquireAddon<Light3DClustersBuffer>();
        
        RenderFrame.Start(() => {
            ref var cameraState = ref cameraManager.RenderStates.GetOrNullRef(Camera);
            if (Unsafe.IsNullRef(ref cameraState)) {
                return ShouldStop;
            }
            buffer.Load(world, cameraState);
            return true;
        });

        RenderFrame.Start(() => {
            ref var cameraState = ref cameraManager.RenderStates.GetOrNullRef(Camera);
            if (Unsafe.IsNullRef(ref cameraState)) {
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