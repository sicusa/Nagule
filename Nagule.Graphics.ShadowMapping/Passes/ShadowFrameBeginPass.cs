namespace Nagule.Graphics.Backends.OpenTK;

using Sia;

public class ShadowFrameBeginPass : RenderPassBase
{
    private ShadowPipelineFramebuffer? _framebuffer;

    public override void Execute(World world, Scheduler scheduler, IEntityQuery query)
    {
        _framebuffer ??= AddAddon<ShadowPipelineFramebuffer>(world);

        PrepareFramebuffer(out var clearFlags);

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, _framebuffer!.Handle.Handle);
        GL.Viewport(0, 0, _framebuffer.Width, _framebuffer.Height);
        GLUtils.Clear(clearFlags);
    }

    private void PrepareFramebuffer(out ClearFlags clearFlags)
    {
        ref var cameraState = ref CameraState.Get<Camera3DState>();
        if (!cameraState.Loaded) {
            clearFlags = ClearFlags.Depth;
            return;
        }

        clearFlags = cameraState.ClearFlags;

        ref var renderSettingsState = ref cameraState.SettingsState
            .Get<RenderSettingsState>();
        if (!renderSettingsState.Loaded) { return; }

        int width = _framebuffer!.Width;
        int height = _framebuffer.Height;

        var resolution = renderSettingsState.Resolution;
        if (resolution != null) {
            (width, height) = resolution.Value;
        }
        else if (cameraState.RenderTarget != null) {
            (width, height) = cameraState.RenderTarget.ViewportSize;
        }

        if (_framebuffer.Width != width || _framebuffer.Height != height) {
            _framebuffer.Resize(width, height);
        }

        GL.BindBufferBase(BufferTargetARB.UniformBuffer, (int)UniformBlockBinding.Pipeline, _framebuffer.UniformBufferHandle.Handle);
        GL.BindBufferBase(BufferTargetARB.UniformBuffer, (int)UniformBlockBinding.Camera, cameraState.Handle.Handle);

        _framebuffer.Update(RenderFramer.Time);
    }
}