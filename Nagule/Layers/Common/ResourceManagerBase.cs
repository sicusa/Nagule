namespace Nagule;

using Aeco;
using Aeco.Reactive;

public abstract class ResourceManagerBase<TResource>
    : Layer, IResourceUpdateListener, ILateUpdateListener
    where TResource : IResource
{
    protected Group<Modified<Resource<TResource>>, Resource<TResource>> ObjectGroup { get; } = new();
    protected Group<Removed<Resource<TResource>>> RemovedObjectGroup { get; } = new();
    protected Group<Resource<TResource>, Destroy> DestroyedObjectGroup { get; } = new();

    public virtual void OnResourceUpdate(IContext context)
    {
        ObjectGroup.Refresh(context);
        if (ObjectGroup.Count == 0) { return; }

        ref var resLib = ref context.Acquire<ResourceLibrary>();
        resLib.OnResourceObjectCreated += OnResourceObjectCreated;

        int initialCount = 0;
        int offset = 0;

        do {
            offset = initialCount;
            initialCount = ObjectGroup.Count;

            for (int i = offset; i != initialCount; ++i) {
                var id = ObjectGroup[i];
                try {
                    var resource = context.InspectRaw<Resource<TResource>>(id).Value;
                    if (resource == null) {
                        throw new ArgumentNullException("Resource not set");
                    }
                    ref var initializedRes = ref context.Acquire<InitializedResource<TResource>>(id, out bool exists);

                    if (exists) {
                        if (Object.ReferenceEquals(initializedRes.Value, resource)) {
                            continue;
                        }
                        initializedRes.Subscription?.Dispose();
                        Initialize(context, id, resource, initializedRes.Value);
                    }
                    else {
                        Initialize(context, id, resource, default);
                    }

                    initializedRes.Value = resource;
                    initializedRes.Subscription = Subscribe(context, id, resource);
                }
                catch (Exception e) {
                    Console.WriteLine($"Failed to initialize {typeof(TResource)} [{id}]: " + e);
                }
            }
        } while (ObjectGroup.Count != initialCount);

        resLib.OnResourceObjectCreated -= OnResourceObjectCreated;
    }

    private void OnResourceObjectCreated(IContext context, IResource resource, Guid id)
    {
        if (resource is TResource) {
            ObjectGroup.Add(id);
        }
    }

    public void OnLateUpdate(IContext context)
    {
        foreach (var id in RemovedObjectGroup.Query(context)) {
            if (context.Contains<Resource<TResource>>(id)) {
                continue;
            }
            DoUninitialize(context, id);
        }

        DestroyedObjectGroup.Refresh(context);
        foreach (var id in DestroyedObjectGroup) {
            DoUninitialize(context, id);
        }
    }

    private void DoUninitialize(IContext context, Guid id)
    {
        if (!context.Remove<InitializedResource<TResource>>(id, out var initializedRes)) {
            return;
        }
        try {
            ResourceLibrary.UnregisterImplicit(context, initializedRes.Value, id);
            initializedRes.Subscription?.Dispose();
            Uninitialize(context, id, initializedRes.Value);
        }
        catch (Exception e) {
            Console.WriteLine($"Failed to uninitialize {typeof(TResource)} [{id}]: " + e);
        }
    }

    protected abstract void Initialize(
        IContext context, Guid id, TResource resource, TResource? prevResource);
    protected abstract void Uninitialize(
        IContext context, Guid id, TResource resource);

    protected virtual IDisposable? Subscribe(
        IContext context, Guid id, TResource resource)
        => null;
}