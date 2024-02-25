namespace Nagule.Prelude;

using System.Runtime.CompilerServices;
using Sia;

[SiaTemplate(nameof(Events))]
[NaAsset]
public record REvents : RFeatureBase
{
    public Action<World, EntityRef>? OnStart { get; init; }
    public Action<World, EntityRef>? OnDestroy { get; init; }
    public Action<World, EntityRef>? OnEnable { get; init; }
    public Action<World, EntityRef>? OnDisable { get; init; }
    public IEventListener? Listener { get; init; }
}

public partial class EventsManager
{
    public override void OnInitialize(World world)
    {
        base.OnInitialize(world);

        Listen((in EntityRef entity, ref Events snapshot, in Events.SetListener cmd) => {
            var node = entity.GetFeatureNode();

            var dispatcher = World.Dispatcher;
            if (snapshot.Listener != null) {
                dispatcher.Unlisten(node, snapshot.Listener);
            }

            var newListener = cmd.Value;
            if (newListener != null) {
                dispatcher.Listen(node, newListener);
            }
        });
    }

    public override void LoadAsset(in EntityRef entity, ref Events asset, EntityRef stateEntity)
    {
        var node = entity.GetFeatureNode();
        asset.OnStart?.Invoke(World, node);

        ref var nodeHierarchy = ref node.Get<NodeHierarchy>();
        if (nodeHierarchy.IsEnabled) {
            asset.OnEnable?.Invoke(World, node);
        }

        var eventListener = asset.Listener;
        if (eventListener != null) {
            World.Dispatcher.Listen(node, eventListener);
        }
    }

    public override void UnloadAsset(in EntityRef entity, in Events asset, EntityRef stateEntity)
    {
        var node = entity.GetFeatureNode();

        var eventListener = asset.Listener;
        if (eventListener != null) {
            World.Dispatcher.Unlisten(node, eventListener);
        }

        entity.Get<Events>().OnDestroy?.Invoke(World, node);
    }
}

public class IsEnabledEventNotifySystem()
    : SystemBase(
        matcher: Matchers.Of<Events>(),
        trigger: EventUnion.Of<Feature.OnIsEnabledChanged>())
{
    public override void Execute(World world, Scheduler scheduler, IEntityQuery query)
    {
        foreach (var entity in query) {
            ref var events = ref entity.Get<Events>();
            ref var feature = ref entity.Get<Feature>();
            
            if (feature.IsEnabled) {
                events.OnEnable?.Invoke(world, feature.Node);
            }
            else {
                events.OnDisable?.Invoke(world, feature.Node);
            }
        }
    }
}

[NaAssetModule<REvents>]
public partial class EventsModule()
    : AssetModuleBase(
        children: SystemChain.Empty
            .Add<IsEnabledEventNotifySystem>());