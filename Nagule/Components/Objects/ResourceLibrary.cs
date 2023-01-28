namespace Nagule;

using System.Diagnostics.CodeAnalysis;

public struct ResourceLibrary<TResource> : ISingletonComponent
    where TResource : IResource
{
    public delegate void OnResourceObjectCreatedDelegate(IContext context, TResource resource, Guid id);
    public static event OnResourceObjectCreatedDelegate? OnResourceObjectCreated;

    public Dictionary<TResource, Guid> ImplicitResourceIds = new();

    private static Stack<HashSet<Guid>> _resourcesToUnreferencePool = new();

    public ResourceLibrary() {}

    public static bool TryGetImplicit(
        IContext context, in TResource resource, [MaybeNullWhen(false)] out Guid id)
        => context.AcquireAny<ResourceLibrary<TResource>>()
            .ImplicitResourceIds.TryGetValue(resource, out id);

    public static Guid EnsureImplicit(IContext context, TResource resource)
        => EnsureImplicit(context, ref context.AcquireAny<ResourceLibrary<TResource>>(), resource);

    private static Guid EnsureImplicit(IContext context, ref ResourceLibrary<TResource> lib, TResource resource)
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
        IContext context, TResource resource, Guid id)
    {
        ref var lib = ref context.AcquireAny<ResourceLibrary<TResource>>();
        if (!lib.ImplicitResourceIds.TryGetValue(resource, out var resId)
                || resId != id) {
            return false;
        }
        lib.ImplicitResourceIds.Remove(resource);
        return true;
    }

    public static Guid Reference(IContext context, Guid referencerId, TResource resource)
    {
        var id = EnsureImplicit(context, resource);
        Reference(context, id, referencerId);
        return id;
    }

    public static void Reference(IContext context, Guid resourceId, Guid referencerId)
    {
        ref var referencers = ref context.Acquire<ResourceReferencers>(resourceId);
        referencers.Ids.Add(referencerId);

        ref var resources = ref context.Acquire<ReferencedResources<TResource>>(referencerId);
        resources.Ids.Add(resourceId);
    }

    public static bool Unreference(IContext context, Guid referencerId, Guid resourceId)
        => Unreference(context, referencerId, resourceId, out int _);

    public static bool Unreference(IContext context, Guid referencerId, Guid resourceId, out int newRefCount)
    {
        ref var resources = ref context.Acquire<ReferencedResources<TResource>>(referencerId);
        if (!resources.Ids.Remove(resourceId)) {
            newRefCount = 0;
            return false;
        }

        ref var referencers = ref context.Acquire<ResourceReferencers>(resourceId);
        if (!referencers.Ids.Remove(referencerId)) {
            newRefCount = 0;
            return false;
        }

        newRefCount = referencers.Ids.Count;
        return true;
    }

    public static bool Unreference(IContext context, Guid referencerId, TResource resource)
    {
        ref var lib = ref context.AcquireAny<ResourceLibrary<TResource>>();
        if (!lib.ImplicitResourceIds.TryGetValue(resource, out var id)) {
            return false;
        }
        return Unreference(context, id, referencerId);
    }

    public static void UnreferenceAll(IContext context, Guid referencerId)
    {
        if (!context.TryGet<ReferencedResources<TResource>>(referencerId, out var resources)) {
            return;
        }
        foreach (var id in resources.Ids) {
            ref var referencers = ref context.Acquire<ResourceReferencers>(id);
            referencers.Ids.Remove(referencerId);
        }
        context.Remove<ReferencedResources<TResource>>(referencerId);
    }

    public static void UpdateReferences(
        IContext context, Guid referencerId, IEnumerable<TResource> newReferences,
        Action<IContext, Guid, Guid, TResource> referenceInitializer,
        Action<IContext, Guid, Guid, TResource> referenceReinitializer,
        Action<IContext, Guid, Guid> referenceUninitializer)
    {
        ref var lib = ref context.AcquireAny<ResourceLibrary<TResource>>();
        ref var resources = ref context.Acquire<ReferencedResources<TResource>>(referencerId, out bool exists);

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
            resourcesToUnreference.Add(id);
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

    public static void UpdateReferences<TArg>(
        IContext context, Guid referencerId, IEnumerable<KeyValuePair<TResource, TArg>> newReferences,
        Action<IContext, Guid, Guid, TResource, TArg> referenceInitializer,
        Action<IContext, Guid, Guid, TResource, TArg> referenceReinitializer,
        Action<IContext, Guid, Guid> referenceUninitializer)
    {
        if (!_resourcesToUnreferencePool.TryPop(out var resourcesToUnreference)) {
            resourcesToUnreference = new();
        }

        ref var resources = ref context.Acquire<ReferencedResources<TResource>>(referencerId);
        foreach (var id in resources.Ids) {
            resourcesToUnreference.Add(id);
        }

        ref var lib = ref context.AcquireAny<ResourceLibrary<TResource>>();
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