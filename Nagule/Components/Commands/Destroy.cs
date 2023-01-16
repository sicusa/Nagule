namespace Nagule;

public struct Destroy : IReactiveComponent
{
}

public static class ContextDestroyExtensions
{
    public static void Destroy(this IContext context, Guid id)
        => context.Acquire<Destroy>(id);
}