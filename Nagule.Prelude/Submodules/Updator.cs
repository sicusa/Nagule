namespace Nagule.Prelude;

using Sia;

using UpdatorAction = Action<Sia.World, Sia.EntityRef, SimulationFramer>;

[SiaTemplate(nameof(Updator))]
[NaAsset]
public record RUpdator(UpdatorAction Action) : RFeatureBase
{
    public RUpdator(Action<EntityRef, SimulationFramer> action)
        : this((world, entity, framer) => action(entity, framer)) {}
}

public struct UpdatorState
{
    public Scheduler.TaskGraphNode? TaskGraphNode;
}

public partial class UpdatorManager
{
    public override void OnInitialize(World world)
    {
        base.OnInitialize(world);

        Listen((in EntityRef e, in Updator.SetAction cmd) => {
            ref var state = ref e.GetState<UpdatorState>();

            if (state.TaskGraphNode != null) {
                state.TaskGraphNode.Dispose();
                state.TaskGraphNode = ActivateUpdator(
                    SimulationFramer, e.GetFeatureNode(), cmd.Value);
            }
        });
    }

    public override void LoadAsset(in EntityRef entity, ref Updator asset, EntityRef stateEntity)
    {
        ref var feature = ref entity.Get<Feature>();
        if (feature.IsEnabled) {
            stateEntity.Get<UpdatorState>().TaskGraphNode =
                ActivateUpdator(SimulationFramer, feature.Node, asset.Action);
        }
    }

    public override void UnloadAsset(in EntityRef entity, in Updator asset, EntityRef stateEntity)
    {
        ref var state = ref stateEntity.Get<UpdatorState>();
        state.TaskGraphNode?.Dispose();
    }

    internal static Scheduler.TaskGraphNode ActivateUpdator(
        SimulationFramer framer, EntityRef nodeEntity, UpdatorAction action)
    {
        var world = Context<World>.Current!;
        return framer.Scheduler.CreateTask(() => {
            action(world, nodeEntity, framer);
            return false;
        });
    }
}

public class UpdatorActivationStateChangeSystem()
    : SystemBase(
        matcher: Matchers.Of<Updator>(),
        trigger: EventUnion.Of<Feature.OnIsEnabledChanged>())
{
    public override void Execute(World world, Scheduler scheduler, IEntityQuery query)
    {
        foreach (var entity in query) {
            ref var state = ref entity.GetState<UpdatorState>();
            ref var feature = ref entity.Get<Feature>();

            if (feature.IsEnabled) {
                state.TaskGraphNode =
                    UpdatorManager.ActivateUpdator(
                        world.GetAddon<SimulationFramer>(), feature.Node, entity.Get<Updator>().Action);
            }
            else {
                state.TaskGraphNode!.Dispose();
                state.TaskGraphNode = null;
            }
        }
    }
}


[NaAssetModule<RUpdator, UpdatorState>]
public partial class UpdatorModule()
    : AssetModuleBase(
        children: SystemChain.Empty
            .Add<UpdatorActivationStateChangeSystem>());