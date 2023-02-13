namespace Nagule;

using System.Diagnostics.CodeAnalysis;

public struct ResourceLibrary : ISingletonComponent
{
    public delegate void OnResourceObjectCreatedDelegate(IContext context, IResource resource, Guid id);
    public static event OnResourceObjectCreatedDelegate? OnResourceObjectCreated;

    public static Guid Id { get; } = Guid.NewGuid();

    public Dictionary<IResource, Guid> ImplicitResourceIds = new();

    private static Stack<HashSet<Guid>> _resourcesToUnreferencePool = new();

    public ResourceLibrary() {}

    public static bool TryGetImplicit(
        IContext context, IResource resource, [MaybeNullWhen(false)] out Guid id)
        => context.Acquire<ResourceLibrary>(Id)
            .ImplicitResourceIds.TryGetValue(resource, out id);

    public static Guid EnsureImplicit<TResource>(IContext context, TResource resource)
        where TResource : IResource
        => EnsureImplicit(context, ref context.Acquire<ResourceLibrary>(Id), resource);

    private static Guid EnsureImplicit<TResource>(IContext context, ref ResourceLibrary lib, TResource resource)
        where TResource : IResource
    {
        if (!lib.ImplicitResourceIds.TryGetValue(resource, out var id)) {
            id = resource.Id ?? Guid.NewGuid();
            lib.ImplicitResourceIds.Add(resource, id);
            context.Acquire<Resource<TResource>>(id).Value = resource;
            context.Acquire<ResourceImplicit>(id);
            OnResourceObjectCreated?.Invoke(context, resource, id);
        }
        return id;
    }

    public static bool UnregisterImplicit(
        IContext context, IResource resource, Guid id)
    {
        ref var lib = ref context.Acquire<ResourceLibrary>(Id);
        if (!lib.ImplicitResourceIds.TryGetValue(resource, out var resId)
                || resId != id) {
            return false;
        }
        lib.ImplicitResourceIds.Remove(resource);
        return true;
    }

    public static Guid Reference<TResource>(IContext context, Guid referencerId, TResource resource)
        where TResource : IResource
    {
        var id = EnsureImplicit(context, resource);
        Reference(context, id, referencerId);
        return id;
    }

    public static void Reference(IContext context, Guid resourceId, Guid referencerId)
    {
        ref var referencers = ref context.Acquire<ResourceReferencers>(resourceId);
        referencers.Ids.Add(referencerId);

        ref var resources = ref context.Acquire<ReferencedResources>(referencerId);
        resources.Ids.Add(resourceId);
    }

    public static bool Unreference(IContext context, Guid referencerId, IResource resource)
    {
        ref var lib = ref context.Acquire<ResourceLibrary>(Id);
        if (!lib.ImplicitResourceIds.TryGetValue(resource, out var id)) {
            return false;
        }
        return Unreference(context, id, referencerId);
    }

    public static bool Unreference(IContext context, Guid referencerId, Guid resourceId)
        => Unreference(context, referencerId, resourceId, out int _);

    public static bool Unreference(IContext context, Guid referencerId, Guid resourceId, out int newRefCount)
    {
        ref var resources = ref context.Acquire<ReferencedResources>(referencerId);
        if (!resources.Ids.Remove(resourceId)) {
            newRefCount = 0;
            return false;
        }

        var ids = context.Acquire<ResourceReferencers>(resourceId).Ids;
        if (!ids.Remove(referencerId)) {
            Console.WriteLine("Internal error: resource not found");
            newRefCount = 0;
            return false;
        }
        newRefCount = ids.Count;
        return true;
    }

    public static void UnreferenceAll(IContext context, Guid referencerId)
    {
        if (!context.TryGet<ReferencedResources>(referencerId, out var resources)) {
            return;
        }
        foreach (var id in resources.Ids) {
            var ids = context.Acquire<ResourceReferencers>(id).Ids;
            ids.Remove(referencerId);
        }
        context.Remove<ReferencedResources>(referencerId);
    }

    public static void UnreferenceAll<TResource>(IContext context, Guid referencerId)
        where TResource : IResource
    {
        if (!context.TryGet<ReferencedResources>(referencerId, out var resources)) {
            return;
        }
        foreach (var id in resources.Ids) {
            if (context.Contains<Resource<TResource>>(id)) {
                var ids = context.Acquire<ResourceReferencers>(id).Ids;
                ids.Remove(referencerId);
            }
        }
        context.Remove<ReferencedResources>(referencerId);
    }

    public static void UpdateReferences<TResource>(
        IContext context, Guid referencerId, IEnumerable<TResource> newReferences,
        Action<IContext, Guid, Guid, TResource> referenceInitializer,
        Action<IContext, Guid, Guid, TResource> referenceReinitializer,
        Action<IContext, Guid, Guid> referenceUninitializer)
        where TResource : IResource
    {
        ref var lib = ref context.Acquire<ResourceLibrary>(Id);
        ref var resources = ref context.Acquire<ReferencedResources>(referencerId, out bool exists);

        if (!exists || resources.Ids.Count == 0) {
            foreach (var res in newReferences) {
                var resId = EnsureImplicit(context, ref lib, res);
                ref var referencers = ref context.Acquire<ResourceReferencers>(resId);
                referencers.Ids.Add(referencerId);
                resources.Ids.Add(resId);
                referenceInitializer(context, referencerId, resId, res);
            }
            return;
        }

        if (!_resourcesToUnreferencePool.TryPop(out var resourcesToUnreference)) {
            resourcesToUnreference = new();
        }
        foreach (var id in resources.Ids) {
            if (context.Contains<Resource<TResource>>(id)) {
                resourcesToUnreference.Add(id);
            }
        }

        foreach (var res in newReferences) {
            var resId = EnsureImplicit(context, ref lib, res);
            if (resourcesToUnreference.Remove(resId)) {
                referenceReinitializer(context, referencerId, resId, res);
                continue;
            }
            ref var referencers = ref context.Acquire<ResourceReferencers>(resId);
            referencers.Ids.Add(referencerId);
            resources.Ids.Add(resId);
            referenceInitializer(context, referencerId, resId, res);
        }

        foreach (var resId in resourcesToUnreference) {
            ref var referencers = ref context.Acquire<ResourceReferencers>(resId);
            referencers.Ids.Remove(referencerId);
            resources.Ids.Remove(resId);
            referenceUninitializer(context, referencerId, resId);
        }

        resourcesToUnreference.Clear();
        _resourcesToUnreferencePool.Push(resourcesToUnreference);
    }

    public static void UpdateReferences<TResource, TArg>(
        IContext context, Guid referencerId, IEnumerable<KeyValuePair<TResource, TArg>> newReferences,
        Action<IContext, Guid, Guid, TResource, TArg> referenceInitializer,
        Action<IContext, Guid, Guid, TResource, TArg> referenceReinitializer,
        Action<IContext, Guid, Guid> referenceUninitializer)
        where TResource : IResource
    {
        ref var lib = ref context.Acquire<ResourceLibrary>(Id);
        ref var resources = ref context.Acquire<ReferencedResources>(referencerId, out bool exists);

        if (!exists || resources.Ids.Count == 0) {
            foreach (var (res, arg) in newReferences) {
                var resId = EnsureImplicit(context, ref lib, res);
                ref var referencers = ref context.Acquire<ResourceReferencers>(resId);
                referencers.Ids.Add(referencerId);
                resources.Ids.Add(resId);
                referenceInitializer(context, referencerId, resId, res, arg);
            }
            return;
        }

        if (!_resourcesToUnreferencePool.TryPop(out var resourcesToUnreference)) {
            resourcesToUnreference = new();
        }
        foreach (var id in resources.Ids) {
            if (context.Contains<Resource<TResource>>(id)) {
                resourcesToUnreference.Add(id);
            }
        }

        foreach (var (res, arg) in newReferences) {
            var resId = EnsureImplicit(context, ref lib, res);
            if (resourcesToUnreference.Remove(resId)) {
                referenceReinitializer(context, referencerId, resId, res, arg);
                continue;
            }
            ref var referencers = ref context.Acquire<ResourceReferencers>(resId);
            referencers.Ids.Add(referencerId);
            resources.Ids.Add(resId);
            referenceInitializer(context, referencerId, resId, res, arg);
        }

        foreach (var resId in resourcesToUnreference) {
            ref var referencers = ref context.Acquire<ResourceReferencers>(resId);
            referencers.Ids.Remove(referencerId);
            resources.Ids.Remove(resId);
            referenceUninitializer(context, referencerId, resId);
        }

        resourcesToUnreference.Clear();
        _resourcesToUnreferencePool.Push(resourcesToUnreference);
    }
}