namespace Nagule.Prelude;

using Sia;

[SiaTemplate(nameof(Updator))]
[NaAsset]
public record RUpdator(Action<World, EntityRef, SimulationFramer> Action) : RFeatureBase
{
    public RUpdator(Action<EntityRef, SimulationFramer> action)
        : this((world, entity, framer) => action(entity, framer)) {}
}

public class UpdatorExecuteSystem()
    : SystemBase(
        matcher: Matchers.Of<Updator>())
{
    public override void Execute(World world, Scheduler scheduler, IEntityQuery query)
    {
        var data = (
            world, scheduler,
            framer: world.GetAddon<SimulationFramer>()
        );
        query.ForEach(data, static (d, entity) => {
            var node = entity.GetFeatureNode();
            entity.Get<Updator>().Action(d.world, node, d.framer);
        });
    }
}

[NaAssetModule<RUpdator>]
public partial class UpdatorModule()
    : AssetModuleBase(
        children: SystemChain.Empty
            .Add<UpdatorExecuteSystem>());