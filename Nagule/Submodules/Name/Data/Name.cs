namespace Nagule;

public readonly record struct Name(string Value)
{
    public static implicit operator Name(string name)
        => new(name);

    public static implicit operator string(Name name)
        => name.Value;
}