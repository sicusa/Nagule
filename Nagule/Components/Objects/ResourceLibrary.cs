namespace Nagule;

using System.Diagnostics.CodeAnalysis;

using Aeco;

public class ResourceLibrary : ISingletonComponent
{
    public delegate void OnResourceObjectCreatedDelegate(IContext context, IResource resource, uint id);

    public OnResourceObjectCreatedDelegate? OnResourceObjectCreated;
    [AllowNull] public Dictionary<IResource, uint> ImplicitResourceIds;

    [AllowNull] private Stack<HashSet<uint>> _resourcesToUnreferencePool;
    [AllowNull] private IContext _context;

    public ResourceLibrary() {}

    internal void Initialize(IContext context)
    {
        ImplicitResourceIds = new();
        _resourcesToUnreferencePool = new();
        _context = context;
    }

    public bool TryGetImplicit(IResource resource, [MaybeNullWhen(false)] out uint id)
        => ImplicitResourceIds.TryGetValue(resource, out id);

    private uint EnsureImplicit<TResource>(TResource resource)
        where TResource : IResource
    {
        if (!ImplicitResourceIds.TryGetValue(resource, out var id)) {
            id = resource.Id ?? IdFactory.New();
            ImplicitResourceIds.Add(resource, id);
            _context.Acquire<Resource<TResource>>(id).Value = resource;
            _context.Acquire<ResourceImplicit>(id);
            OnResourceObjectCreated?.Invoke(_context, resource, id);
        }
        return id;
    }

    public bool UnregisterImplicit(IResource resource, uint id)
    {
        if (!ImplicitResourceIds.TryGetValue(resource, out var resId)
                || resId != id) {
            return false;
        }
        ImplicitResourceIds.Remove(resource);
        return true;
    }

    public uint Reference<TResource>(uint referencerId, TResource resource)
        where TResource : IResource
    {
        var id = EnsureImplicit(resource);
        Reference(id, referencerId);
        return id;
    }

    public void Reference(uint resourceId, uint referencerId)
    {
        ref var referencers = ref _context.Acquire<ResourceReferencers>(resourceId);
        referencers.Ids.Add(referencerId);

        ref var resources = ref _context.Acquire<ReferencedResources>(referencerId);
        resources.Ids.Add(resourceId);
    }

    public bool Unreference(uint referencerId, IResource resource)
    {
        if (!ImplicitResourceIds.TryGetValue(resource, out var resourceId)) {
            return false;
        }
        return Unreference(referencerId, resourceId);
    }

    public bool Unreference(uint referencerId, IResource resource, out uint resourceId)
    {
        if (!ImplicitResourceIds.TryGetValue(resource, out resourceId)) {
            return false;
        }
        return Unreference(referencerId, resourceId);
    }

    public bool Unreference(uint referencerId, uint resourceId)
        => Unreference(referencerId, resourceId, out int _);

    public bool Unreference(uint referencerId, uint resourceId, out int newRefCount)
    {
        ref var resources = ref _context.Acquire<ReferencedResources>(referencerId);
        if (!resources.Ids.Remove(resourceId)) {
            newRefCount = 0;
            return false;
        }

        var ids = _context.Acquire<ResourceReferencers>(resourceId).Ids;
        if (!ids.Remove(referencerId)) {
            Console.WriteLine("Internal error: resource not found");
            newRefCount = 0;
            return false;
        }
        newRefCount = ids.Count;
        return true;
    }

    public void UnreferenceAll(uint referencerId)
    {
        if (!_context.TryGet<ReferencedResources>(referencerId, out var resources)) {
            return;
        }
        foreach (var id in resources.Ids) {
            var ids = _context.Acquire<ResourceReferencers>(id).Ids;
            ids.Remove(referencerId);
        }
        _context.Remove<ReferencedResources>(referencerId);
    }

    public void UnreferenceAll<TResource>(uint referencerId)
        where TResource : IResource
    {
        if (!_context.TryGet<ReferencedResources>(referencerId, out var resources)) {
            return;
        }
        foreach (var id in resources.Ids) {
            if (_context.Contains<Resource<TResource>>(id)) {
                var ids = _context.Acquire<ResourceReferencers>(id).Ids;
                ids.Remove(referencerId);
            }
        }
        _context.Remove<ReferencedResources>(referencerId);
    }

    public void UpdateReferences<TResource>(
        uint referencerId, IEnumerable<TResource> newReferences,
        Action<IContext, uint, uint, TResource> referenceInitializer,
        Action<IContext, uint, uint, TResource> referenceReinitializer,
        Action<IContext, uint, uint> referenceUninitializer)
        where TResource : IResource
    {
        ref var resources = ref _context.Acquire<ReferencedResources>(referencerId, out bool exists);

        if (!exists || resources.Ids.Count == 0) {
            foreach (var res in newReferences) {
                var resId = EnsureImplicit(res);
                ref var referencers = ref _context.Acquire<ResourceReferencers>(resId);
                referencers.Ids.Add(referencerId);
                resources.Ids.Add(resId);
                referenceInitializer(_context, referencerId, resId, res);
            }
            return;
        }

        if (!_resourcesToUnreferencePool.TryPop(out var resourcesToUnreference)) {
            resourcesToUnreference = new();
        }
        foreach (var id in resources.Ids) {
            if (_context.Contains<Resource<TResource>>(id)) {
                resourcesToUnreference.Add(id);
            }
        }

        foreach (var res in newReferences) {
            var resId = EnsureImplicit(res);
            if (resourcesToUnreference.Remove(resId)) {
                referenceReinitializer(_context, referencerId, resId, res);
                continue;
            }
            ref var referencers = ref _context.Acquire<ResourceReferencers>(resId);
            referencers.Ids.Add(referencerId);
            resources.Ids.Add(resId);
            referenceInitializer(_context, referencerId, resId, res);
        }

        foreach (var resId in resourcesToUnreference) {
            ref var referencers = ref _context.Acquire<ResourceReferencers>(resId);
            referencers.Ids.Remove(referencerId);
            resources.Ids.Remove(resId);
            referenceUninitializer(_context, referencerId, resId);
        }

        resourcesToUnreference.Clear();
        _resourcesToUnreferencePool.Push(resourcesToUnreference);
    }

    public void UpdateReferences<TResource, TArg>(
        uint referencerId, IEnumerable<KeyValuePair<TResource, TArg>> newReferences,
        Action<IContext, uint, uint, TResource, TArg> referenceInitializer,
        Action<IContext, uint, uint, TResource, TArg> referenceReinitializer,
        Action<IContext, uint, uint> referenceUninitializer)
        where TResource : IResource
    {
        ref var resources = ref _context.Acquire<ReferencedResources>(referencerId, out bool exists);

        if (!exists || resources.Ids.Count == 0) {
            foreach (var (res, arg) in newReferences) {
                var resId = EnsureImplicit(res);
                ref var referencers = ref _context.Acquire<ResourceReferencers>(resId);
                referencers.Ids.Add(referencerId);
                resources.Ids.Add(resId);
                referenceInitializer(_context, referencerId, resId, res, arg);
            }
            return;
        }

        if (!_resourcesToUnreferencePool.TryPop(out var resourcesToUnreference)) {
            resourcesToUnreference = new();
        }
        foreach (var id in resources.Ids) {
            if (_context.Contains<Resource<TResource>>(id)) {
                resourcesToUnreference.Add(id);
            }
        }

        foreach (var (res, arg) in newReferences) {
            var resId = EnsureImplicit(res);
            if (resourcesToUnreference.Remove(resId)) {
                referenceReinitializer(_context, referencerId, resId, res, arg);
                continue;
            }
            ref var referencers = ref _context.Acquire<ResourceReferencers>(resId);
            referencers.Ids.Add(referencerId);
            resources.Ids.Add(resId);
            referenceInitializer(_context, referencerId, resId, res, arg);
        }

        foreach (var resId in resourcesToUnreference) {
            ref var referencers = ref _context.Acquire<ResourceReferencers>(resId);
            referencers.Ids.Remove(referencerId);
            resources.Ids.Remove(resId);
            referenceUninitializer(_context, referencerId, resId);
        }

        resourcesToUnreference.Clear();
        _resourcesToUnreferencePool.Push(resourcesToUnreference);
    }
}

public static class ResourceLibraryExtensions
{
    public static ResourceLibrary GetResourceLibrary(this IContext context)
    {
        var lib = context.Acquire<ResourceLibrary>(out bool exists);
        if (!exists) {
            lib.Initialize(context);
        }
        return lib;
    }
}