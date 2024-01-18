namespace Nagule.Graphics.Backends.OpenTK;

using Sia;

public class FrameFinishPass : RenderPassSystemBase
{
    public unsafe override void Initialize(World world, Scheduler scheduler)
    {
        base.Initialize(world, scheduler);

        RenderFramer.Start(() => {
            GL.Finish();
            return NextFrame;
        });
    }
}