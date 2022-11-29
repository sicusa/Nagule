namespace Nagule;

using Aeco;
using Aeco.Reactive;

public abstract class ReactiveObjectUpdatorBase<TObject> : VirtualLayer, IUpdateListener, ILateUpdateListener
    where TObject : IReactiveComponent
{
    private Query<TObject, Destroy> _destroy_q = new();

    public virtual void OnUpdate(IContext context, float deltaTime)
    {
        foreach (var id in context.Query<TObject>()) {
            if (context.Contains<Modified<TObject>>(id)) {
                UpdateObject(context, id);
            }
        }
    }

    public virtual void OnLateUpdate(IContext context, float deltaTime)
    {
        foreach (var id in _destroy_q.Query(context)) {
            ReleaseObject(context, id);
        }
    }

    protected abstract void UpdateObject(IContext context, Guid id);
    protected abstract void ReleaseObject(IContext context, Guid id);
}

public abstract class ReactiveObjectUpdatorBase<TObject, TDirtyTag> : VirtualLayer, IUpdateListener, ILateUpdateListener
    where TObject : IReactiveComponent
    where TDirtyTag : Aeco.IComponent
{
    private Query<TObject, Destroy> _destroy_q = new();

    public virtual void OnUpdate(IContext context, float deltaTime)
    {
        foreach (var id in context.Query<TObject>()) {
            bool dirty = context.Contains<TDirtyTag>(id);
            if (dirty || context.Contains<Modified<TObject>>(id)) {
                UpdateObject(context, id, dirty);
            }
        }
    }

    public virtual void OnLateUpdate(IContext context, float deltaTime)
    {
        foreach (var id in _destroy_q.Query(context)) {
            ReleaseObject(context, id);
        }
    }

    protected abstract void UpdateObject(IContext context, Guid id, bool dirty);
    protected abstract void ReleaseObject(IContext context, Guid id);
}