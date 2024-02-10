namespace Nagule.Graphics.Backends.OpenTK;

using Sia;

public class BlitToRenderTargetPass : RenderPassBase
{
    private IPipelineFramebuffer? _framebuffer;

    public override void Execute(World world, Scheduler scheduler, IEntityQuery query)
    {
        _framebuffer ??= world.GetAddon<IPipelineFramebuffer>();

        ref var cameraState = ref CameraState.Get<Camera3DState>();
        cameraState.RenderTarget?.Blit(_framebuffer);
    }
}