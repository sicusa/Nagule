namespace Nagule;

using System.Numerics;
using System.Collections.Immutable;

public record struct Property(string Name, Dyn Value)
{
    public Property(string name, int value) : this(name, Dyn.From(value)) {}
    public Property(string name, uint value) : this(name, Dyn.From(value)) {}
    public Property(string name, long value) : this(name, Dyn.From(value)) {}
    public Property(string name, ulong value) : this(name, Dyn.From(value)) {}
    public Property(string name, bool value) : this(name, Dyn.From(value)) {}
    public Property(string name, System.Half value) : this(name, Dyn.From(value)) {}
    public Property(string name, float value) : this(name, Dyn.From(value)) {}
    public Property(string name, double value) : this(name, Dyn.From(value)) {}

    public Property(string name, Vector2 value) : this(name, Dyn.From(value)) {}
    public Property(string name, Vector3 value) : this(name, Dyn.From(value)) {}
    public Property(string name, Vector4 value) : this(name, Dyn.From(value)) {}

    public Property(string name, Matrix4x4 value) : this(name, Dyn.From(value)) {}
    public Property(string name, Matrix3x2 value) : this(name, Dyn.From(value)) {}

    public Property(string name, string value) : this(name, Dyn.From(value)) {}
    public Property(string name, ImmutableDictionary<string, Dyn> value) : this(name, Dyn.From(value)) {}

    public Property(string name, ImmutableArray<Dyn> value) : this(name, Dyn.From(value)) {}

    public static implicit operator KeyValuePair<string, Dyn>(Property p) => new(p.Name, p.Value);
    public static KeyValuePair<string, Dyn> ToPair(Property p) => new(p.Name, p.Value);
}