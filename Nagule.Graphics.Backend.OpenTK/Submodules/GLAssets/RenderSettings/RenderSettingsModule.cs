namespace Nagule.Graphics.Backend.OpenTK;

using System.Diagnostics.CodeAnalysis;
using Sia;

public class RenderSettingsAutoResizeSystem : SystemBase
{
    public RenderSettingsAutoResizeSystem()
    {
        Matcher = Matchers.Of<Window>();
        Trigger = EventUnion.Of<WorldEvents.Add, Window.OnSizeChanged>();
    }

    public override void Execute(World world, Scheduler scheduler, IEntityQuery query)
    {
        var data = (
            primaryWindow: world.GetAddon<PrimaryWindow>(),
            renderSettings: world.Query<TypeUnion<RenderSettings>>()
        );

        query.ForEach(data, static (d, windowEntity) => {
            if (windowEntity != d.primaryWindow.Entity) {
                return;
            }
            var size = windowEntity.Get<Window>().Size;
            d.renderSettings.ForEach(entity => {
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
