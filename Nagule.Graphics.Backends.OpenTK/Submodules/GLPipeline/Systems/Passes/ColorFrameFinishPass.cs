namespace Nagule.Graphics.Backends.OpenTK;

using Sia;

public class ColorFrameFinishPass : RenderPassBase
{
    public override void Execute(World world, Scheduler scheduler, IEntityQuery query)
    {
        GL.Finish();
    }
}