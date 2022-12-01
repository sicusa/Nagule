namespace Nagule;

using System.Diagnostics.CodeAnalysis;
using System.Collections.Immutable;

public struct ResourceLibrary<TResource> : ISingletonComponent
    where TResource : IResource
{
    public delegate void OnResourceObjectCreatedDelegate(IContext context, in TResource resource, Guid id);
    public static event OnResourceObjectCreatedDelegate? OnResourceObjectCreated;

    public ImmutableDictionary<TResource, List<Guid>> Dictionary =
        ImmutableDictionary<TResource, List<Guid>>.Empty;

    public ResourceLibrary() {}

    public static Guid Ensure<TObject>(IContext context, in TResource resource)
        where TObject : IResourceObject<TResource>, new()
    {
        ref var lib = ref context.AcquireAny<ResourceLibrary<TResource>>();
        if (!lib.Dictionary.TryGetValue(resource, out var objects)) {
            objects = new();
            lib.Dictionary = lib.Dictionary.Add(resource, objects);
        }
        if (objects.Count == 0) {
            var id = Guid.NewGuid();
            context.Acquire<TObject>(id).Resource = resource;
            objects.Add(id);
            OnResourceObjectCreated?.Invoke(context, in resource, id);
            return id;
        }
        return objects[0];
    }

    public static Guid Reference<TObject>(IContext context, in TResource resource, Guid referencerId)
        where TObject : IResourceObject<TResource>, new()
    {
        var id = Ensure<TObject>(context, resource);
        ref var referencers = ref context.Acquire<ResourceReferencers>(id);
        referencers.Ids = referencers.Ids.Add(referencerId);
        return id;
    }

    public static bool Unreference(IContext context, Guid resourceId, Guid referencerId, out int newRefCount)
    {
        ref var referencers = ref context.Acquire<ResourceReferencers>(resourceId);
        var newIds = referencers.Ids.Remove(referencerId);
        newRefCount = newIds.Count;
        if (newIds != referencers.Ids) {
            referencers.Ids = newIds;
            return true;
        }
        return false;
    }

    public static bool Unreference(IContext context, Guid resourceId, Guid referencerId)
    {
        ref var referencers = ref context.Acquire<ResourceReferencers>(resourceId);
        var newIds = referencers.Ids.Remove(referencerId);
        if (newIds != referencers.Ids) {
            referencers.Ids = newIds;
            return true;
        }
        return false;
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
            lib.Dictionary = lib.Dictionary.Add(resource, objects);
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