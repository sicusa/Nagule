namespace Nagule.Graphics.Backend.OpenTK;

using System.Diagnostics.CodeAnalysis;
using Sia;

public class RenderSettingsAutoResizeSystem : SystemBase
{
    [AllowNull] private PrimaryWindow _primaryWindow;
    [AllowNull] private World.EntityQuery _renderSettings;

    public RenderSettingsAutoResizeSystem()
    {
        Matcher = Matchers.Of<Window>();
        Trigger = EventUnion.Of<WorldEvents.Add, Window.OnSizeChanged>();
    }

    public override void Initialize(World world, Scheduler scheduler)
    {
        base.Initialize(world, scheduler);
        _primaryWindow = world.GetAddon<PrimaryWindow>();
        _renderSettings = world.Query<TypeUnion<RenderSettings>>();
    }

    public override void Execute(World world, Scheduler scheduler, IEntityQuery query)
    {
        query.ForEach(this, static (sys, windowEntity) => {
            if (windowEntity != sys._primaryWindow.Entity) {
                return;
            }
            var size = windowEntity.Get<Window>().Size;
            sys._renderSettings.ForEach(entity => {
                ref var renderSettings = ref entity.Get<RenderSettings>();
                if (renderSettings.AutoResizeByWindow) {
                    entity.Modify(new RenderSettings.SetSize(size));
                }
            });
        });
    }
}

public class RenderSettingsModule : AddonSystemBase
{
    public RenderSettingsModule()
    {
        Children = SystemChain.Empty
            .Add<RenderSettingsAutoResizeSystem>();
    }

    public override void Initialize(World world, Scheduler scheduler)
    {
        base.Initialize(world, scheduler);
        AddAddon<RenderSettingsManager>(world);
    }
}
