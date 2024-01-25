namespace Nagule.Graphics;

using System.Diagnostics.CodeAnalysis;
using Sia;

public abstract class RenderSystemBase : AddonSystemBase
{
    [AllowNull] internal World World { get; private set; }

    protected RenderFramer RenderFramer => World.GetAddon<RenderFramer>();

    public RenderSystemBase() {}
    public RenderSystemBase(
        SystemChain? children = null, IEntityMatcher? matcher = null,
        IEventUnion? trigger = null, IEventUnion? filter = null)
        : base(children, matcher, trigger, filter) {}

    public override void Initialize(World world, Scheduler scheduler)
    {
        World = world;
    }
}