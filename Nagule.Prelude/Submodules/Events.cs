namespace Nagule.Prelude;

using Sia;

[SiaTemplate(nameof(Events))]
[NaAsset]
public record REvents : RFeatureBase
{
    public Action<World, EntityRef>? Start { get; init; }
    public Action<World, EntityRef>? Destroy { get; init; }
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

    protected override void LoadAsset(EntityRef entity, ref Events asset)
    {
        var node = entity.GetFeatureNode();
        asset.Start?.Invoke(World, node);

        var eventListener = asset.Listener;
        if (eventListener != null) {
            World.Dispatcher.Listen(node, eventListener);
        }
    }

    protected override void UnloadAsset(EntityRef entity, ref Events asset)
    {
        var node = entity.GetFeatureNode();

        var eventListener = asset.Listener;
        if (eventListener != null) {
            World.Dispatcher.Unlisten(node, eventListener);
        }

        entity.Get<Events>().Destroy?.Invoke(World, node);
    }
}

[NaAssetModule<REvents>]
public partial class EventsModule;