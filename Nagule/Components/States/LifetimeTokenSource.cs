namespace Nagule;

public struct LifetimeTokenSource : IPooledComponent
{
    public readonly CancellationTokenSource Value = new();

    public LifetimeTokenSource() {}
}

public static class LifetimeTokenSourceExtensions
{
    public static CancellationToken GetLifetimeToken(this IContext context, Guid id)
        => context.Acquire<LifetimeTokenSource>(id).Value.Token;
}