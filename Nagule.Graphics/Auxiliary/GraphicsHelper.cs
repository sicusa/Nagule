namespace Nagule.Graphics;

using System.Text;
using System.Numerics;
using System.Collections.Immutable;
using System.Collections.Concurrent;

using Aeco;

public static class GraphicsHelper
{
    public static readonly string EmptyFragmentShader = "#version 410 core\nvoid main() { }";

    private static ConcurrentDictionary<string, string> s_loadedEmbededShaders = new();

    public static string LoadEmbededShader(string resourceId)
        => s_loadedEmbededShaders.GetOrAdd(resourceId,
            id => EmbededAssets.LoadText("Nagule.Graphics.Embeded.Shaders." + id));

    public static void LoadEmbededShaderPrograms(IContext context)
    {
        context.SetResource(Graphics.DefaultShaderProgramId,
            new GLSLProgram { Name = "nagule.blinn_phong" }
                .WithShaders(
                    new(ShaderType.Vertex, LoadEmbededShader("blinn_phong.vert.glsl")),
                    new(ShaderType.Fragment, LoadEmbededShader("blinn_phong.frag.glsl")))
                .WithParameters(
                    new(MaterialKeys.Tiling),
                    new(MaterialKeys.Offset),
                    new(MaterialKeys.Diffuse),
                    new(MaterialKeys.DiffuseTex),
                    new(MaterialKeys.Specular),
                    new(MaterialKeys.SpecularTex),
                    new(MaterialKeys.RoughnessTex),
                    new(MaterialKeys.Ambient),
                    new(MaterialKeys.AmbientTex),
                    new(MaterialKeys.AmbientOcclusionTex),
                    new(MaterialKeys.AmbientOcclusionMultiplier),
                    new(MaterialKeys.Emission),
                    new(MaterialKeys.EmissionTex),
                    new(MaterialKeys.Shininess),
                    new(MaterialKeys.Reflectivity),
                    new(MaterialKeys.ReflectionTex),
                    new(MaterialKeys.OpacityTex),
                    new(MaterialKeys.Threshold),
                    new(MaterialKeys.NormalTex),
                    new(MaterialKeys.HeightTex),
                    new(MaterialKeys.ParallaxScale),
                    new(MaterialKeys.EnableParallaxEdgeClip),
                    new(MaterialKeys.EnableParallaxShadow)));

        context.SetResource(Graphics.DefaultDepthShaderProgramId,
            new GLSLProgram { Name = "nagule.depth" }
                .WithShaders(
                    new(ShaderType.Vertex, LoadEmbededShader("nagule.common.simple.vert.glsl")),
                    new(ShaderType.Fragment, EmptyFragmentShader)));
    }

    public static GLSLProgram TransformMaterialShaderProgram(
        IContext context, Material material, Action<IContext, string, Dyn>? propertyHandler = null)
    {
        var program = material.ShaderProgram ??
            context.Inspect<Resource<GLSLProgram>>(Graphics.DefaultShaderProgramId).Value;
        
        var macros = program.Macros.ToBuilder();
        macros.Add("RenderMode_" + Enum.GetName(material.RenderMode));
        macros.Add("LightingMode_" + Enum.GetName(material.LightingMode));
        
        var props = material.Properties;
        if (props.Count == 0) {
            return program with { Macros = macros.ToImmutable() };
        }

        var programPars = program.Parameters;
        if (propertyHandler != null) {
            foreach (var (name, value) in props) {
                if (!programPars.ContainsKey(name)) {
                    continue;
                }
                macros.Add("_" + name);
                propertyHandler(context, name, value);
            }
        }
        else {
            foreach (var (name, value) in props) {
                if (!programPars.ContainsKey(name)) {
                    continue;
                }
                macros.Add("_" + name);
            }
        }

        return program with { Macros = macros.ToImmutable() };
    }

    private unsafe static EnumArray<ShaderParameterType, Action<IntPtr, Dyn>> s_propertySetters = new() {
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
            ((Dyn.DoubleMatrix2x2)dyn).Value.CopyTo(new Span<double>((double*)ptr, 4)),
    };

    public static void SetShaderParameter(string name, ShaderParameterType type, Dyn value, IntPtr pointer)
    {
        bool success = true;

        switch (type) {
        case ShaderParameterType.Unit:
            success = value is Dyn.Unit;
            break;
        case ShaderParameterType.Texture:
            success = value is TextureDyn;
            break;
        default:
            try {
                var setter = s_propertySetters[type];
                setter(pointer, value);
            }
            catch {
                success = false;
            }
            break;
        }

        if (!success) {
            Console.WriteLine(
                $"Error: parameter '{name}' has type {Enum.GetName(type)} that does not match with argument type " + value.GetType());
        }
    }

    public static string GenerateGLSLPropertiesStatement(IEnumerable<MaterialProperty> properties, Action<MaterialProperty, ShaderParameterType>? validPropertyCallback = null)
    {
        var sourceBuilder = new StringBuilder();
        sourceBuilder.Append("properties {");

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

        foreach (var prop in properties) {
            switch (prop.Value) {
            case Dyn.Int conv: AppendParam(ShaderParameterType.Int, "int", prop); break;
            case Dyn.UInt conv: AppendParam(ShaderParameterType.UInt, "uint", prop); break;
            case Dyn.Bool conv: AppendParam(ShaderParameterType.Bool, "bool", prop); break;
            case Dyn.Float conv: AppendParam(ShaderParameterType.Float, "float", prop); break;
            case Dyn.Double conv: AppendParam(ShaderParameterType.Double, "double", prop); break;

            case Dyn.Vector2 conv: AppendParam(ShaderParameterType.Vector2, "vec2", prop); break;
            case Dyn.Vector3 conv: AppendParam(ShaderParameterType.Vector3, "vec3", prop); break;
            case Dyn.Vector4 conv: AppendParam(ShaderParameterType.Vector4, "vec4", prop); break;

            case Dyn.DoubleVector2 conv: AppendParam(ShaderParameterType.DoubleVector2, "dvec2", prop); break;
            case Dyn.DoubleVector3 conv: AppendParam(ShaderParameterType.DoubleVector3, "dvec3", prop); break;
            case Dyn.DoubleVector4 conv: AppendParam(ShaderParameterType.DoubleVector4, "dvec4", prop); break;

            case Dyn.IntVector2 conv: AppendParam(ShaderParameterType.IntVector2, "ivec2", prop); break;
            case Dyn.IntVector3 conv: AppendParam(ShaderParameterType.IntVector3, "ivec3", prop); break;
            case Dyn.IntVector4 conv: AppendParam(ShaderParameterType.IntVector4, "ivec4", prop); break;

            case Dyn.UIntVector2 conv: AppendParam(ShaderParameterType.UIntVector2, "uvec2", prop); break;
            case Dyn.UIntVector3 conv: AppendParam(ShaderParameterType.UIntVector3, "uvec3", prop); break;
            case Dyn.UIntVector4 conv: AppendParam(ShaderParameterType.UIntVector4, "uvec4", prop); break;

            case Dyn.BoolVector2 conv: AppendParam(ShaderParameterType.BoolVector2, "bvec2", prop); break;
            case Dyn.BoolVector3 conv: AppendParam(ShaderParameterType.BoolVector3, "bvec3", prop); break;
            case Dyn.BoolVector4 conv: AppendParam(ShaderParameterType.BoolVector4, "bvec4", prop); break;

            case Dyn.Matrix4x4 conv: AppendParam(ShaderParameterType.Matrix4x4, "mat4", prop); break;
            case Dyn.Matrix4x3 conv: AppendParam(ShaderParameterType.Matrix4x3, "mat4x3", prop); break;
            case Dyn.Matrix3x3 conv: AppendParam(ShaderParameterType.Matrix3x3, "mat3", prop); break;
            case Dyn.Matrix3x2 conv: AppendParam(ShaderParameterType.Matrix3x2, "mat3x2", prop); break;
            case Dyn.Matrix2x2 conv: AppendParam(ShaderParameterType.Matrix2x2, "mat2", prop); break;

            case Dyn.DoubleMatrix4x4 conv: AppendParam(ShaderParameterType.DoubleMatrix4x4, "dmat4", prop); break;
            case Dyn.DoubleMatrix4x3 conv: AppendParam(ShaderParameterType.DoubleMatrix4x3, "dmat4x3", prop); break;
            case Dyn.DoubleMatrix3x3 conv: AppendParam(ShaderParameterType.DoubleMatrix3x3, "dmat3", prop); break;
            case Dyn.DoubleMatrix3x2 conv: AppendParam(ShaderParameterType.DoubleMatrix3x2, "dmat3x2", prop); break;
            case Dyn.DoubleMatrix2x2 conv: AppendParam(ShaderParameterType.DoubleMatrix2x2, "dmat2", prop); break;
            }
        }

        sourceBuilder.AppendLine();
        sourceBuilder.Append('}');
        return sourceBuilder.ToString();
    }
}