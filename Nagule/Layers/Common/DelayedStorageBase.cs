namespace Nagule;

using System.Diagnostics.CodeAnalysis;

using Aeco.Local;

public abstract class DelayedStorageBase<TComponent> : MonoPoolStorage<TComponent>, ILoadListener
    where TComponent : IDelayedComponent, new()
{
    [AllowNull]
    protected IContext Context { get; private set; }

    public void OnLoad(IContext context)
        => Context = context;

    public override ref TComponent Acquire(Guid entityId, out bool exists)
    {
        ref TComponent comp = ref base.Acquire(entityId, out exists);
        if (!exists || comp.Dirty) {
            OnRefresh(entityId, ref comp);
            comp.Dirty = false;
        }
        return ref comp;
    }

    public override sealed ref TComponent Require(Guid entityId)
        => ref Acquire(entityId);

    public override sealed ref readonly TComponent Inspect(Guid entityId)
        => ref Acquire(entityId);

    public override bool TryGet(Guid entityId, [MaybeNullWhen(false)] out TComponent comp)
    {
        if (base.TryGet(entityId, out comp)) {
            if (comp.Dirty) {
                OnRefresh(entityId, ref comp);
                comp.Dirty = false;
            }
            return true;
        }
        return false;
    }

    protected abstract void OnRefresh(Guid id, ref TComponent comp);
}
