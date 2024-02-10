namespace Nagule.Graphics.Backends.OpenTK;

using Sia;

public class Camera3DWindowAspectRatioUpdateSystem()
    : SystemBase(
        matcher: Matchers.Of<Window>(),
        trigger: EventUnion.Of<WorldEvents.Add, Window.OnSizeChanged>())
{
    private World.EntityQuery _cameraQuery = null!;

    public override void Initialize(World world, Scheduler scheduler)
    {
        base.Initialize(world, scheduler);
        _cameraQuery = world.Query<TypeUnion<Camera3D>>();
    }

    public override void Execute(World world, Scheduler scheduler, IEntityQuery query)
    {
        var data = (
            manager: world.GetAddon<Camera3DManager>(),
            _cameraQuery
        );

        query.ForEach(data, static (d, windowEntity) => {
            d._cameraQuery.ForEach(d.manager, static (manager, cameraEntity) => {
                var camera = cameraEntity.Get<Camera3D>();
                if (camera.AspectRatio == null && camera.Target is RenderTarget.Window) {
                    manager.UpdateCameraParameters(cameraEntity);
                }
            });
        });
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
    public override void Execute(World world, Scheduler scheduler, IEntityQuery query)
    {
        var manager = world.GetAddon<Camera3DManager>();
        query.ForEach(manager, static (manager, entity) => {
            manager.UpdateCameraParameters(entity);
        });
    }
}

[NaAssetModule<RCamera3D, Bundle<Camera3DState, RenderPipelineProvider>>(
    typeof(GraphicsAssetManagerBase<,>))]
internal partial class Camera3DModule()
    : AssetModuleBase(
        children: SystemChain.Empty
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