namespace Nagule.Graphics.Backends.OpenTK;

using Sia;

public class RenderTexture2DAutoResizeByWindowSystem()
    : SystemBase(
        matcher: Matchers.Of<Window>(),
        trigger: EventUnion.Of<WorldEvents.Add, Window.OnSizeChanged>())
{
    private IEntityQuery _textureQuery = null!;

    public override void Initialize(World world, Scheduler scheduler)
    {
        base.Initialize(world, scheduler);
        _textureQuery = world.Query<TypeUnion<RenderTexture2D>>();
    }

    public override void Execute(World world, Scheduler scheduler, IEntityQuery query)
    {
        var data = (
            textureQuery: _textureQuery,
            manager: world.GetAddon<RenderTexture2DManager>(),
            primaryWindow: world.GetAddon<PrimaryWindow>()
        );

        query.ForEach(data, static (d, windowEntity) => {
            if (windowEntity != d.primaryWindow.Entity) {
                return;
            }

            ref var window = ref windowEntity.Get<Window>();
            var (width, height) = window.ScaledSize;

            d.manager.WindowSize = window.ScreenSize;
            d.textureQuery.ForEach((d.manager, width, height), static (d, texEntity) => {
                ref var tex = ref texEntity.Get<RenderTexture2D>();
                if (tex.AutoResizeByWindow) {
                    d.manager.RegenerateRenderTexture(texEntity);
                }
            });
        });
    }
}

[NaAssetModule<RRenderTexture2D, RenderTexture2DState>(typeof(TextureManagerBase<,>))]
internal partial class RenderTexture2DModule()
    : AssetModuleBase(
        children: SystemChain.Empty
            .Add<RenderTexture2DAutoResizeByWindowSystem>());