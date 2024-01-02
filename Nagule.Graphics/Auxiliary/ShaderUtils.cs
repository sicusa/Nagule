namespace Nagule.Graphics;

using System.Text;
using System.Numerics;
using System.Collections.Concurrent;

public static class ShaderUtils
{
    public static readonly string EmptyFragmentShader = "#version 410 core\nvoid main() { }";

    public static IReadOnlyDictionary<ShaderParameterType, int> ParameterSizes => s_parameterSizes;
    public static IReadOnlyDictionary<string, string> InternalShaders => s_internalShaders;

    private static readonly Dictionary<string, string> s_internalShaders = new() {
        ["nagule/common.glsl"] = LoadCore("nagule.common.glsl"),
        ["nagule/noise.glsl"] = LoadCore("nagule.noise.glsl"),
        ["nagule/transparency.glsl"] = LoadCore("nagule.transparency.glsl"),
        ["nagule/lighting.glsl"] = LoadCore("nagule.lighting.glsl"),
        ["nagule/parallax_mapping.glsl"] = LoadCore("nagule.parallax_mapping.glsl")
    };

    private static ConcurrentDictionary<string, string>? s_loadedEmbedded;

    public static string LoadCore(string id)
        => (s_loadedEmbedded ??= new()).GetOrAdd(id,
            id => EmbeddedAssets.LoadText("Nagule.Graphics.Embedded.Shaders." + id));

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

    public static string GenerateGLSLPropertiesStatement(
        IEnumerable<MaterialProperty> properties, Action<MaterialProperty, ShaderParameterType>? validPropertyCallback = null)
    {
        var sourceBuilder = new StringBuilder();
        sourceBuilder.Append("properties {");

        var texturesBuilder = new StringBuilder();

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

        void AppendTexture2D(MaterialProperty prop)
        {
            texturesBuilder.AppendLine();
            texturesBuilder.Append("uniform sampler2D ");
            texturesBuilder.Append(prop.Name);
            texturesBuilder.Append(';');
            validPropertyCallback?.Invoke(prop, ShaderParameterType.Texture2D);
        }

        /* TODO: support 3d texture
        void AppendTexture3D(MaterialProperty prop)
        {
            texturesBuilder.AppendLine();
            texturesBuilder.Append("uniform sampler3d ");
            texturesBuilder.Append(prop.Name);
            texturesBuilder.Append(';');
            validPropertyCallback?.Invoke(prop, ShaderParameterType.Texture2D);
        }*/

        void AppendCubemap(MaterialProperty prop)
        {
            texturesBuilder.AppendLine();
            texturesBuilder.Append("uniform samplerCube ");
            texturesBuilder.Append(prop.Name);
            texturesBuilder.Append(';');
            validPropertyCallback?.Invoke(prop, ShaderParameterType.Texture2D);
        }

        foreach (var prop in properties) {
            switch (prop.Value) {
            case TextureDyn textureDyn:
                switch (textureDyn.Value) {
                case RRenderTexture2D:
                case RTexture2D: AppendTexture2D(prop); break;
                case RCubemap: AppendCubemap(prop); break;
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
        return sourceBuilder.ToString() + texturesBuilder.ToString();
    }

    public static bool SetParameter(IntPtr pointer, ShaderParameterType type, Dyn value)
    {
        switch (type) {
            case ShaderParameterType.Unit:
                return true;
            case ShaderParameterType.Texture2D: {
                return value is TextureDyn texDyn
                    && (texDyn.Value is RTexture2D || texDyn.Value is RRenderTexture2D);
            }
            case ShaderParameterType.Texture3D:
                // TODO: support 3d texture
                return false;
            case ShaderParameterType.Cubemap: {
                return value is TextureDyn texDyn && texDyn.Value is RCubemap;
            }
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
        switch (type) {
            case ShaderParameterType.Unit:
            case ShaderParameterType.Texture2D:
            case ShaderParameterType.Texture3D:
            case ShaderParameterType.Cubemap:
                return;
        }
        new Span<byte>((void*)pointer, s_parameterSizes[type]).Clear();
    }
}