namespace Nagule.Graphics.Backend.OpenTK;

using Sia;

public class SwapBuffersPass : RenderPassSystemBase
{
    public unsafe override void Initialize(World world, Scheduler scheduler)
    {
        base.Initialize(world, scheduler);

        TKWindow* context = null;

        RenderFramer.Start(() => {
            context = GLFW.GetCurrentContext();
            return true;
        });

        RenderFramer.Start(() => {
            GLFW.SwapBuffers(context);
            return false;
        });
    }
}