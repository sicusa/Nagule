namespace Nagule;

using Aeco;
using Aeco.Reactive;

public abstract class ResourceManagerBase<TObject, TObjectData, TResource>
    : VirtualLayer, IUpdateListener, ILateUpdateListener
    where TObject : IResourceObject<TResource>
    where TObjectData : IComponent, new()
    where TResource : IResource
{
    private Query<Modified<TObject>, TObject> _q = new();
    private Query<Destroy, TObject> _destroy_q = new();

    public virtual void OnUpdate(IContext context, float deltaTime)
    {
        foreach (var id in _q.Query(context)) {
            try {
                ref var obj = ref context.UnsafeInspect<TObject>(id);
                if (obj.Resource == null) {
                    throw new Exception("Resource not set");
                }

                ref var data = ref context.Acquire<TObjectData>(id, out bool exists);
                if (exists) {
                    Initialize(context, id, ref obj, ref data, true);
                }
                else {
                    Initialize(context, id, ref obj, ref data, false);
                    ResourceLibrary<TResource>.Register(context, obj.Resource, id);
                }
            }
            catch (Exception e) {
                Console.WriteLine($"Failed to initialize {typeof(TObject)} [{id}]: " + e);
            }
        }
    }

    public virtual void OnLateUpdate(IContext context, float deltaTime)
        => DoUninitialize(context, _destroy_q.Query(context));

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
                if (!ResourceLibrary<TResource>.Unregister(context, obj.Resource, id)) {
                    throw new KeyNotFoundException($"{typeof(TObject)} [{id}] not found in resource library.");
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