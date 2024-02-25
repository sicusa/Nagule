namespace Nagule.Graphics.Backends.OpenTK;

using Sia;

public class Camera3DIsEnabledChangedHandleSystem()
    : SystemBase(
        matcher: Matchers.Of<Camera3D>(),
        trigger: EventUnion.Of<Feature.OnIsEnabledChanged>())
{
    private Camera3DRenderer _renderer = null!;
    private RenderFramer _framer = null!;

    public override void Initialize(World world, Scheduler scheduler)
    {
        base.Initialize(world, scheduler);
        _renderer = world.GetAddon<Camera3DRenderer>();
        _framer = world.GetAddon<RenderFramer>();
    }

    public override void Execute(World world, Scheduler scheduler, IEntityQuery query)
    {
        foreach (var entity in query) {
            ref var feature = ref entity.Get<Feature>();
            var stateEntity = entity.GetStateEntity();

            if (feature.IsEnabled) {
                ref var camera = ref entity.Get<Camera3D>();
                var priority = camera.Priority;

                _framer.Enqueue(entity, () => {
                    ref var state = ref stateEntity.Get<Camera3DState>();
                    _renderer.Register(priority, state.PipelineStateEntity);
                });
            }
            else {
                ref var camera = ref entity.Get<Camera3D>();
                _framer.Enqueue(entity, () => {
                    ref var state = ref stateEntity.Get<Camera3DState>();
                    _renderer.Unregister(state.PipelineStateEntity);
                });
            }
        }
    }
}

public class Camera3DWindowAspectRatioUpdateSystem()
    : SystemBase(
        matcher: Matchers.Of<Window>(),
        trigger: EventUnion.Of<WorldEvents.Add, Window.OnSizeChanged>())
{
    private Camera3DManager _manager = null!;
    private IReactiveEntityQuery _cameraQuery = null!;

    public override void Initialize(World world, Scheduler scheduler)
    {
        base.Initialize(world, scheduler);
        _manager = world.GetAddon<Camera3DManager>();
        _cameraQuery = world.Query<TypeUnion<Camera3D>>();
    }

    public override void Execute(World world, Scheduler scheduler, IEntityQuery query)
    {
        foreach (var entity in query) {
            foreach (var cameraEntity in _cameraQuery) {
                var camera = cameraEntity.Get<Camera3D>();
                if (camera.AspectRatio == null && camera.Target is RenderTarget.Window) {
                    _manager.UpdateCameraParameters(cameraEntity);
                }
            }
        }
    }
}

public class Camera3DTransformUpdateSystem()
    : SystemBase(
        matcher: Matchers.Of<Camera3D>(),
        trigger: EventUnion.Of<
            Feature.OnIsEnabledChanged,
            Feature.OnNodeTransformChanged>())
{
    public override void Execute(World world, Scheduler scheduler, IEntityQuery query)
        => world.GetAddon<Camera3DUpdator>().Record(query);
}

public class Camera3DParametersUpdateSystem()
    : SystemBase(
        matcher: Matchers.Of<Camera3D>(),
        trigger: EventUnion.Of<
            Camera3D.SetProjectionMode,
            Camera3D.SetAspectRatio,
            Camera3D.SetFieldOfView,
            Camera3D.SetOrthographicWidth,
            Camera3D.SetNearPlaneDistance,
            Camera3D.SetFarPlaneDistance
        >())
{
    private Camera3DManager _manager = null!;

    public override void Initialize(World world, Scheduler scheduler)
    {
        base.Initialize(world, scheduler);
        _manager = world.GetAddon<Camera3DManager>();
    }

    public override void Execute(World world, Scheduler scheduler, IEntityQuery query)
    {
        foreach (var entity in query) {
            _manager.UpdateCameraParameters(entity);
        }
    }
}

[NaAssetModule<RCamera3D, Bundle<Camera3DState, RenderPipelineProvider>>(
    typeof(GraphicsAssetManagerBase<,>))]
internal partial class Camera3DModule()
    : AssetModuleBase(
        children: SystemChain.Empty
            .Add<Camera3DIsEnabledChangedHandleSystem>()
            .Add<Camera3DWindowAspectRatioUpdateSystem>()
            .Add<Camera3DTransformUpdateSystem>()
            .Add<Camera3DParametersUpdateSystem>())
{
    public override void Initialize(World world, Scheduler scheduler)
    {
        AddAddon<Camera3DRenderer>(world);
        base.Initialize(world, scheduler);
        AddAddon<Camera3DUpdator>(world);
    }
}