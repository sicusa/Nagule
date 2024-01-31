namespace Nagule;

using Sia;

public static class TransformUtils
{
    private const TransformDirtyTags _globalTags = TransformDirtyTags.Globals;

    public static void NotifyDirty(World world, EntityRef entity)
    {
        world.Send(entity, Transform3D.OnChanged.Instance);

        foreach (var child in entity.Get<NodeHierarchy>()) {
            ref var childTrans = ref child.Get<Transform3D>();
            if ((childTrans.DirtyTags & _globalTags) != _globalTags) {
                childTrans.DirtyTags |= _globalTags;
                NotifyDirty(world, child);
            }
        }
    }
}