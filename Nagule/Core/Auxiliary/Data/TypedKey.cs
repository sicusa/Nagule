namespace Nagule;

public record struct TypedKey<TKey, TValue>(TKey Key)
{
    public static implicit operator TypedKey<TKey, TValue>(TKey key) => new(key);
}

public record struct TypedKey<TValue>(string Name)
{
    public static implicit operator TypedKey<TValue>(string name) => new(name);
}