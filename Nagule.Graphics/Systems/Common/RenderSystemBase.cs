namespace Nagule.Graphics;

using System.Diagnostics.CodeAnalysis;
using Sia;

public abstract class RenderSystemBase : AddonSystemBase
{
    public bool ShouldStop { get; private set; }

    [AllowNull] protected RenderFrame RenderFrame { get; private set; }

    public override void Initialize(World world, Scheduler scheduler)
    {
        RenderFrame = world.GetAddon<RenderFrame>();
    }

    public override void Uninitialize(World world, Scheduler scheduler)
    {
        ShouldStop = true;
    }
}