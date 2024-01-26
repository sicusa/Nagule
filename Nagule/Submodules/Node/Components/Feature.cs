namespace Nagule;

using Sia;

public record struct Feature
{
    public readonly record struct OnTransformChanged(EntityRef Node) : IEvent;

    public EntityRef Node { get; private set; }

    internal Feature(EntityRef node)
    {
        Node = node;
    }

    public readonly record struct SetNode(EntityRef Value)
        : ICommand<Feature>, IReconstructableCommand<SetNode>
    {
        public static SetNode ReconstructFromCurrentState(in EntityRef entity)
            => new(entity.Get<Feature>().Node);

        public void Execute(World world, in EntityRef target)
            => Execute(world, target, ref target.Get<Feature>());

        public void Execute(World world, in EntityRef target, ref Feature component)
        {
            ref var prevNodeFeatures = ref component.Node.GetState<NodeState>().FeaturesRaw;
            ref var newNodeFeatures = ref Value.GetState<NodeState>().FeaturesRaw;

            prevNodeFeatures!.Remove(target);
            component.Node.Unrefer(target);

            newNodeFeatures ??= [];
            newNodeFeatures.Add(target);

            Value.Refer(target);
            component.Node = Value;

            world.Send(target, new OnTransformChanged(Value));
        }
    }
}