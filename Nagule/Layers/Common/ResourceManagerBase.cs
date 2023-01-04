namespace Nagule;

using Aeco;
using Aeco.Reactive;

public abstract class ResourceManagerBase<TResource, TObjectData>
    : VirtualLayer, IUpdateListener, ILateUpdateListener
    where TObjectData : IComponent, new()
    where TResource : IResource
{
    protected Group<Modified<Resource<TResource>>, Resource<TResource>> ObjectGroup { get; } = new();
    protected Group<Resource<TResource>, Destroy> DestroyedObjectGroup { get; } = new();

    public virtual void OnUpdate(IContext context)
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
                    ref var res = ref context.InspectRaw<Resource<TResource>>(id);
                    if (res.Value == null) {
                        throw new Exception("Resource not set");
                    }
                    ref var data = ref context.Acquire<TObjectData>(id, out bool exists);
                    if (exists) {
                        Initialize(context, id, res.Value, ref data, true);
                        Console.WriteLine($"{typeof(TResource)} reinitialized: " + DebugHelper.Print(context, id));
                    }
                    else {
                        Initialize(context, id, res.Value, ref data, false);
                        ResourceLibrary<TResource>.Register(context, res.Value, id);
                        Console.WriteLine($"{typeof(TResource)} initialized: " + DebugHelper.Print(context, id));
                    }
                }
                catch (Exception e) {
                    Console.WriteLine($"Failed to initialize {typeof(TResource)} [{id}]: " + e);
                }
            }
        } while (ObjectGroup.Count != initialCount);

        ResourceLibrary<TResource>.OnResourceObjectCreated -= OnResourceObjectCreated;
    }

    private void OnResourceObjectCreated(IContext context, in TResource resource, Guid id)
    {
        ObjectGroup.Add(id);
    }

    public void OnLateUpdate(IContext context)
    {
        DestroyedObjectGroup.Refresh(context);
        foreach (var id in DestroyedObjectGroup) {
            try {
                if (!context.Remove<Resource<TResource>>(id, out var res)) {
                    throw new KeyNotFoundException($"{typeof(TResource)} [{id}] does not have object component.");
                }
                if (!context.Remove<TObjectData>(id, out var data)) {
                    throw new KeyNotFoundException($"{typeof(TResource)} [{id}] does not have object data component.");
                }
                Uninitialize(context, id, res.Value!, in data);

                if (!ResourceLibrary<TResource>.Unregister(context, res.Value!, id)) {
                    throw new KeyNotFoundException($"{typeof(TResource)} [{id}] not found in resource library.");
                }
                Console.WriteLine($"{typeof(TResource)} uninitialized: " + DebugHelper.Print(context, id));
            }
            catch (Exception e) {
                Console.WriteLine($"Failed to uninitialize {typeof(TResource)} [{id}]: " + e);
            }
        }
    }

    protected abstract void Initialize(
        IContext context, Guid id, TResource resource, ref TObjectData data, bool updating);
    protected abstract void Uninitialize(
        IContext context, Guid id, TResource resource, in TObjectData data);
}