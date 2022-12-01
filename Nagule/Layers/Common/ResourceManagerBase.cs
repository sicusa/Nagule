namespace Nagule;

using Aeco;
using Aeco.Reactive;

public abstract class ResourceManagerBase<TObject, TObjectData, TResource>
    : VirtualLayer, IUpdateListener, ILateUpdateListener
    where TObject : IResourceObject<TResource>
    where TObjectData : IComponent, new()
    where TResource : IResource
{
    private Group<Modified<TObject>, TObject> _g = new();
    private Query<TObject, Destroy> _destroyQ = new();

    private void OnResourceObjectCreated(IContext context, in TResource resource, Guid id)
    {
        _g.Add(id);
    }

    public virtual void OnUpdate(IContext context, float deltaTime)
    {
        _g.Refresh(context);
        if (_g.Count == 0) { return; }

        ResourceLibrary<TResource>.OnResourceObjectCreated += OnResourceObjectCreated;

        int initialCount = 0;
        int offset = 0;

        do {
            offset = initialCount;
            initialCount = _g.Count;

            for (int i = offset; i != initialCount; ++i) {
                var id = _g[i];
                try {
                    ref var obj = ref context.UnsafeInspect<TObject>(id);
                    if (obj.Resource == null) {
                        throw new Exception("Resource not set");
                    }
                    ref var data = ref context.Acquire<TObjectData>(id, out bool exists);
                    if (exists) {
                        Initialize(context, id, ref obj, ref data, true);
                        Console.WriteLine($"{typeof(TObject)} reinitialized: " + DebugHelper.Print(context, id));
                    }
                    else {
                        Initialize(context, id, ref obj, ref data, false);
                        ResourceLibrary<TResource>.Register(context, obj.Resource, id);
                        Console.WriteLine($"{typeof(TObject)} initialized: " + DebugHelper.Print(context, id));
                    }
                }
                catch (Exception e) {
                    Console.WriteLine($"Failed to initialize {typeof(TObject)} [{id}]: " + e);
                }
            }
        } while (_g.Count != initialCount);

        ResourceLibrary<TResource>.OnResourceObjectCreated -= OnResourceObjectCreated;
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