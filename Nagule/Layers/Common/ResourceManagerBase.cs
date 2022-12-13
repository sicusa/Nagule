namespace Nagule;

using Aeco;
using Aeco.Reactive;

public abstract class ResourceManagerBase<TObject, TObjectData, TResource>
    : VirtualLayer, IUpdateListener, ILateUpdateListener
    where TObject : IResourceObject<TResource>
    where TObjectData : IComponent, new()
    where TResource : IResource
{
    protected Group<Modified<TObject>, TObject> ObjectGroup { get; } = new();
    protected Group<TObject, Destroy> DestroyedObjectGroup { get; } = new();

    private void OnResourceObjectCreated(IContext context, in TResource resource, Guid id)
    {
        ObjectGroup.Add(id);
    }

    public virtual void OnUpdate(IContext context, float deltaTime)
    {
        ObjectGroup.Refresh(context);
        if (ObjectGroup.Count == 0) { return; }

        ResourceLibrary<TResource>.OnResourceObjectCreated += OnResourceObjectCreated;

        int initialCount = 0;
        int offset = 0;

        do {
            offset = initialCount;
            initialCount = ObjectGroup.Count;

            for (int i = offset; i != initialCount; ++i) {
                var id = ObjectGroup[i];
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
        } while (ObjectGroup.Count != initialCount);

        ResourceLibrary<TResource>.OnResourceObjectCreated -= OnResourceObjectCreated;
    }

    public virtual void OnLateUpdate(IContext context, float deltaTime)
    {
        DestroyedObjectGroup.Refresh(context);

        foreach (var id in DestroyedObjectGroup) {
            try {
                if (!context.Remove<TObject>(id, out var obj)) {
                    throw new KeyNotFoundException($"{typeof(TObject)} [{id}] does not have object component.");
                }
                if (!context.Remove<TObjectData>(id, out var data)) {
                    throw new KeyNotFoundException($"{typeof(TObject)} [{id}] does not have object data component.");
                }
                Uninitialize(context, id, in obj, in data);

                if (!ResourceLibrary<TResource>.Unregister(context, obj.Resource, id)) {
                    throw new KeyNotFoundException($"{typeof(TObject)} [{id}] not found in resource library.");
                }
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