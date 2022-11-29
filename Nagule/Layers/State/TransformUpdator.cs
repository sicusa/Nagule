namespace Nagule;

using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

using Aeco;
using Aeco.Reactive;

public class TransformUpdator : VirtualLayer, IUpdateListener, ILateUpdateListener
{
    private Query<Modified<Transform>, Transform> _transformModified = new();
    private Query<Transform, Destroy> _transformDestroyed = new();
    private Query<Modified<Parent>, Parent> _parentModified = new();

    public unsafe void OnUpdate(IContext context, float deltaTime)
    {
        HashSet<Guid>? dirtyIds = null;
        foreach (var id in _transformModified.Query(context)) {
            dirtyIds ??= context.AcquireAny<DirtyTransforms>().Ids;
            TagDirty(context, id, dirtyIds);
        }

        foreach (var id in _transformDestroyed.Query(context)) {
            if (context.TryGet<Transform>(id, out var transform)) {
            }
        }

        foreach (var id in context.Query<Removed<Parent>>()) {
            if (!context.Remove<AppliedParent>(id, out var parent)) {
                continue;
            }
            RemoveChild(context, parent.Id, id);
        }

        foreach (var id in _parentModified.Query(context)) {
            ref readonly var parent = ref context.Inspect<Parent>(id);
            ref var appliedParent = ref context.Acquire<AppliedParent>(id, out bool exists);

            if (exists && parent.Id != appliedParent.Id) {
                ref var prevChildren = ref context.Acquire<Children>(appliedParent.Id);
                prevChildren.Ids = prevChildren.Ids.Remove(id);
                RemoveChild(context, appliedParent.Id, id);
            }
            if (parent.Id == Guid.Empty) {
                Console.WriteLine("Parent ID should not be empty.");
                continue;
            }

            ref var children = ref context.Acquire<Children>(parent.Id);
            children.Ids = children.Ids.Add(id);
            appliedParent.Id = parent.Id;
            AddChild(context, parent.Id, id);
        }
    }

    public void OnLateUpdate(IContext context, float deltaTime)
        => context.AcquireAny<DirtyTransforms>().Ids.Clear();

    private unsafe ref Transform GetTransform(IContext context, Guid id)
    {
        ref var transform = ref context.UnsafeAcquire<Transform>(id);
        if (transform.Id == Guid.Empty) {
            transform.Id = id;
        }
        return ref transform;
    }

    private unsafe void TagDirty(IContext context, Guid id, HashSet<Guid> dirtyIds)
    {
        dirtyIds.Add(id);
        if (context.TryGet<Children>(id, out var children)) {
            TagChildrenDirty(context, in children, dirtyIds);
        }
    }

    private unsafe void TagChildrenDirty(IContext context, in Children children, HashSet<Guid> dirtyIds)
    {
        var childrenIds = children.Ids;
        for (int i = 0; i != childrenIds.Length; ++i) {
            var childId = childrenIds[i];
            dirtyIds.Add(childId);
            if (context.TryGet<Children>(childId, out var sub)) {
                TagChildrenDirty(context, in sub, dirtyIds);
            }
        }
    }

    private unsafe void AddChild(IContext context, Guid parentId, Guid childId)
    {
        ref var parentTrans = ref GetTransform(context, parentId);
        var childrenPtr = parentTrans.Children;
        var childrenCount = parentTrans.ChildrenCount;

        if (childrenPtr == null) {
            var childrenBuffer = new IntPtr[Transform.InitialChildrenCapacity];
            var childrenHandle = GCHandle.Alloc(childrenBuffer, GCHandleType.Pinned);
            childrenPtr = (Transform**)childrenHandle.AddrOfPinnedObject();
            parentTrans.ChildrenHandle = childrenHandle;
            parentTrans.ChildrenCapacity = childrenBuffer.Length;
            parentTrans.Children = childrenPtr;
        }
        else if (parentTrans.ChildrenCount >= parentTrans.ChildrenCapacity) {
            var childrenBuffer = new IntPtr[parentTrans.ChildrenCapacity * 2];
            var childrenHandle = GCHandle.Alloc(childrenBuffer, GCHandleType.Pinned);

            int length = sizeof(Transform*) * parentTrans.ChildrenCount;
            childrenPtr = (Transform**)childrenHandle.AddrOfPinnedObject();
            System.Buffer.MemoryCopy((Transform**)parentTrans.Children, childrenPtr, length, length);

            parentTrans.ChildrenHandle.Free();
            parentTrans.ChildrenHandle = childrenHandle;
            parentTrans.ChildrenCapacity *= 2;
            parentTrans.Children = childrenPtr;
        }

        ref var transform = ref GetTransform(context, childId);
        *(childrenPtr + parentTrans.ChildrenCount) = (Transform*)Unsafe.AsPointer(ref transform);
        ++parentTrans.ChildrenCount;

        transform.Parent = (Transform*)Unsafe.AsPointer(ref parentTrans);
        transform.TagDirty();
    }

    private unsafe bool RemoveChild(IContext context, Guid parentId, Guid childId)
    {
        if (parentId == Guid.Empty) {
            Console.WriteLine("Internal error: applied parent ID is empty.");
            return false;
        }

        ref var parentTrans = ref GetTransform(context, parentId);
        var childrenPtr = parentTrans.Children;
        var childrenCount = parentTrans.ChildrenCount;

        for (int i = 0; i != childrenCount; ++i) {
            var childPtr = *(childrenPtr + i);
            if (childPtr->Id != childId) {
                continue;
            }

            for (int j = i + 1; j != childrenCount; ++j) {
                *(childrenPtr + j - 1) = *(childrenPtr + j);
            }
            --parentTrans.ChildrenCount;

            ref var transform = ref GetTransform(context, childId);
            transform.Parent = null;
            transform.TagDirty();
            return true;
        }

        Console.WriteLine("Internal error: children not found in transform tree.");
        return false;
    }
}