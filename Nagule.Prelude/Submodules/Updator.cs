namespace Nagule.Prelude;

using Sia;

[SiaTemplate(nameof(Updator))]
[NaAsset]
public record RUpdator(Action<World, EntityRef, SimulationFrame> Action) : RFeatureBase
{
    public RUpdator(Action<EntityRef, SimulationFrame> action)
        : this((world, entity, frame) => action(entity, frame)) {}
}

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
            entity.Get<Updator>().Action(d.world, node, d.frame);
        });
    }
}

[NaAssetModule<RUpdator>]
public partial class UpdatorModule()
    : AssetModuleBase(
        children: SystemChain.Empty
            .Add<UpdatorExecuteSystem>());