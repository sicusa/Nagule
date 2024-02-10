namespace Nagule.Graphics;

using Sia;

public class RenderPassBase(
    SystemChain? children = null, IEntityMatcher? matcher = null,
    IEventUnion? trigger = null, IEventUnion? filter = null)
    : AddonSystemBase(children, matcher, trigger, filter)
{
    protected World World { get; private set; } = null!;
    protected RenderFramer RenderFramer { get; private set; } = null!;

    protected EntityRef CameraState => _pipelineInfo.CameraState;
    protected World MainWorld => _pipelineInfo.MainWorld;

    public RenderPassBase() : this(matcher: Matchers.Any) {}

    private RenderPipelineInfo _pipelineInfo = null!;

    public override void Initialize(World world, Scheduler scheduler)
    {
        base.Initialize(world, scheduler);

        _pipelineInfo = world.GetAddon<RenderPipelineInfo>();

        World = world;
        RenderFramer = MainWorld.GetAddon<RenderFramer>();
    }
}