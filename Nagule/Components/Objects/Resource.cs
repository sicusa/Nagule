namespace Nagule;

public struct Resource<TResource> : IReactiveComponent
    where TResource : IResource
{
    public TResource Value;
}

public static class ContextResourceExtensions
{
    public static void SetResource<TResource>(this IContext context, Guid id, TResource resource)
        where TResource : IResource
        => context.Acquire<Resource<TResource>>(id).Value = resource;
}