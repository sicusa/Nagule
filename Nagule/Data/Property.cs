namespace Nagule;

using System.Numerics;
using System.Collections.Immutable;

public record struct Property(string Name, Dyn Value)
{
    public Property(string name) : this(name, Dyn.UnitValue) {}
    public Property(string name, int value) : this(name, Dyn.From(value)) {}
    public Property(string name, uint value) : this(name, Dyn.From(value)) {}
    public Property(string name, long value) : this(name, Dyn.From(value)) {}
    public Property(string name, ulong value) : this(name, Dyn.From(value)) {}
    public Property(string name, bool value) : this(name, Dyn.From(value)) {}
    public Property(string name, System.Half value) : this(name, Dyn.From(value)) {}
    public Property(string name, float value) : this(name, Dyn.From(value)) {}
    public Property(string name, double value) : this(name, Dyn.From(value)) {}

    public Property(TypedKey<Dyn.Unit> key) : this(key.Name, Dyn.UnitValue) {}
    public Property(TypedKey<int> key, int value) : this(key.Name, Dyn.From(value)) {}
    public Property(TypedKey<uint> key, uint value) : this(key.Name, Dyn.From(value)) {}
    public Property(TypedKey<long> key, long value) : this(key.Name, Dyn.From(value)) {}
    public Property(TypedKey<ulong> key, ulong value) : this(key.Name, Dyn.From(value)) {}
    public Property(TypedKey<bool> key, bool value) : this(key.Name, Dyn.From(value)) {}
    public Property(TypedKey<System.Half> key, System.Half value) : this(key.Name, Dyn.From(value)) {}
    public Property(TypedKey<float> key, float value) : this(key.Name, Dyn.From(value)) {}
    public Property(TypedKey<double> key, double value) : this(key.Name, Dyn.From(value)) {}

    public Property(string name, Vector2 value) : this(name, Dyn.From(value)) {}
    public Property(string name, Vector3 value) : this(name, Dyn.From(value)) {}
    public Property(string name, Vector4 value) : this(name, Dyn.From(value)) {}
    
    public Property(TypedKey<Vector2> key, Vector2 value) : this(key.Name, Dyn.From(value)) {}
    public Property(TypedKey<Vector3> key, Vector3 value) : this(key.Name, Dyn.From(value)) {}
    public Property(TypedKey<Vector4> key, Vector4 value) : this(key.Name, Dyn.From(value)) {}

    public Property(string name, Matrix4x4 value) : this(name, Dyn.From(value)) {}
    public Property(string name, Matrix3x2 value) : this(name, Dyn.From(value)) {}

    public Property(TypedKey<Matrix4x4> key, Matrix4x4 value) : this(key.Name, Dyn.From(value)) {}
    public Property(TypedKey<Matrix3x2> key, Matrix3x2 value) : this(key.Name, Dyn.From(value)) {}

    public Property(string name, string value) : this(name, Dyn.From(value)) {}
    public Property(string name, ImmutableDictionary<string, Dyn> value) : this(name, Dyn.From(value)) {}
    public Property(string name, ImmutableArray<Dyn> value) : this(name, Dyn.From(value)) {}

    public Property(TypedKey<string> key, string value) : this(key.Name, Dyn.From(value)) {}
    public Property(TypedKey<ImmutableDictionary<string, Dyn>> key, ImmutableDictionary<string, Dyn> value) : this(key.Name, Dyn.From(value)) {}
    public Property(TypedKey<ImmutableArray<Dyn>> key, ImmutableArray<Dyn> value) : this(key.Name, Dyn.From(value)) {}

    public static implicit operator KeyValuePair<string, Dyn>(Property p) => new(p.Name, p.Value);
    public static KeyValuePair<string, Dyn> ToPair(Property p) => new(p.Name, p.Value);
}