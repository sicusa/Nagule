namespace Nagule.Graphics.Backend.OpenTK;

using Sia;

public class Texture2DRegenerateSystem()
    : SystemBase(
        matcher: Matchers.Of<Texture2D>(),
        trigger: EventUnion.Of<Texture2D.SetType, Texture2D.SetImage>())
{
    public override void Execute(World world, Scheduler scheduler, IEntityQuery query)
    {
        var manager = world.GetAddon<Texture2DManager>();

        query.ForEach(manager, static (manager, entity) => {
            ref var tex = ref entity.Get<Texture2D>();
            var type = tex.Type;
            var image = tex.Image ?? RImage.Hint;

            manager.RegenerateTexture(entity, () => {
                GLUtils.TexImage2D(type, image);
            });
        });
    }
}

internal class Texture2DModule()
    : AddonSystemBase(
        children: SystemChain.Empty
            .Add<Texture2DRegenerateSystem>())
{
    public override void Initialize(World world, Scheduler scheduler)
    {
        base.Initialize(world, scheduler);
        AddAddon<Texture2DManager>(world);
    }
}