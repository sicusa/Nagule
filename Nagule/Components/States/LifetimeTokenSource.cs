namespace Nagule;

public struct LifetimeTokenSource : IHashComponent
{
    public readonly CancellationTokenSource Value = new();

    public LifetimeTokenSource() {}
}

public static class LifetimeTokenSourceExtensions
{
    public static CancellationToken GetLifetimeToken(this IContext context, uint id)
        => context.Acquire<LifetimeTokenSource>(id).Value.Token;
}