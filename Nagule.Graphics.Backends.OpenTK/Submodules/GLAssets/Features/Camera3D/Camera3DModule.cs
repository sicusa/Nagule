namespace Nagule.Graphics.Backends.OpenTK;

using System.Diagnostics.CodeAnalysis;
using Sia;

public class Camera3DWindowAspectRatioUpdateSystem()
    : SystemBase(
        matcher: Matchers.Of<Window>(),
        trigger: EventUnion.Of<WorldEvents.Add, Window.OnSizeChanged>())
{
    [AllowNull] private World.EntityQuery _cameraQuery;

    public override void Initialize(World world, Scheduler scheduler)
    {
        base.Initialize(world, scheduler);
        _cameraQuery = world.Query<TypeUnion<Camera3D>>();
    }

    public override void Execute(World world, Scheduler scheduler, IEntityQuery query)
    {
        var data = (
            manager: world.GetAddon<Camera3DManager>(),
            primaryWindow: world.GetAddon<PrimaryWindow>(),
            _cameraQuery
        );

        query.ForEach(data, static (d, windowEntity) => {
            if (windowEntity != d.primaryWindow.Entity) {
                return;
            }

            ref var window = ref windowEntity.Get<Window>();
            var (width, height) = window.Size;

            d.manager.WindowAspectRatio = width / (float)height;
            d._cameraQuery.ForEach(d.manager, static (manager, cameraEntity) => {
                var camera = cameraEntity.Get<Camera3D>();
                if (camera.AspectRatio != null) {
                    return;
                }
                manager.UpdateCameraParameters(cameraEntity);
            });
        });
    }
}

public class Camera3DTransformUpdateSystem()
    : SystemBase(
        matcher: Matchers.Of<Camera3D>(),
        trigger: EventUnion.Of<Feature.OnNodeTransformChanged>())
{
    public override void Execute(World world, Scheduler scheduler, IEntityQuery query)
        => world.GetAddon<Camera3DUpdator>().Record(query);
}

public class Camera3DParametersUpdateSystem()
    : SystemBase(
        matcher: Matchers.Of<Camera3D>(),
        trigger: EventUnion.Of<
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
    typeof(GraphicsAssetManager<,,>))]
internal partial class Camera3DModule()
    : AssetModuleBase(
        children: SystemChain.Empty
            .Add<Camera3DWindowAspectRatioUpdateSystem>()
            .Add<Camera3DTransformUpdateSystem>()
            .Add<Camera3DParametersUpdateSystem>())
{
    public override void Initialize(World world, Scheduler scheduler)
    {
        base.Initialize(world, scheduler);
        AddAddon<Camera3DUpdator>(world);
    }
}