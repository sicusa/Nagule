namespace Nagule.Prelude;

using Sia;

public class UpdatorManager : AssetManagerBase<Updator, RUpdator>;

public class UpdatorExecuteSystem()
    : SystemBase(
        matcher: Matchers.Of<Updator>())
{
    public override void Execute(World world, Scheduler scheduler, IEntityQuery query)
    {
        var data = (
            world, scheduler,
            frame: world.GetAddon<SimulationFrame>()
        );

        query.ForEach(data, static (d, entity) => {
            var node = entity.GetFeatureNode();
            if (node.Valid) {
                entity.Get<Updator>().Action(d.world, node, d.frame);
            }
        });
    }
}

public class UpdatorModule()
    : AddonSystemBase(
        children: SystemChain.Empty
            .Add<UpdatorExecuteSystem>())
{
    public override void Initialize(World world, Scheduler scheduler)
    {
        base.Initialize(world, scheduler);
        AddAddon<UpdatorManager>(world);
    }
}