namespace Nagule.Graphics;

using System.Diagnostics.CodeAnalysis;
using Sia;

public abstract class RenderSystemBase : AddonSystemBase
{
    [AllowNull] protected RenderFramer RenderFramer { get; private set; }

    public RenderSystemBase() {}
    public RenderSystemBase(
        SystemChain? children = null, IEntityMatcher? matcher = null,
        IEventUnion? trigger = null, IEventUnion? filter = null)
        : base(children, matcher, trigger, filter) {}

    public override void Initialize(World world, Scheduler scheduler)
    {
        RenderFramer = world.GetAddon<RenderFramer>();
    }
}