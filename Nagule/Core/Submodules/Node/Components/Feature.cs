namespace Nagule;

using Sia;

public record struct Feature
{
    public readonly record struct OnTransformChanged(EntityRef Node) : IEvent;

    public EntityRef Node { get; private set; }

    public Feature(EntityRef node)
    {
        Node = node;
    }

    public readonly record struct SetNode(EntityRef Value)
        : ICommand, ICommand<Feature>, IReconstructableCommand<SetNode>
    {
        public static SetNode ReconstructFromCurrentState(in EntityRef entity)
            => new(entity.Get<Feature>().Node);

        public void Execute(World world, in EntityRef target)
            => Execute(world, target, ref target.Get<Feature>());

        public void Execute(World world, in EntityRef target, ref Feature component)
        {
            var prevNodeFeatures = component.Node.GetState<Node3DState>().Features;
            var newNodeFeatures = Value.GetState<Node3DState>().Features;

            prevNodeFeatures.Remove(target);
            component.Node.UnreferAsset(target);

            newNodeFeatures.Add(target);
            Value.ReferAsset(target);

            component.Node = Value;
            world.Send(target, new OnTransformChanged(Value));
        }
    }
}