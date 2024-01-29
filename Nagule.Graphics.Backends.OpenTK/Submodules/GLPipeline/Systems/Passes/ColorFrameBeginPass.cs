namespace Nagule.Graphics.Backends.OpenTK;

using Sia;

public class ColorFrameBeginPass : RenderPassBase
{
    public override void Execute(World world, Scheduler scheduler, IEntityQuery query)
    {
        var framebuffer = world.AcquireAddon<ColorFramebuffer>();
        var clearFlags = ClearFlags.Color | ClearFlags.Depth;

        ref var cameraState = ref CameraState.Get<Camera3DState>();
        if (cameraState.Loaded) {
            clearFlags = cameraState.ClearFlags;

            ref var renderSettingsState = ref cameraState.RenderSettingsState
                .Get<RenderSettingsState>();

            if (renderSettingsState.Loaded) {
                int width = renderSettingsState.Width;
                int height = renderSettingsState.Height;
                if (framebuffer!.Width != width || framebuffer.Height != height) {
                    framebuffer.Resize(width, height);
                }
            }
        }

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, framebuffer!.Handle.Handle);
        GL.Viewport(0, 0, framebuffer.Width, framebuffer.Height);
        GLUtils.Clear(clearFlags);

        GL.BindBufferBase(BufferTargetARB.UniformBuffer, (int)UniformBlockBinding.Pipeline, framebuffer.UniformBufferHandle.Handle);
        GL.BindBufferBase(BufferTargetARB.UniformBuffer, (int)UniformBlockBinding.Camera, cameraState.Handle.Handle);

        framebuffer.Update(RenderFramer.Time);
    }
}