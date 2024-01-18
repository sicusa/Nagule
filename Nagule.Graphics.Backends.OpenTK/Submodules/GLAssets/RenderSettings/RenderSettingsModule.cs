namespace Nagule.Graphics.Backends.OpenTK;

using Sia;

public class RenderSettingsAutoResizeSystem()
    : SystemBase(
        matcher: Matchers.Of<Window>(),
        trigger: EventUnion.Of<WorldEvents.Add, Window.OnSizeChanged>())
{
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
                    entity.RenderSettings_SetSize(size);
                }
            });
        });
    }
}

[NaAssetModule<RRenderSettings, RenderSettingsState>(typeof(GraphicsAssetManager<,,>))]
internal partial class RenderSettingsModule()
    : AssetModuleBase(
        children: SystemChain.Empty
            .Add<RenderSettingsAutoResizeSystem>());