namespace Nagule.Graphics;

using System.Numerics;

public record ShaderParameter(string Name, ShaderParameterType Type)
{
    public ShaderParameter(TypedKey<Dyn.Unit> key) : this(key.Name, ShaderParameterType.Unit) {}
    
    public ShaderParameter(TypedKey<Texture> key) : this(key.Name, ShaderParameterType.Texture) {}
    public ShaderParameter(TypedKey<Cubemap> key) : this(key.Name, ShaderParameterType.Texture) {}
    public ShaderParameter(TypedKey<RenderTexture> key) : this(key.Name, ShaderParameterType.Texture) {}

    public ShaderParameter(TypedKey<int> key) : this(key.Name, ShaderParameterType.Int) {}
    public ShaderParameter(TypedKey<uint> key) : this(key.Name, ShaderParameterType.UInt) {}
    public ShaderParameter(TypedKey<bool> key) : this(key.Name, ShaderParameterType.Bool) {}
    public ShaderParameter(TypedKey<float> key) : this(key.Name, ShaderParameterType.Float) {}
    public ShaderParameter(TypedKey<double> key) : this(key.Name, ShaderParameterType.Double) {}

    public ShaderParameter(TypedKey<Vector2> key) : this(key.Name, ShaderParameterType.Vector2) {}
    public ShaderParameter(TypedKey<Vector3> key) : this(key.Name, ShaderParameterType.Vector3) {}
    public ShaderParameter(TypedKey<Vector4> key) : this(key.Name, ShaderParameterType.Vector4) {}

    public ShaderParameter(TypedKey<Matrix4x4> key) : this(key.Name, ShaderParameterType.Matrix4x3) {}
    public ShaderParameter(TypedKey<Matrix3x2> key) : this(key.Name, ShaderParameterType.Matrix3x2) {}

    public static KeyValuePair<string, ShaderParameterType> ToPair(ShaderParameter p)
        => KeyValuePair.Create(p.Name, p.Type);
}