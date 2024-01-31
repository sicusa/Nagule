namespace Nagule.Graphics.Backends.OpenTK;

using Sia;

public class FrameFinishPass : RenderPassBase
{
    public override void Execute(World world, Scheduler scheduler, IEntityQuery query)
    {
        GL.Finish();
    }
}