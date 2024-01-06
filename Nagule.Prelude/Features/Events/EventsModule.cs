namespace Nagule.Prelude;

using Sia;

public class EventsManager : AssetManager<Events, REvents>
{
    public override void OnInitialize(World world)
    {
        base.OnInitialize(world);

        Listen((EntityRef entity, ref Events snapshot, in Events.SetListener cmd) => {
            var node = entity.GetFeatureNode();
            if (!node.Valid) { return; }

            var dispatcher = World.Dispatcher;
            if (snapshot.Listener != null) {
                dispatcher.Unlisten(entity, snapshot.Listener);
            }
            var newListener = cmd.Value;
            if (newListener != null) {
                dispatcher.Listen(entity, newListener);
            }
        });
    }

    protected override void LoadAsset(EntityRef entity, ref Events asset)
    {
        var node = entity.GetFeatureNode();
        if (!node.Valid) { return; }

        asset.Start?.Invoke(World, node);

        var eventListener = asset.Listener;
        if (eventListener != null) {
            World.Dispatcher.Listen(node, eventListener);
        }
    }

    protected override void UnloadAsset(EntityRef entity, ref Events asset)
    {
        var node = entity.GetFeatureNode();
        if (!node.Valid) { return; }

        var eventListener = asset.Listener;
        if (eventListener != null) {
            World.Dispatcher.Unlisten(node, eventListener);
        }

        entity.Get<Events>().Destroy?.Invoke(World, node);
    }
}

public class EventsModule : AddonSystemBase
{
    public override void Initialize(World world, Scheduler scheduler)
    {
        base.Initialize(world, scheduler);
        AddAddon<EventsManager>(world);
    }
}