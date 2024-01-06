using Sia;

namespace Nagule;

public class Node3DManager : NodeManager<Node3D, RNode3D>
{
    public override void OnInitialize(World world)
    {
        base.OnInitialize(world);

        Listen((in EntityRef entity, in Node3D.SetFeatures cmd) => SetFeatures(entity, cmd.Value));
        Listen((in EntityRef entity, in Node3D.AddFeature cmd) => AddFeature(entity, cmd.Value));
        Listen((in EntityRef entity, in Node3D.SetFeature cmd) => SetFeature(entity, cmd.Index, cmd.Value));
        Listen((in EntityRef entity, in Node3D.RemoveFeature cmd) => RemoveFeature(entity, cmd.Value));
    }
}