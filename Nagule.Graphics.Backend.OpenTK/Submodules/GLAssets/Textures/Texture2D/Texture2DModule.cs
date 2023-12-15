namespace Nagule.Graphics.Backend.OpenTK;

using System.Diagnostics.CodeAnalysis;
using Sia;

public class Texture2DRegenerateSystem : SystemBase
{
    [AllowNull] private Texture2DManager _manager;

    public Texture2DRegenerateSystem()
    {
        Matcher = Matchers.Of<Texture2D>();
        Trigger = EventUnion.Of<
            Texture2D.SetType,
            Texture2D.SetImage
        >();
    }

    public override void Initialize(World world, Scheduler scheduler)
    {
        base.Initialize(world, scheduler);
        _manager = world.GetAddon<Texture2DManager>();
    }

    public override void Execute(World world, Scheduler scheduler, IEntityQuery query)
    {
        query.ForEach(this, static (sys, entity) => {
            var manager = sys._manager;

            ref var tex = ref entity.Get<Texture2D>();
            var type = tex.Type;
            var image = tex.Image ?? ImageAsset.Hint;

            manager.RegenerateTexture(entity, () => {
                GLUtils.TexImage2D(type, image);
            });
        });
    }
}

public class Texture2DModule : AddonSystemBase
{
    public Texture2DModule()
    {
        Children = SystemChain.Empty
            .Add<Texture2DRegenerateSystem>();
    }

    public override void Initialize(World world, Scheduler scheduler)
    {
        base.Initialize(world, scheduler);
        AddAddon<Texture2DManager>(world);
    }
}