namespace Nagule.Graphics.Backend.OpenTK;

using System.Diagnostics.CodeAnalysis;
using Sia;

public class CubemapRegenerateSystem()
    : SystemBase(
        matcher: Matchers.Of<Cubemap>(),
        trigger: EventUnion.Of<
            Cubemap.SetType,
            Cubemap.SetImages
        >())
{
    [AllowNull] private CubemapManager _manager;

    public override void Initialize(World world, Scheduler scheduler)
    {
        base.Initialize(world, scheduler);
        _manager = world.GetAddon<CubemapManager>();
    }

    public override void Execute(World world, Scheduler scheduler, IEntityQuery query)
    {
        query.ForEach(this, static (sys, entity) => {
            var manager = sys._manager;
            ref var tex = ref entity.Get<Cubemap>();

            var type = tex.Type;
            var images = tex.Images;

            manager.RegenerateTexture(entity, () => {
                foreach (var (target, image) in images) {
                    var textureTarget = TextureUtils.Cast(target);
                    GLUtils.TexImage2D(textureTarget, type, image);
                }
            });
        });
    }
}

internal class CubemapModule()
    : AddonSystemBase(
        children: SystemChain.Empty
            .Add<CubemapRegenerateSystem>())
{
    public override void Initialize(World world, Scheduler scheduler)
    {
        base.Initialize(world, scheduler);
        AddAddon<CubemapManager>(world);
    }
}