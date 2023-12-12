namespace Nagule.Graphics.Backend.OpenTK;

using Sia;

public class GLSLProgramModule : AddonSystemBase
{
    public override void Initialize(World world, Scheduler scheduler)
    {
        base.Initialize(world, scheduler);
        AddAddon<GLSLProgramManager>(world);
    }
}