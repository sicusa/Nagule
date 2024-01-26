namespace Nagule.Graphics.Backends.OpenTK;

using Sia;

public unsafe class SwapBuffersPass : RenderPassBase
{
    private TKWindow* context;

    public override void Execute(World world, Scheduler scheduler, IEntityQuery query)
    {
        if (context == null) {
            context = GLFW.GetCurrentContext();
        }
        GLFW.SwapBuffers(context);
    }
}