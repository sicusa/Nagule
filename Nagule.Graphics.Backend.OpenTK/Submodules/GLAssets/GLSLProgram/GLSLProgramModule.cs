namespace Nagule.Graphics.Backend.OpenTK;

using Sia;

internal class GLSLProgramModule : AddonSystemBase
{
    public override void Initialize(World world, Scheduler scheduler)
    {
        base.Initialize(world, scheduler);
        AddAddon<GLSLProgramManager>(world);
    }
}