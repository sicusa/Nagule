namespace Nagule.Graphics;

using System.Numerics;

public record ShaderParameter(string Name, ShaderParameterType Type)
{
    public ShaderParameter(TypedKey<Dyn.Unit> key) : this(key.Name, ShaderParameterType.Unit) {}

    public ShaderParameter(TypedKey<RTexture1D> key) : this(key.Name, ShaderParameterType.Texture1D) {}
    public ShaderParameter(TypedKey<RTexture2D> key) : this(key.Name, ShaderParameterType.Texture2D) {}
    public ShaderParameter(TypedKey<RTexture3D> key) : this(key.Name, ShaderParameterType.Texture3D) {}
    public ShaderParameter(TypedKey<RCubemap> key) : this(key.Name, ShaderParameterType.Cubemap) {}
    public ShaderParameter(TypedKey<RRenderTexture2D> key) : this(key.Name, ShaderParameterType.Texture2D) {}
    public ShaderParameter(TypedKey<RArrayTexture1D> key) : this(key.Name, ShaderParameterType.ArrayTexture1D) {}
    public ShaderParameter(TypedKey<RArrayTexture2D> key) : this(key.Name, ShaderParameterType.ArrayTexture2D) {}
    public ShaderParameter(TypedKey<RTileset2D> key) : this(key.Name, ShaderParameterType.Tileset2D) {}

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