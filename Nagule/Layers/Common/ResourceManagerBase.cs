namespace Nagule;

using Aeco;
using Aeco.Reactive;

public abstract class ResourceManagerBase<TResource>
    : Layer, IResourceUpdateListener, ILateUpdateListener
    where TResource : IResource
{
    protected Group<Modified<Resource<TResource>>, Resource<TResource>> ObjectGroup { get; } = new();
    protected Group<Resource<TResource>, Destroy> DestroyedObjectGroup { get; } = new();

    public virtual void OnResourceUpdate(IContext context)
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
                    var resource = context.InspectRaw<Resource<TResource>>(id).Value;
                    if (resource == null) {
                        throw new ArgumentNullException("Resource not set");
                    }
                    if (resource.Name != "") {
                        context.Acquire<Name>(id).Value = resource.Name;
                    }
                    if (context.TryGet<InitializedResource<TResource>>(id, out var initializedRes)) {
                        if (Object.ReferenceEquals(initializedRes.Value, resource)) {
                            continue;
                        }
                        Initialize(context, id, resource, initializedRes.Value);
                    }
                    else {
                        Initialize(context, id, resource, default);
                        context.Acquire<InitializedResource<TResource>>(id).Value = resource;
                    }
                }
                catch (Exception e) {
                    Console.WriteLine($"Failed to initialize {typeof(TResource)} [{id}]: " + e);
                }
            }
        } while (ObjectGroup.Count != initialCount);

        ResourceLibrary<TResource>.OnResourceObjectCreated -= OnResourceObjectCreated;
    }

    private void OnResourceObjectCreated(IContext context, TResource resource, Guid id)
    {
        ObjectGroup.Add(id);
    }

    public void OnLateUpdate(IContext context)
    {
        DestroyedObjectGroup.Refresh(context);
        foreach (var id in DestroyedObjectGroup) {
            try {
                if (!context.Remove<Resource<TResource>>(id)) {
                    throw new KeyNotFoundException($"{typeof(TResource)} [{id}] does not have object component.");
                }
                if (!context.Remove<InitializedResource<TResource>>(id, out var initializedRes)) {
                    continue;
                }
                ResourceLibrary<TResource>.UnregisterImplicit(context, initializedRes.Value, id);
                Uninitialize(context, id, initializedRes.Value);
            }
            catch (Exception e) {
                Console.WriteLine($"Failed to uninitialize {typeof(TResource)} [{id}]: " + e);
            }
        }
    }

    protected abstract void Initialize(
        IContext context, Guid id, TResource resource, TResource? prevResource);
    protected abstract void Uninitialize(
        IContext context, Guid id, TResource resource);
}