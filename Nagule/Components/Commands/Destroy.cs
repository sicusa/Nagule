namespace Nagule;

using Aeco;

public struct Destroy : IReactiveComponent
{
}

public static class ContextDestroyExtensions
{
    public static void Destroy(this IContext context, Guid id)
        => context.Acquire<Destroy>(id);

    public static void Destroy(this IEntity<IComponent> entity)
        => entity.Acquire<Destroy>();
}