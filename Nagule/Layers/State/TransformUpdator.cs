namespace Nagule;

using Aeco;
using Aeco.Reactive;

public class TransformUpdator : Layer, IEngineUpdateListener, ILateUpdateListener
{
    private Query<Created<Transform>, Transform> _createdTransformQuery = new();
    private Query<Modified<Transform>, Transform> _modifiedTransformQuery = new();
    private Query<Modified<Parent>, Parent> _modifiedParentQuery = new();
    private Group<Transform, Destroy> _destroyedTransformGroup = new();

    public unsafe void OnEngineUpdate(IContext context)
    {
        foreach (var id in context.Query<Removed<Parent>>()) {
            if (!context.Remove<AppliedParent>(id, out var parent)) {
                continue;
            }
            RemoveChild(context, parent.Id, id);
        }

        foreach (var id in _modifiedParentQuery.Query(context)) {
            ref readonly var parent = ref context.Inspect<Parent>(id);
            ref var appliedParent = ref context.Acquire<AppliedParent>(id, out bool exists);

            if (exists) {
                if (parent.Id == appliedParent.Id) {
                    continue;
                }
                ref var prevChildren = ref context.Acquire<Children>(appliedParent.Id);
                prevChildren.IdsRaw.Remove(id);
                RemoveChild(context, appliedParent.Id, id);
            }
            if (parent.Id == Guid.Empty) {
                Console.WriteLine("Parent ID should not be empty.");
                continue;
            }

            ref var children = ref context.Acquire<Children>(parent.Id);
            children.IdsRaw.Add(id);
            appliedParent.Id = parent.Id;
            AddChild(context, parent.Id, id);
        }

        foreach (var id in _modifiedTransformQuery.Query(context)) {
            TagDirty(context, id);
        }
    }

    public void OnLateUpdate(IContext context)
    {
        foreach (var id in _destroyedTransformGroup.Query(context)) {
            ReleaseTransform(context, id);
        }
        context.DirtyTransformIds.Clear();
    }

    private unsafe void TagDirty(IContext context, Guid id)
    {
        context.DirtyTransformIds.Add(id);
        if (!context.TryGet<Children>(id, out var children)) {
            return;
        }
        var childrenIds = children.IdsRaw;
        for (int i = 0; i != childrenIds.Count; ++i) {
            TagDirty(context, childrenIds[i]);
        }
    }

    private unsafe void ReleaseTransform(IContext context, Guid id)
    {
        if (context.Remove<AppliedParent>(id, out var parent)) {
            RemoveChild(context, parent.Id, id);
        }

        if (context.Remove<Children>(id, out var children)) {
            var childrenIds = children.IdsRaw;
            for (int i = 0; i != childrenIds.Count; ++i) {
                var childId = childrenIds[i];
                context.Destroy(childId);
                ReleaseTransform(context, childId);
            }
        }
    }

    private unsafe void AddChild(IContext context, Guid parentId, Guid childId)
    {
        ref var parentTrans = ref context.Acquire<Transform>(parentId);
        ref var childTrans = ref context.Acquire<Transform>(childId);

        var children = parentTrans.Children;
        if (children == null) {
            children = new();
            parentTrans.Children = children;
        }

        childTrans.Parent = new TransformRef(context.GetRef<Transform>(parentId));
        childTrans.TagDirty();
        children.Add(context.GetRef<Transform>(childId));
    }

    private unsafe bool RemoveChild(IContext context, Guid parentId, Guid childId)
    {
        if (parentId == Guid.Empty) {
            Console.WriteLine("Internal error: applied parent ID is empty.");
            return false;
        }

        ref var parentTrans = ref context.Acquire<Transform>(parentId);
        var children = parentTrans.Children;
        if (children == null) {
            Console.WriteLine("Internal error: child not found.");
            return false;
        }

        for (int i = 0; i != children.Count; ++i) {
            var childRef = children[i];
            if (childRef.Id != childId) {
                continue;
            }

            ref var transform = ref childRef.GetRef();
            transform.Parent = null;
            transform.TagDirty();

            children.RemoveAt(i);
            return true;
        }

        Console.WriteLine("Internal error: child not found.");
        return false;
    }
}