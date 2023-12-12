namespace Nagule.Graphics.Backend.OpenTK;

using Sia;

public class FrameFinishPass : RenderPassSystemBase
{
    public unsafe override void Initialize(World world, Scheduler scheduler)
    {
        base.Initialize(world, scheduler);

        var framebuffer = Pipeline.GetAddon<Framebuffer>();
        RenderFrame.Start(() => {
            framebuffer.FenceSync();
            framebuffer.WaitSync();
            return false;
        });
    }
}