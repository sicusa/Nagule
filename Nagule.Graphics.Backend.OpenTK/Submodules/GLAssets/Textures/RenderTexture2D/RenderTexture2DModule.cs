namespace Nagule.Graphics.Backend.OpenTK;

using System.Diagnostics.CodeAnalysis;
using Sia;

public record struct RenderTexture2DState : ITextureState
{
    public readonly bool Loaded => Handle != TextureHandle.Zero;

    public bool MipmapEnabled { get; set; }
    public TextureHandle Handle { get; set; }

    public int Width;
    public int Height;
    public FramebufferHandle FramebufferHandle;
}

public class RenderTexture2DAutoResizeByWindowSystem()
    : SystemBase(
        matcher: Matchers.Of<Window>(),
        trigger: EventUnion.Of<WorldEvents.Add, Window.OnSizeChanged>())
{
    [AllowNull] private IEntityQuery _textureQuery;

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
            var (width, height) = window.Size;

            d.manager.WindowSize = window.Size;
            d.textureQuery.ForEach((d.manager, width, height), static (d, texEntity) => {
                ref var tex = ref texEntity.Get<RenderTexture2D>();
                if (tex.AutoResizeByWindow) {
                    d.manager.RegenerateRenderTexture(texEntity);
                }
            });
        });
    }
}

[NaAssetModule<RRenderTexture2D, RenderTexture2DState>(typeof(TextureManagerBase<,,>))]
internal partial class RenderTexture2DModule()
    : AssetModuleBase(
        children: SystemChain.Empty
            .Add<RenderTexture2DAutoResizeByWindowSystem>());