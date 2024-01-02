namespace Nagule.Graphics;

using System.Diagnostics.CodeAnalysis;
using Sia;

public abstract class RenderSystemBase : AddonSystemBase
{
    public bool NextFrame { get; private set; }

    [AllowNull] protected RenderFrame RenderFrame { get; private set; }

    public RenderSystemBase() {}
    public RenderSystemBase(
        SystemChain? children = null, IEntityMatcher? matcher = null,
        IEventUnion? trigger = null, IEventUnion? filter = null)
        : base(children, matcher, trigger, filter) {}

    public override void Initialize(World world, Scheduler scheduler)
    {
        RenderFrame = world.GetAddon<RenderFrame>();
    }

    public override void Uninitialize(World world, Scheduler scheduler)
    {
        NextFrame = true;
    }
}