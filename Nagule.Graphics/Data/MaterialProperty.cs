namespace Nagule.Graphics;

using System.Numerics;

public record TextureDyn(TextureAssetBase? Value) : Dyn;

public record struct MaterialProperty(string Name, Dyn Value)
{
    public MaterialProperty(string name) : this(name, Dyn.UnitValue) {}
    public MaterialProperty(TypedKey<Dyn.Unit> key) : this(key.Name, Dyn.UnitValue) {}

    public MaterialProperty(string name, Texture2DAsset? value) : this(name, new TextureDyn(value)) {}
    public MaterialProperty(string name, CubemapAsset? value) : this(name, new TextureDyn(value)) {}
    public MaterialProperty(string name, RenderTexture2DAsset? value) : this(name, new TextureDyn(value)) {}

    public MaterialProperty(TypedKey<Texture2DAsset> key, Texture2DAsset? texture) : this(key.Name, new TextureDyn(texture)) {}
    public MaterialProperty(TypedKey<CubemapAsset> key, CubemapAsset? texture) : this(key.Name, new TextureDyn(texture)) {}
    public MaterialProperty(TypedKey<RenderTexture2DAsset> key, RenderTexture2DAsset? texture) : this(key.Name, new TextureDyn(texture)) {}

    public MaterialProperty(string name, int value) : this(name, Dyn.From(value)) {}
    public MaterialProperty(string name, uint value) : this(name, Dyn.From(value)) {}
    public MaterialProperty(string name, bool value) : this(name, Dyn.From(value)) {}
    public MaterialProperty(string name, float value) : this(name, Dyn.From(value)) {}
    public MaterialProperty(string name, double value) : this(name, Dyn.From(value)) {}

    public MaterialProperty(TypedKey<int> key, int value) : this(key.Name, Dyn.From(value)) {}
    public MaterialProperty(TypedKey<uint> key, uint value) : this(key.Name, Dyn.From(value)) {}
    public MaterialProperty(TypedKey<bool> key, bool value) : this(key.Name, Dyn.From(value)) {}
    public MaterialProperty(TypedKey<float> key, float value) : this(key.Name, Dyn.From(value)) {}
    public MaterialProperty(TypedKey<double> key, double value) : this(key.Name, Dyn.From(value)) {}

    public MaterialProperty(string name, Vector2 value) : this(name, Dyn.From(value)) {}
    public MaterialProperty(string name, Vector3 value) : this(name, Dyn.From(value)) {}
    public MaterialProperty(string name, Vector4 value) : this(name, Dyn.From(value)) {}
    
    public MaterialProperty(TypedKey<Vector2> key, Vector2 value) : this(key.Name, Dyn.From(value)) {}
    public MaterialProperty(TypedKey<Vector3> key, Vector3 value) : this(key.Name, Dyn.From(value)) {}
    public MaterialProperty(TypedKey<Vector4> key, Vector4 value) : this(key.Name, Dyn.From(value)) {}

    public MaterialProperty(string name, Matrix4x4 value) : this(name, Dyn.From(value)) {}
    public MaterialProperty(string name, Matrix3x2 value) : this(name, Dyn.From(value)) {}

    public MaterialProperty(TypedKey<Matrix4x4> key, Matrix4x4 value) : this(key.Name, Dyn.From(value)) {}
    public MaterialProperty(TypedKey<Matrix3x2> key, Matrix3x2 value) : this(key.Name, Dyn.From(value)) {}

    public static KeyValuePair<string, Dyn> ToPair(MaterialProperty property)
        => KeyValuePair.Create(property.Name, property.Value);
}