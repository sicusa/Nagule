namespace Nagule.Graphics;

using System.Text;
using System.Numerics;
using System.Collections.Concurrent;
using System.Collections.Frozen;

public static class ShaderUtils
{
    public static readonly string EmptyFragmentShader = "#version 410 core\nvoid main() { }";

    public static IReadOnlyDictionary<ShaderParameterType, int> ParameterSizes => s_parameterSizes;

    public static readonly FrozenDictionary<Type, char?> ChannelTypeTextureSamplerMap =
        new Dictionary<Type, char?> {
            [typeof(byte)] = null,
            [typeof(Half)] = null,
            [typeof(float)] = null,
            [typeof(short)] = 'i',
            [typeof(ushort)] = 'u',
            [typeof(int)] = 'i',
            [typeof(uint)] = 'u',
        }.ToFrozenDictionary();

    public static readonly FrozenDictionary<ShaderParameterType, string> TextureSamplerMap =
        new Dictionary<ShaderParameterType, string>() {
            [ShaderParameterType.Texture1D] = "sampler1D",
            [ShaderParameterType.Texture2D] = "sampler2D",
            [ShaderParameterType.Texture3D] = "sampler3D",
            [ShaderParameterType.Cubemap] = "samplerCube",
            [ShaderParameterType.ArrayTexture1D] = "sampler1DArray",
            [ShaderParameterType.ArrayTexture2D] = "sampler2DArray",
            [ShaderParameterType.Tileset2D] = "sampler2DArray",
        }.ToFrozenDictionary();

    public static readonly FrozenDictionary<ShaderParameterType, Type> TextureRecordTypes =
        new Dictionary<ShaderParameterType, Type>() {
            [ShaderParameterType.Texture1D] = typeof(RTexture1D),
            [ShaderParameterType.Texture2D] = typeof(RTexture2D),
            [ShaderParameterType.Texture3D] = typeof(RTexture3D),
            [ShaderParameterType.Cubemap] = typeof(RCubemap),
            [ShaderParameterType.ArrayTexture1D] = typeof(RArrayTexture1D),
            [ShaderParameterType.ArrayTexture2D] = typeof(RArrayTexture2D),
            [ShaderParameterType.Tileset2D] = typeof(RTileset2D)
        }.ToFrozenDictionary();

    private static readonly EnumDictionary<ShaderParameterType, int> s_parameterSizes = new() {
        [ShaderParameterType.Int] = sizeof(int),
        [ShaderParameterType.UInt] = sizeof(uint),
        [ShaderParameterType.Bool] = sizeof(double),
        [ShaderParameterType.Float] = sizeof(float),
        [ShaderParameterType.Double] = sizeof(double),

        [ShaderParameterType.Vector2] = sizeof(float) * 2,
        [ShaderParameterType.Vector3] = sizeof(float) * 3,
        [ShaderParameterType.Vector4] = sizeof(float) * 4,

        [ShaderParameterType.DoubleVector2] = sizeof(double) * 2,
        [ShaderParameterType.DoubleVector3] = sizeof(double) * 3,
        [ShaderParameterType.DoubleVector4] = sizeof(double) * 4,

        [ShaderParameterType.IntVector2] = sizeof(int) * 2,
        [ShaderParameterType.IntVector3] = sizeof(int) * 3,
        [ShaderParameterType.IntVector4] = sizeof(int) * 4,

        [ShaderParameterType.UIntVector2] = sizeof(uint) * 2,
        [ShaderParameterType.UIntVector3] = sizeof(uint) * 3,
        [ShaderParameterType.UIntVector4] = sizeof(uint) * 4,

        [ShaderParameterType.Matrix4x4] = sizeof(float) * 16,
        [ShaderParameterType.Matrix4x3] = sizeof(float) * 12,
        [ShaderParameterType.Matrix3x3] = sizeof(float) * 9,
        [ShaderParameterType.Matrix3x2] = sizeof(float) * 6,
        [ShaderParameterType.Matrix2x2] = sizeof(float) * 4,

        [ShaderParameterType.DoubleMatrix4x4] = sizeof(double) * 16,
        [ShaderParameterType.DoubleMatrix4x3] = sizeof(double) * 12,
        [ShaderParameterType.DoubleMatrix3x3] = sizeof(double) * 9,
        [ShaderParameterType.DoubleMatrix3x2] = sizeof(double) * 6,
        [ShaderParameterType.DoubleMatrix2x2] = sizeof(double) * 4
    };

    private static readonly unsafe EnumDictionary<ShaderParameterType, Action<IntPtr, Dyn>> s_propertySetters = new() {
        [ShaderParameterType.Int] = (ptr, dyn) => *(int*)ptr = ((Dyn.Int)dyn).Value,
        [ShaderParameterType.UInt] = (ptr, dyn) => *(uint*)ptr = ((Dyn.UInt)dyn).Value,
        [ShaderParameterType.Bool] = (ptr, dyn) => *(bool*)ptr = ((Dyn.Bool)dyn).Value,
        [ShaderParameterType.Float] = (ptr, dyn) => *(float*)ptr = ((Dyn.Float)dyn).Value,
        [ShaderParameterType.Double] = (ptr, dyn) => *(double*)ptr = ((Dyn.Double)dyn).Value,

        [ShaderParameterType.Vector2] = (ptr, dyn) => *(Vector2*)ptr = ((Dyn.Vector2)dyn).Value,
        [ShaderParameterType.Vector3] = (ptr, dyn) => *(Vector3*)ptr = ((Dyn.Vector3)dyn).Value,
        [ShaderParameterType.Vector4] = (ptr, dyn) => *(Vector4*)ptr = ((Dyn.Vector4)dyn).Value,

        [ShaderParameterType.DoubleVector2] = (ptr, dyn) => {
            var convPtr = (double*)ptr;
            var convPar = (Dyn.DoubleVector2)dyn;
            convPtr[0] = convPar.X;
            convPtr[1] = convPar.Y;
        },
        [ShaderParameterType.DoubleVector3] = (ptr, dyn) => {
            var convPtr = (double*)ptr;
            var convPar = (Dyn.DoubleVector3)dyn;
            convPtr[0] = convPar.X;
            convPtr[1] = convPar.Y;
            convPtr[2] = convPar.Z;
        },
        [ShaderParameterType.DoubleVector4] = (ptr, dyn) => {
            var convPtr = (double*)ptr;
            var convPar = (Dyn.DoubleVector4)dyn;
            convPtr[0] = convPar.X;
            convPtr[1] = convPar.Y;
            convPtr[2] = convPar.Z;
            convPtr[3] = convPar.W;
        },

        [ShaderParameterType.IntVector2] = (ptr, dyn) => {
            var convPtr = (int*)ptr;
            var convPar = (Dyn.IntVector2)dyn;
            convPtr[0] = convPar.X;
            convPtr[1] = convPar.Y;
        },
        [ShaderParameterType.IntVector3] = (ptr, dyn) => {
            var convPtr = (int*)ptr;
            var convPar = (Dyn.IntVector3)dyn;
            convPtr[0] = convPar.X;
            convPtr[1] = convPar.Y;
            convPtr[2] = convPar.Z;
        },
        [ShaderParameterType.IntVector4] = (ptr, dyn) => {
            var convPtr = (int*)ptr;
            var convPar = (Dyn.IntVector4)dyn;
            convPtr[0] = convPar.X;
            convPtr[1] = convPar.Y;
            convPtr[2] = convPar.Z;
            convPtr[3] = convPar.W;
        },

        [ShaderParameterType.UIntVector2] = (ptr, dyn) => {
            var convPtr = (uint*)ptr;
            var convPar = (Dyn.UIntVector2)dyn;
            convPtr[0] = convPar.X;
            convPtr[1] = convPar.Y;
        },
        [ShaderParameterType.UIntVector3] = (ptr, dyn) => {
            var convPtr = (uint*)ptr;
            var convPar = (Dyn.UIntVector3)dyn;
            convPtr[0] = convPar.X;
            convPtr[1] = convPar.Y;
            convPtr[2] = convPar.Z;
        },
        [ShaderParameterType.UIntVector4] = (ptr, dyn) => {
            var convPtr = (uint*)ptr;
            var convPar = (Dyn.UIntVector4)dyn;
            convPtr[0] = convPar.X;
            convPtr[1] = convPar.Y;
            convPtr[2] = convPar.Z;
            convPtr[3] = convPar.W;
        },

        [ShaderParameterType.Matrix4x4] = (ptr, dyn) =>
            *(Matrix4x4*)ptr = ((Dyn.Matrix4x4)dyn).Value,
        [ShaderParameterType.Matrix4x3] = (ptr, dyn) =>
            ((Dyn.Matrix4x3)dyn).Value.CopyTo(new Span<float>((float*)ptr, 12)),
        [ShaderParameterType.Matrix3x3] = (ptr, dyn) =>
            ((Dyn.Matrix3x3)dyn).Value.CopyTo(new Span<float>((float*)ptr, 9)),
        [ShaderParameterType.Matrix3x2] = (ptr, dyn) =>
            *(Matrix3x2*)ptr = ((Dyn.Matrix3x2)dyn).Value,
        [ShaderParameterType.Matrix2x2] = (ptr, dyn) =>
            ((Dyn.Matrix2x2)dyn).Value.CopyTo(new Span<float>((float*)ptr, 4)),

        [ShaderParameterType.DoubleMatrix4x4] = (ptr, dyn) =>
            ((Dyn.DoubleMatrix4x4)dyn).Value.CopyTo(new Span<double>((double*)ptr, 16)),
        [ShaderParameterType.DoubleMatrix4x3] = (ptr, dyn) =>
            ((Dyn.DoubleMatrix4x3)dyn).Value.CopyTo(new Span<double>((double*)ptr, 12)),
        [ShaderParameterType.DoubleMatrix3x3] = (ptr, dyn) =>
            ((Dyn.DoubleMatrix3x3)dyn).Value.CopyTo(new Span<double>((double*)ptr, 9)),
        [ShaderParameterType.DoubleMatrix3x2] = (ptr, dyn) =>
            ((Dyn.DoubleMatrix3x2)dyn).Value.CopyTo(new Span<double>((double*)ptr, 6)),
        [ShaderParameterType.DoubleMatrix2x2] = (ptr, dyn) =>
            ((Dyn.DoubleMatrix2x2)dyn).Value.CopyTo(new Span<double>((double*)ptr, 4))
    };

    private static readonly ConcurrentDictionary<string, string> s_loadedEmbedded = new();

    public static string LoadCore(string id)
        => s_loadedEmbedded.GetOrAdd(id,
            id => EmbeddedAssets.LoadInternal<RText>("shaders." + id));

    public static string GenerateGLSLPropertiesStatement(
        IEnumerable<MaterialProperty> properties, Action<MaterialProperty, ShaderParameterType>? validPropertyCallback = null)
    {
        var sourceBuilder = new StringBuilder();
        sourceBuilder.Append("properties {");

        var texUniformsBuilder = new StringBuilder();

        void AppendParam(ShaderParameterType type, string glslType, MaterialProperty prop)
        {
            sourceBuilder.AppendLine();
            sourceBuilder.Append("    ");
            sourceBuilder.Append(glslType);
            sourceBuilder.Append(' ');
            sourceBuilder.Append(prop.Name);
            sourceBuilder.Append(';');
            validPropertyCallback?.Invoke(prop, type);
        }

        void AppendTexture(MaterialProperty prop, ShaderParameterType type, RImageBase image)
        {
            if (!ChannelTypeTextureSamplerMap.TryGetValue(image.ChannelType, out var typeChar)
                    || !TextureSamplerMap.TryGetValue(type, out var typeStr)) {
                return;
            }
            texUniformsBuilder.AppendLine();
            texUniformsBuilder.Append("uniform ");
            if (typeChar.HasValue) {
                texUniformsBuilder.Append(typeChar.Value);
            }
            texUniformsBuilder.Append(typeStr);
            texUniformsBuilder.Append(' ');
            texUniformsBuilder.Append(prop.Name);
            texUniformsBuilder.Append(';');
            validPropertyCallback?.Invoke(prop, ShaderParameterType.Texture2D);
        }

        foreach (var prop in properties) {
            switch (prop.Value) {
            case TextureDyn textureDyn:
                switch (textureDyn.Value) {
                case RRenderTexture2D tex:
                    AppendTexture(prop, ShaderParameterType.Texture2D, tex.Image);
                    break;
                case RTexture2D tex:
                    AppendTexture(prop, ShaderParameterType.Texture2D, tex.Image);
                    break;
                case RCubemap tex:
                    AppendTexture(prop, ShaderParameterType.Cubemap, tex.Images.Values.FirstOrDefault(RImage.Hint));
                    break;
                case RArrayTexture2D tex:
                    AppendTexture(prop, ShaderParameterType.ArrayTexture2D, tex.Images.FirstOrDefault(RImage.Hint));
                    break;
                case RTileset2D tex:
                    AppendTexture(prop, ShaderParameterType.ArrayTexture2D, tex.Image);
                    break;
                }
                break;
            case Dyn.Int: AppendParam(ShaderParameterType.Int, "int", prop); break;
            case Dyn.UInt: AppendParam(ShaderParameterType.UInt, "uint", prop); break;
            case Dyn.Bool: AppendParam(ShaderParameterType.Bool, "bool", prop); break;
            case Dyn.Float: AppendParam(ShaderParameterType.Float, "float", prop); break;
            case Dyn.Double: AppendParam(ShaderParameterType.Double, "double", prop); break;

            case Dyn.Vector2: AppendParam(ShaderParameterType.Vector2, "vec2", prop); break;
            case Dyn.Vector3: AppendParam(ShaderParameterType.Vector3, "vec3", prop); break;
            case Dyn.Vector4: AppendParam(ShaderParameterType.Vector4, "vec4", prop); break;

            case Dyn.DoubleVector2: AppendParam(ShaderParameterType.DoubleVector2, "dvec2", prop); break;
            case Dyn.DoubleVector3: AppendParam(ShaderParameterType.DoubleVector3, "dvec3", prop); break;
            case Dyn.DoubleVector4: AppendParam(ShaderParameterType.DoubleVector4, "dvec4", prop); break;

            case Dyn.IntVector2: AppendParam(ShaderParameterType.IntVector2, "ivec2", prop); break;
            case Dyn.IntVector3: AppendParam(ShaderParameterType.IntVector3, "ivec3", prop); break;
            case Dyn.IntVector4: AppendParam(ShaderParameterType.IntVector4, "ivec4", prop); break;

            case Dyn.UIntVector2: AppendParam(ShaderParameterType.UIntVector2, "uvec2", prop); break;
            case Dyn.UIntVector3: AppendParam(ShaderParameterType.UIntVector3, "uvec3", prop); break;
            case Dyn.UIntVector4: AppendParam(ShaderParameterType.UIntVector4, "uvec4", prop); break;

            case Dyn.BoolVector2: AppendParam(ShaderParameterType.BoolVector2, "bvec2", prop); break;
            case Dyn.BoolVector3: AppendParam(ShaderParameterType.BoolVector3, "bvec3", prop); break;
            case Dyn.BoolVector4: AppendParam(ShaderParameterType.BoolVector4, "bvec4", prop); break;

            case Dyn.Matrix4x4: AppendParam(ShaderParameterType.Matrix4x4, "mat4", prop); break;
            case Dyn.Matrix4x3: AppendParam(ShaderParameterType.Matrix4x3, "mat4x3", prop); break;
            case Dyn.Matrix3x3: AppendParam(ShaderParameterType.Matrix3x3, "mat3", prop); break;
            case Dyn.Matrix3x2: AppendParam(ShaderParameterType.Matrix3x2, "mat3x2", prop); break;
            case Dyn.Matrix2x2: AppendParam(ShaderParameterType.Matrix2x2, "mat2", prop); break;

            case Dyn.DoubleMatrix4x4: AppendParam(ShaderParameterType.DoubleMatrix4x4, "dmat4", prop); break;
            case Dyn.DoubleMatrix4x3: AppendParam(ShaderParameterType.DoubleMatrix4x3, "dmat4x3", prop); break;
            case Dyn.DoubleMatrix3x3: AppendParam(ShaderParameterType.DoubleMatrix3x3, "dmat3", prop); break;
            case Dyn.DoubleMatrix3x2: AppendParam(ShaderParameterType.DoubleMatrix3x2, "dmat3x2", prop); break;
            case Dyn.DoubleMatrix2x2: AppendParam(ShaderParameterType.DoubleMatrix2x2, "dmat2", prop); break;
            }
        }

        sourceBuilder.AppendLine();
        sourceBuilder.Append('}');
        return sourceBuilder.ToString() + texUniformsBuilder.ToString();
    }

    public static bool SetParameter(IntPtr pointer, ShaderParameterType type, Dyn value)
    {
        if (type == ShaderParameterType.Unit) {
            return value is Dyn.Unit;
        }
        if (TextureRecordTypes.TryGetValue(type, out var recordType)) {
            return value is TextureDyn texDyn &&
                (texDyn.Value == null || texDyn.Value.GetType() == recordType);
        }
        try {
            var setter = s_propertySetters[type];
            setter(pointer, value);
        }
        catch {
            return false;
        }
        return true;
    }

    public static unsafe void ClearParameter(IntPtr pointer, ShaderParameterType type)
    {
        if (type == ShaderParameterType.Unit
                || TextureSamplerMap.ContainsKey(type)) {
            return;
        }
        new Span<byte>((void*)pointer, s_parameterSizes[type]).Clear();
    }
}