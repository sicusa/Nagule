using Sia;

namespace Nagule.Graphics.Backends.OpenTK;

public unsafe class WindowRenderTarget(int index) : ColorRenderTargetBase
{
    public int Index { get; } = index;

    public override (int, int) ViewportSize
        => _primaryWindow.Entity.Get<Window>().Size;

    private PrimaryWindow _primaryWindow = null!;
    private TKWindow* _context;

    public override void OnInitialize(World world, EntityRef cameraEntity)
    {
        base.OnInitialize(world, cameraEntity);
        _primaryWindow = world.GetAddon<PrimaryWindow>();
    }

    protected override bool PrepareBlit()
    {
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

        var window = _primaryWindow!.Entity.Get<Window>();
        var (width, height) = window.IsFullscreen ? window.Size : window.PhysicalSize;

        GL.Viewport(0, 0, width, height);

        return true;
    }

    protected override void FinishBlit()
    {
        if (_context == null) {
            _context = GLFW.GetCurrentContext();
        }

        GL.Finish();
        GLFW.SwapBuffers(_context);
    }
}