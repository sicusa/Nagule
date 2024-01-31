namespace Nagule.Graphics;

using System.Numerics;

public record ShaderParameter(string Name, ShaderParameterType Type)
{
    public static implicit operator ShaderParameter(TypedKey<Dyn.Unit> key) => new(key.Name, ShaderParameterType.Unit);

    public static implicit operator ShaderParameter(TypedKey<RTexture1D> key) => new(key.Name, ShaderParameterType.Texture1D);
    public static implicit operator ShaderParameter(TypedKey<RTexture2D> key) => new(key.Name, ShaderParameterType.Texture2D);
    public static implicit operator ShaderParameter(TypedKey<RTexture3D> key) => new(key.Name, ShaderParameterType.Texture3D);
    public static implicit operator ShaderParameter(TypedKey<RCubemap> key) => new(key.Name, ShaderParameterType.Cubemap);
    public static implicit operator ShaderParameter(TypedKey<RRenderTexture2D> key) => new(key.Name, ShaderParameterType.Texture2D);
    public static implicit operator ShaderParameter(TypedKey<RArrayTexture1D> key) => new(key.Name, ShaderParameterType.ArrayTexture1D);
    public static implicit operator ShaderParameter(TypedKey<RArrayTexture2D> key) => new(key.Name, ShaderParameterType.ArrayTexture2D);
    public static implicit operator ShaderParameter(TypedKey<RTileset2D> key) => new(key.Name, ShaderParameterType.Tileset2D);

    public static implicit operator ShaderParameter(TypedKey<int> key) => new(key.Name, ShaderParameterType.Int);
    public static implicit operator ShaderParameter(TypedKey<uint> key) => new(key.Name, ShaderParameterType.UInt);
    public static implicit operator ShaderParameter(TypedKey<bool> key) => new(key.Name, ShaderParameterType.Bool);
    public static implicit operator ShaderParameter(TypedKey<float> key) => new(key.Name, ShaderParameterType.Float);
    public static implicit operator ShaderParameter(TypedKey<double> key) => new(key.Name, ShaderParameterType.Double);

    public static implicit operator ShaderParameter(TypedKey<Vector2> key) => new(key.Name, ShaderParameterType.Vector2);
    public static implicit operator ShaderParameter(TypedKey<Vector3> key) => new(key.Name, ShaderParameterType.Vector3);
    public static implicit operator ShaderParameter(TypedKey<Vector4> key) => new(key.Name, ShaderParameterType.Vector4);

    public static implicit operator ShaderParameter(TypedKey<Matrix4x4> key) => new(key.Name, ShaderParameterType.Matrix4x3);
    public static implicit operator ShaderParameter(TypedKey<Matrix3x2> key) => new(key.Name, ShaderParameterType.Matrix3x2);

    public static KeyValuePair<string, ShaderParameterType> ToPair(ShaderParameter p)
        => KeyValuePair.Create(p.Name, p.Type);
}