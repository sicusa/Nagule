namespace Nagule;

using System.Diagnostics.CodeAnalysis;

public struct ResourceLibrary<TResource> : ISingletonComponent
    where TResource : IResource
{
    public delegate void OnResourceObjectCreatedDelegate(IContext context, in TResource resource, Guid id);
    public static event OnResourceObjectCreatedDelegate? OnResourceObjectCreated;

    public Dictionary<TResource, List<Guid>> Dictionary = new();

    public ResourceLibrary() {}

    public static Guid Ensure(IContext context, in TResource resource)
    {
        ref var lib = ref context.AcquireAny<ResourceLibrary<TResource>>();
        if (!lib.Dictionary.TryGetValue(resource, out var objects)) {
            objects = new();
            lib.Dictionary.Add(resource, objects);
        }
        if (objects.Count == 0) {
            var id = resource.Id ?? Guid.NewGuid();
            context.Acquire<Resource<TResource>>(id).Value = resource;
            objects.Add(id);
            OnResourceObjectCreated?.Invoke(context, in resource, id);
            return id;
        }
        return objects[0];
    }

    public static Guid Reference(IContext context, in TResource resource, Guid referencerId)
    {
        var id = Ensure(context, resource);
        ref var referencers = ref context.Acquire<ResourceReferencers>(id);
        referencers.Ids.Add(referencerId);
        return id;
    }

    public static void Reference(IContext context, Guid resourceId, Guid referencerId)
    {
        ref var referencers = ref context.Acquire<ResourceReferencers>(resourceId);
        referencers.Ids.Add(referencerId);
    }

    public static bool Unreference(IContext context, Guid resourceId, Guid referencerId, out int newRefCount)
    {
        ref var referencers = ref context.Acquire<ResourceReferencers>(resourceId);
        var result = referencers.Ids.Remove(referencerId);
        newRefCount = referencers.Ids.Count;
        return result;
    }

    public static bool Unreference(IContext context, Guid resourceId, Guid referencerId)
    {
        ref var referencers = ref context.Acquire<ResourceReferencers>(resourceId);
        return referencers.Ids.Remove(referencerId);
    }

    public static bool Unreference(IContext context, in TResource resource, Guid referencerId)
    {
        ref var lib = ref context.AcquireAny<ResourceLibrary<TResource>>();
        if (!lib.Dictionary.TryGetValue(resource, out var objects)) {
            return false;
        }
        foreach (var id in objects) {
            if (Unreference(context, id, referencerId)) {
                return true;
            }
        }
        return false;
    }

    public static bool TryGet(
        IContext context, in TResource resource, [MaybeNullWhen(false)] out Guid id)
    {
        if (!context.AcquireAny<ResourceLibrary<TResource>>()
                .Dictionary.TryGetValue(resource, out var objects)
                || objects.Count == 0) {
            id = default;
            return false;
        }
        id = objects[0];
        return true;
    }

    public static IEnumerable<Guid> GetAll(IContext context, in TResource resource)
    {
        if (!context.AcquireAny<ResourceLibrary<TResource>>()
                .Dictionary.TryGetValue(resource, out var objects)) {
            return Enumerable.Empty<Guid>();
        }
        return objects;
    }
    
    public static void Register(
        IContext context, in TResource resource, Guid id)
    {
        ref var lib = ref context.AcquireAny<ResourceLibrary<TResource>>();
        if (!lib.Dictionary.TryGetValue(resource, out var objects)) {
            objects = new();
            lib.Dictionary.Add(resource, objects);
        }
        objects.Add(id);
    }

    public static bool Unregister(
        IContext context, in TResource resource, Guid id)
    {
        ref var lib = ref context.AcquireAny<ResourceLibrary<TResource>>();
        if (!lib.Dictionary.TryGetValue(resource, out var objects)
                || !objects.Remove(id)) {
            return false;
        }
        return true;
    }
}