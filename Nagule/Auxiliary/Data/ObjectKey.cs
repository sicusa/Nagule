namespace Nagule;

using System.Runtime.CompilerServices;

public record struct ObjectKey<T>(T Value) : IEquatable<ObjectKey<T>>
    where T : class
{
    public override readonly int GetHashCode()
        => RuntimeHelpers.GetHashCode(Value);

    public readonly bool Equals(ObjectKey<T>? other)
        => ReferenceEquals(Value, other?.Value);
}