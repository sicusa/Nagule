namespace Nagule.Graphics.Backend.OpenTK;

using System.Diagnostics.CodeAnalysis;
using Sia;

public class RenderTexture2DAutoResizeByWindowSystem : SystemBase
{
    [AllowNull] private IEntityQuery _textureQuery;
    [AllowNull] private RenderTexture2DManager _manager;
    [AllowNull] private PrimaryWindow _primaryWindow;

    public RenderTexture2DAutoResizeByWindowSystem()
    {
        Matcher = Matchers.Of<Window>();
        Trigger = EventUnion.Of<WorldEvents.Add, Window.OnSizeChanged>();
    }

    public override void Initialize(World world, Scheduler scheduler)
    {
        base.Initialize(world, scheduler);
        _textureQuery = world.Query<TypeUnion<RenderTexture2D>>();
        _manager = world.GetAddon<RenderTexture2DManager>();
        _primaryWindow = world.GetAddon<PrimaryWindow>();
    }

    public override void Execute(World world, Scheduler scheduler, IEntityQuery query)
    {
        query.ForEach((world, this), static (tuple, windowEntity) => {
            var (world, sys) = tuple;
            if (windowEntity != sys._primaryWindow.Entity) {
                return;
            }

            ref var window = ref windowEntity.Get<Window>();
            var (width, height) = window.Size;

            sys._manager.WindowSize = window.Size;
            sys._textureQuery.ForEach((world, width, height), static (tuple, texEntity) => {
                ref var tex = ref texEntity.Get<RenderTexture2D>();
                if (tex.AutoResizeByWindow) {
                    var (world, width, height) = tuple;
                    texEntity.Modify(ref tex, new RenderTexture2D.SetWidth(width));
                    texEntity.Modify(ref tex, new RenderTexture2D.SetHeight(width));
                }
            });
        });
    }
}

public class RenderTexture2DRegenerateSystem : SystemBase
{
    [AllowNull] private RenderTexture2DManager _manager;

    public RenderTexture2DRegenerateSystem()
    {
        Matcher = Matchers.Of<RenderTexture2D>();
        Trigger = EventUnion.Of<
            RenderTexture2D.SetType,
            RenderTexture2D.SetPixelFormat,
            RenderTexture2D.SetWidth,
            RenderTexture2D.SetHeight
        >();
    }

    public override void Initialize(World world, Scheduler scheduler)
    {
        base.Initialize(world, scheduler);
        _manager = world.GetAddon<RenderTexture2DManager>();
    }

    public override void Execute(World world, Scheduler scheduler, IEntityQuery query)
    {
        query.ForEach(this, static (sys, entity) => {
            var manager = sys._manager;

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

public class RenderTexture2DModule : AddonSystemBase
{
    public RenderTexture2DModule()
    {
        Children = SystemChain.Empty
            .Add<RenderTexture2DAutoResizeByWindowSystem>()
            .Add<RenderTexture2DRegenerateSystem>();
    }

    public override void Initialize(World world, Scheduler scheduler)
    {
        base.Initialize(world, scheduler);
        AddAddon<RenderTexture2DManager>(world);
    }
}