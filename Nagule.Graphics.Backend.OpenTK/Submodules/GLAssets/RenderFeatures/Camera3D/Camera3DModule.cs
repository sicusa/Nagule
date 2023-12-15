namespace Nagule.Graphics.Backend.OpenTK;

using System.Diagnostics.CodeAnalysis;
using Sia;

public class Camera3DAspectRatioUpdateSystem : SystemBase
{
    [AllowNull] private Camera3DManager _manager;
    [AllowNull] private PrimaryWindow _primaryWindow;
    [AllowNull] private World.EntityQuery _cameraQuery;

    public Camera3DAspectRatioUpdateSystem()
    {
        Matcher = Matchers.Of<Window>();
        Trigger = EventUnion.Of<WorldEvents.Add, Window.OnSizeChanged>();
    }

    public override void Initialize(World world, Scheduler scheduler)
    {
        base.Initialize(world, scheduler);
        _cameraQuery = world.Query<TypeUnion<Camera3D>>();
        _manager = world.GetAddon<Camera3DManager>();
        _primaryWindow = world.GetAddon<PrimaryWindow>();
    }

    public override void Execute(World world, Scheduler scheduler, IEntityQuery query)
    {
        query.ForEach(this, static (sys, windowEntity) => {
            if (windowEntity != sys._primaryWindow.Entity) {
                return;
            }
            ref var window = ref windowEntity.Get<Window>();
            var (width, height) = window.Size;
            sys._manager.WindowAspectRatio = width / (float)height;
            sys._cameraQuery.ForEach(sys._manager, static (manager, cameraEntity) => {
                var camera = cameraEntity.Get<Camera3D>();
                if (camera.AspectRatio != null) {
                    return;
                }
                manager.UpdateCameraParameters(cameraEntity);
            });
        });
    }
}

public class Camera3DTransformUpdateSystem : SystemBase
{
    [AllowNull] private Camera3DManager _manager;

    public Camera3DTransformUpdateSystem()
    {
        Matcher = Matchers.Of<Camera3D, Feature>();
        Trigger = EventUnion.Of<Feature.OnTransformChanged>();
    }

    public override void Initialize(World world, Scheduler scheduler)
    {
        base.Initialize(world, scheduler);
        _manager = world.GetAddon<Camera3DManager>();
    }

    public override void Execute(World world, Scheduler scheduler, IEntityQuery query)
    {
        query.ForEach(this, static (sys, entity) => {
            sys._manager.UpdateCameraTransform(entity);
        });
    }
}

public class Camera3DParametersUpdateSystem : SystemBase
{
    [AllowNull] private Camera3DManager _manager;

    public Camera3DParametersUpdateSystem()
    {
        Matcher = Matchers.Of<Camera3D>();
        Trigger = EventUnion.Of<
            Camera3D.SetAspectRatio,
            Camera3D.SetFieldOfView,
            Camera3D.SetOrthographicWidth,
            Camera3D.SetNearPlaneDistance,
            Camera3D.SetFarPlaneDistance
        >();
    }

    public override void Initialize(World world, Scheduler scheduler)
    {
        base.Initialize(world, scheduler);
        _manager = world.GetAddon<Camera3DManager>();
    }

    public override void Execute(World world, Scheduler scheduler, IEntityQuery query)
    {
        query.ForEach(this, static (sys, entity) => {
            sys._manager.UpdateCameraParameters(entity);
        });
    }
}

public class Camera3DModule : AddonSystemBase
{
    public Camera3DModule()
    {
        Children = SystemChain.Empty
            .Add<Camera3DAspectRatioUpdateSystem>()
            .Add<Camera3DTransformUpdateSystem>()
            .Add<Camera3DParametersUpdateSystem>();
    }

    public override void Initialize(World world, Scheduler scheduler)
    {
        base.Initialize(world, scheduler);
        AddAddon<Camera3DManager>(world);
    }
}