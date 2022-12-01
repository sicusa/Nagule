namespace Nagule;

using Aeco;
using Aeco.Reactive;

public abstract class ObjectManagerBase<TObject, TObjectData>
    : VirtualLayer, IUpdateListener, ILateUpdateListener
    where TObject : IPooledComponent
    where TObjectData : IComponent, new()
{
    private Guid _libraryId = Guid.NewGuid();
    private Query<Modified<TObject>, TObject> _q = new();
    private Query<Destroy, TObject> _destroyQ = new();

    public virtual void OnUpdate(IContext context, float deltaTime)
    {
        foreach (var id in _q.Query(context)) {
            try {
                ref var obj = ref context.UnsafeInspect<TObject>(id);
                ref var data = ref context.Acquire<TObjectData>(id, out bool exists);

                if (exists) {
                    Initialize(context, id, ref obj, ref data, true);
                }
                else {
                    Initialize(context, id, ref obj, ref data, false);
                }
            }
            catch (Exception e) {
                Console.WriteLine($"Failed to initialize {typeof(TObject)} [{id}]: " + e);
            }
        }
    }

    public virtual void OnLateUpdate(IContext context, float deltaTime)
        => DoUninitialize(context, _destroyQ.Query(context));

    private void DoUninitialize(IContext context, IEnumerable<Guid> ids)
    {
        foreach (var id in ids) {
            try {
                if (!context.Remove<TObject>(id, out var obj)) {
                    throw new KeyNotFoundException($"{typeof(TObject)} [{id}] does not have object component.");
                }
                if (!context.Remove<TObjectData>(id, out var data)) {
                    throw new KeyNotFoundException($"{typeof(TObject)} [{id}] does not have object data component.");
                }
                Uninitialize(context, id, in obj, in data);
            }
            catch (Exception e) {
                Console.WriteLine($"Failed to uninitialize {typeof(TObject)} [{id}]: " + e);
            }
        }
    }

    protected abstract void Initialize(
        IContext context, Guid id, ref TObject obj, ref TObjectData data, bool updating);
    protected abstract void Uninitialize(
        IContext context, Guid id, in TObject obj, in TObjectData data);
}