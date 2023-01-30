namespace Nagule;

public record struct TypedKey<TValue>(string Name)
{
    public static implicit operator TypedKey<TValue>(string name) => new(name);
    public static implicit operator string(TypedKey<TValue> key) => key.Name;
}
