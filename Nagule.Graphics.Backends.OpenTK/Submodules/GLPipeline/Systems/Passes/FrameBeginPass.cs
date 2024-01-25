namespace Nagule.Graphics.Backends.OpenTK;

using Sia;

public class FrameBeginPass : RenderPassSystemBase
{
    private SimulationFramer? _framer;
    private EntityRef _cameraState;

    public override void Initialize(World world, Scheduler scheduler)
    {
        base.Initialize(world, scheduler);

        _framer = MainWorld.GetAddon<SimulationFramer>();

        ref var camera = ref Camera.Get<Camera3D>();
        _cameraState = Camera.GetStateEntity();
    }

    public override void Execute(World world, Scheduler scheduler, IEntityQuery query)
    {
        var framebuffer = world.AcquireAddon<Framebuffer>();
        var clearFlags = ClearFlags.Color | ClearFlags.Depth;

        ref var cameraState = ref _cameraState.Get<Camera3DState>();
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

        framebuffer.Update(_framer!.Time);
    }
}