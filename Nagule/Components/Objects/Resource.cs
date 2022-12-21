namespace Nagule;

using Aeco;

public struct Resource<TResource> : IReactiveComponent
    where TResource : IResource
{
    public TResource? Value { get; set; }
}

public static class ContextResourceExtensions
{
    public static void SetResource<TResource>(this IContext context, Guid id, TResource resource)
        where TResource : IResource
        => context.Acquire<Resource<TResource>>(id).Value = resource;

    public static void SetResource<TResource>(this IEntity<IComponent> entity, TResource resource)
        where TResource : IResource
        => entity.Acquire<Resource<TResource>>().Value = resource;
}