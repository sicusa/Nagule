namespace Nagule;

using Aeco;
using Aeco.Reactive;

public abstract class ReactiveUpdatorBase<TObject> : Layer, IEngineUpdateListener, ILateUpdateListener
    where TObject : IReactiveComponent
{
    protected Query<Modified<TObject>, TObject> ModifiedObjectQuery { get; } = new();
    protected Group<TObject, Destroy> DestroyedObjectGroup { get; } = new();

    public virtual void OnEngineUpdate(IContext context)
    {
        foreach (var id in ModifiedObjectQuery.Query(context)) {
            Update(context, id);
        }
    }

    public virtual void OnLateUpdate(IContext context)
    {
        foreach (var id in DestroyedObjectGroup.Query(context)) {
            Release(context, id);
        }
    }

    protected abstract void Update(IContext context, uint id);
    protected abstract void Release(IContext context, uint id);
}

public abstract class ReactiveUpdatorBase<TObject, TDirtyTag> : Layer, IEngineUpdateListener, ILateUpdateListener
    where TObject : IReactiveComponent
    where TDirtyTag : IComponent
{
    protected Group<TObject, Destroy> DestroyedObjectGroup { get; } = new();

    public virtual void OnEngineUpdate(IContext context)
    {
        foreach (var id in context.Query<TObject>()) {
            bool dirty = context.Contains<TDirtyTag>(id);
            if (dirty || context.Contains<Modified<TObject>>(id)) {
                Update(context, id, dirty);
            }
        }
    }

    public virtual void OnLateUpdate(IContext context)
    {
        DestroyedObjectGroup.Refresh(context);

        foreach (var id in DestroyedObjectGroup) {
            Release(context, id);
        }
    }

    protected abstract void Update(IContext context, uint id, bool dirty);
    protected abstract void Release(IContext context, uint id);
}