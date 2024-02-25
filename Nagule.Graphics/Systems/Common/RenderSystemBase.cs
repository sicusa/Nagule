namespace Nagule.Graphics;

using Sia;

public abstract class RenderSystemBase : AddonSystemBase
{
    protected World World { get; private set; } = null!;

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