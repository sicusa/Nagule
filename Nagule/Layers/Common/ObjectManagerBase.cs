namespace Nagule;

using Aeco;
using Aeco.Reactive;

public abstract class ObjectManagerBase<TObject, TObjectData>
    : VirtualLayer, IUpdateListener, ILateUpdateListener
    where TObject : IPooledComponent
    where TObjectData : IComponent, new()
{
    protected Query<Modified<TObject>, TObject> ModifiedObjectQuery { get; } = new();
    protected Group<Destroy, TObject> DestroyedObjectGroup { get; } = new();

    public virtual void OnUpdate(IContext context)
    {
        foreach (var id in ModifiedObjectQuery.Query(context)) {
            try {
                ref var obj = ref context.InspectRaw<TObject>(id);
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

    public virtual void OnLateUpdate(IContext context)
    {
        foreach (var id in DestroyedObjectGroup.Query(context)) {
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