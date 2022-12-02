namespace Nagule;

using Aeco;
using Aeco.Reactive;

public abstract class ReactiveObjectUpdatorBase<TObject> : VirtualLayer, IEngineUpdateListener, ILateUpdateListener
    where TObject : IReactiveComponent
{
    private Query<Modified<TObject>, TObject> _q = new();
    private Query<TObject, Destroy> _destroyQ = new();

    public virtual void OnEngineUpdate(IContext context, float deltaTime)
    {
        foreach (var id in _q.Query(context)) {
            UpdateObject(context, id);
        }
    }

    public virtual void OnLateUpdate(IContext context, float deltaTime)
    {
        foreach (var id in _destroyQ.Query(context)) {
            ReleaseObject(context, id);
        }
    }

    protected abstract void UpdateObject(IContext context, Guid id);
    protected abstract void ReleaseObject(IContext context, Guid id);
}

public abstract class ReactiveObjectUpdatorBase<TObject, TDirtyTag> : VirtualLayer, IEngineUpdateListener, ILateUpdateListener
    where TObject : IReactiveComponent
    where TDirtyTag : Aeco.IComponent
{
    private Query<TObject, Destroy> _destroyQ = new();

    public virtual void OnEngineUpdate(IContext context, float deltaTime)
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
        foreach (var id in _destroyQ.Query(context)) {
            ReleaseObject(context, id);
        }
    }

    protected abstract void UpdateObject(IContext context, Guid id, bool dirty);
    protected abstract void ReleaseObject(IContext context, Guid id);
}