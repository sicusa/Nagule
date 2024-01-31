using Sia;

namespace Nagule;

public class Node3DManager : NodeManagerBase<Node3D, RNode3D>
{
    private class EventListener(World world) : IEventListener
    {
        public bool OnEvent<TEvent>(in EntityRef entity, in TEvent e) where TEvent : IEvent
        {
            var eventType = typeof(TEvent);
            if (eventType == typeof(Transform3D.OnChanged)) {
                SendEventToFeatures(world, entity, new Feature.OnNodeTransformChanged(entity));
            }
            else if (eventType == typeof(Node3D.SetIsEnabled)) {
                SetNodeIsEnabledRecursively(world, entity, true);
            }
            return false;
        }
    }

    private EventListener? _eventListener;

    public override void OnInitialize(World world)
    {
        base.OnInitialize(world);
        _eventListener = new(world);

        Listen((in EntityRef entity, in Node3D.SetFeatures cmd) => SetFeatures(entity, cmd.Value));
        Listen((in EntityRef entity, in Node3D.AddFeature cmd) => AddFeature(entity, cmd.Value));
        Listen((in EntityRef entity, in Node3D.SetFeature cmd) => SetFeature(entity, cmd.Index, cmd.Value));
        Listen((in EntityRef entity, in Node3D.RemoveFeature cmd) => RemoveFeature(entity, cmd.Value));
    }

    protected override void LoadAsset(EntityRef entity, ref Node3D asset, EntityRef stateEntity)
    {
        base.LoadAsset(entity, ref asset, stateEntity);
        World.Dispatcher.Listen(entity, _eventListener!);
    }
}