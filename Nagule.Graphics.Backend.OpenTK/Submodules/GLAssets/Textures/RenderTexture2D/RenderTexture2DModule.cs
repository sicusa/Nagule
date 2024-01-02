namespace Nagule.Graphics.Backend.OpenTK;

using System.Diagnostics.CodeAnalysis;
using Sia;

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
            d.textureQuery.ForEach((width, height), static (d, texEntity) => {
                ref var tex = ref texEntity.Get<RenderTexture2D>();
                if (tex.AutoResizeByWindow) {
                    texEntity.Modify(ref tex, new RenderTexture2D.SetWidth(d.width));
                    texEntity.Modify(ref tex, new RenderTexture2D.SetHeight(d.width));
                }
            });
        });
    }
}

public class RenderTexture2DRegenerateSystem()
    : SystemBase(
        matcher: Matchers.Of<RenderTexture2D>(),
        trigger: EventUnion.Of<
            RenderTexture2D.SetType,
            RenderTexture2D.SetPixelFormat,
            RenderTexture2D.SetWidth,
            RenderTexture2D.SetHeight
        >())
{
    public override void Execute(World world, Scheduler scheduler, IEntityQuery query)
    {
        var manager = world.GetAddon<RenderTexture2DManager>();

        query.ForEach(manager, static (manager, entity) => {
            ref var tex = ref entity.Get<RenderTexture2D>();
            var type = tex.Type;
            var pixelFormat = tex.PixelFormat;
            var width = tex.Width;
            var height = tex.Height;

            manager.RegenerateTexture(entity, (ref RenderTexture2DState state) => {
                GLUtils.TexImage2D(type, pixelFormat, width, height);
                state.Width = width;
                state.Height = height;
            });
        });
    }
}

internal class RenderTexture2DModule()
    : AddonSystemBase(
        children: SystemChain.Empty
            .Add<RenderTexture2DAutoResizeByWindowSystem>()
            .Add<RenderTexture2DRegenerateSystem>())
{
    public override void Initialize(World world, Scheduler scheduler)
    {
        base.Initialize(world, scheduler);
        AddAddon<RenderTexture2DManager>(world);
    }
}