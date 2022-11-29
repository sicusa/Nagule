namespace Nagule.Backend.OpenTK.Graphics;

using System.Numerics;
using System.Collections.Immutable;
using System.Text.RegularExpressions;

using global::OpenTK.Graphics.OpenGL4;

using Aeco;
using Nagule.Graphics;

public class ShaderProgramManager : ResourceManagerBase<ShaderProgram, ShaderProgramData, ShaderProgramResource>
{
    private static string LoadShader(string resourceId)
        => InternalAssets.LoadText("Nagule.Backend.OpenTK.Embeded.Shaders." + resourceId);

    private static readonly Dictionary<string, string> s_internalShaderFiles = new() {
        ["nagule/common.glsl"] = LoadShader("nagule.common.glsl"),
        ["nagule/variant.glsl"] = LoadShader("nagule.variant.glsl"),
        ["nagule/instancing.glsl"] = LoadShader("nagule.instancing.glsl"),
        ["nagule/transparency.glsl"] = LoadShader("nagule.transparency.glsl"),
        ["nagule/lighting.glsl"] = LoadShader("nagule.lighting.glsl"),
        ["nagule/blinn_phong.glsl"] = LoadShader("nagule.blinn_phong.glsl")
    };

    private static readonly Dictionary<Type, Action<int, object>> s_uniformSetters = new() {
        [typeof(int)] = (location, value) => GL.Uniform1(location, (int)value),
        [typeof(int[])] = (location, value) => {
            var arr = (int[])value;
            GL.Uniform1(location, arr.Length, arr);
        },
        [typeof(float)] = (location, value) => GL.Uniform1(location, (float)value),
        [typeof(float[])] = (location, value) => {
            var arr = (float[])value;
            GL.Uniform1(location, arr.Length, arr);
        },
        [typeof(double)] = (location, value) => GL.Uniform1(location, (double)value),
        [typeof(double[])] = (location, value) => {
            var arr = (double[])value;
            GL.Uniform1(location, arr.Length, arr);
        },
        [typeof(Vector2)] = (location, value) => {
            var vec = (Vector2)value;
            GL.Uniform2(location, vec.X, vec.Y);
        },
        [typeof(Vector2[])] = (location, value) => {
            var arr = (Vector2[])value;
            GL.Uniform2(location, arr.Length, ref arr[0].X);
        },
        [typeof(Vector3)] = (location, value) => {
            var vec = (Vector3)value;
            GL.Uniform3(location, vec.X, vec.Y, vec.Z);
        },
        [typeof(Vector3[])] = (location, value) => {
            var arr = (Vector3[])value;
            GL.Uniform3(location, arr.Length, ref arr[0].X);
        },
        [typeof(Vector4)] = (location, value) => {
            var vec = (Vector4)value;
            GL.Uniform4(location, vec.X, vec.Y, vec.Z, vec.W);
        },
        [typeof(Vector4[])] = (location, value) => {
            var arr = (Vector4[])value;
            GL.Uniform3(location, arr.Length, ref arr[0].X);
        },
        [typeof(Matrix3x2)] = (location, value) => {
            var mat = (Matrix3x2)value;
            GL.UniformMatrix2x3(location, 1, true, ref mat.M11);
        },
        [typeof(Matrix3x2[])] = (location, value) => {
            var arr = (Matrix3x2[])value;
            GL.UniformMatrix2x3(location, arr.Length, true, ref arr[0].M11);
        },
        [typeof(Matrix4x4)] = (location, value) => {
            var mat = (Matrix4x4)value;
            GL.UniformMatrix4(location, 1, true, ref mat.M11);
        },
        [typeof(Matrix4x4[])] = (location, value) => {
            var arr = (Matrix4x4[])value;
            GL.UniformMatrix4(location, arr.Length, true, ref arr[0].M11);
        },
    };

    public string Desugar(string source)
        => Regex.Replace(source, "(\\#include \\<(?<file>.+)\\>)", match => {
            var filePath = match.Groups["file"].Value;
            if (!s_internalShaderFiles.TryGetValue(filePath, out var result)) {
                Console.WriteLine("Shader file not found: " + match.Value);
                return "";
            }
            return Desugar(result);
        });

    protected override void Initialize(
        IContext context, Guid id, ref ShaderProgram shaderProgram, ref ShaderProgramData data, bool updating)
    {
        if (updating) {
            GL.DeleteProgram(data.Handle);
        }

        var resource = shaderProgram.Resource;
        var shaders = resource.Shaders;

        // create program

        int program = GL.CreateProgram();
        var shaderHandles = new int[shaders.Length];

        try {
            for (int i = 0; i != shaders.Length; ++i) {
                var source = shaders[i];
                if (source == null) { continue; }

                int handle = CompileShader((Nagule.Graphics.ShaderType)i, source);
                if (handle != 0) {
                    GL.AttachShader(program, handle);
                }
                shaderHandles[i] = handle;
            }
        }
        catch {
            GL.DeleteProgram(program);
            throw;
        }

        // apply other settings

        if (resource.TransformFeedbackVaryings != null) {
            GL.TransformFeedbackVaryings(program,
                resource.TransformFeedbackVaryings.Length, resource.TransformFeedbackVaryings,
                TransformFeedbackMode.InterleavedAttribs);
        }

        // link program

        GL.LinkProgram(program);
        GL.GetProgram(program, GetProgramParameterName.LinkStatus, out var success);

        if (success == 0) {
            string infoLog = GL.GetProgramInfoLog(program);
            Console.WriteLine(infoLog);
        }

        // detach shaders

        for (int i = 0; i != shaderHandles.Length; ++i) {
            int handle = shaderHandles[i];
            if (handle != 0) {
                GL.DetachShader(program, handle);
                GL.DeleteShader(handle);
            }
        }

        // initialize uniform locations

        data.DepthBufferLocation = GL.GetUniformLocation(program, "DepthBuffer");
        data.LightsBufferLocation = GL.GetUniformLocation(program, "LightsBuffer");
        data.ClustersBufferLocation = GL.GetUniformLocation(program, "ClustersBuffer");
        data.ClusterLightCountsBufferLocation = GL.GetUniformLocation(program, "ClusterLightCountsBuffer");

        EnumArray<TextureType, int>? textureLocations = null;
        if (resource.IsMaterialTexturesEnabled) {
            textureLocations = new EnumArray<TextureType, int>();
            for (int i = 0; i != textureLocations.Length - 1; i++) {
                textureLocations[i] = GL.GetUniformLocation(program, Enum.GetName((TextureType)i)! + "Tex");
            }
        }

        var customLocations = ImmutableDictionary<string, int>.Empty;
        if (resource.CustomUniforms != null) {
            var builder = ImmutableDictionary.CreateBuilder<string, int>();
            foreach (var uniform in resource.CustomUniforms) {
                var location = GL.GetUniformLocation(program, uniform);
                if (location == -1) {
                    Console.WriteLine($"Custom uniform '{uniform}' not found");
                    continue;
                }
                builder.Add(uniform, location);
            }
            customLocations = builder.ToImmutable();
        }

        if (resource.DefaultUniformValues != null) {
            foreach (var (name, value) in resource.DefaultUniformValues) {
                var location = GL.GetUniformLocation(program, name);
                if (location == -1) {
                    Console.WriteLine($"Failed to set uniform '{name}': uniform not found.");
                }
                if (!s_uniformSetters.TryGetValue(value.GetType(), out var setter)) {
                    Console.WriteLine($"Failed to set uniform '{name}': uniform type unrecognized.");
                    continue;
                }
                try {
                    setter(location, value);
                }
                catch (Exception e) {
                    Console.WriteLine($"Failed to set uniform '{name}': " + e.Message);
                }
            }
        }

        EnumArray<Nagule.Graphics.ShaderType, ImmutableDictionary<string, int>>? subroutineIndeces = null;
        if (resource.Subroutines != null) {
            subroutineIndeces = new();
            Nagule.Graphics.ShaderType shaderType = 0;

            foreach (var names in resource.Subroutines) {
                var indeces = subroutineIndeces[shaderType] ?? ImmutableDictionary<string, int>.Empty;
                if (names == null) {
                    subroutineIndeces[shaderType] = indeces;
                    continue;
                }
                foreach (var name in names) {
                    var index = GL.GetSubroutineIndex(program, ToGLShaderType(shaderType), name);
                    if (index == -1) {
                        Console.WriteLine($"Subroutine index '{name}' not found");
                        continue;
                    }
                    indeces = indeces.Add(name, index);
                }
                subroutineIndeces[shaderType] = indeces;
                ++shaderType;
            }
        }

        var blockLocations = new BlockLocations {
            FramebufferBlock = BindUniformBlock(program, "Framebuffer", UniformBlockBinding.Framebuffer),
            CameraBlock = BindUniformBlock(program, "Camera", UniformBlockBinding.Camera),
            LightingEnvBlock = BindUniformBlock(program, "LightingEnv", UniformBlockBinding.LightingEnv),
            MaterialBlock = BindUniformBlock(program, "Material", UniformBlockBinding.Material),
            MeshBlock = BindUniformBlock(program, "Mesh", UniformBlockBinding.Mesh),
            ObjectBlock = BindUniformBlock(program, "Object", UniformBlockBinding.Object)
        };

        // finish initialization

        data.Handle = program;
        data.TextureLocations = textureLocations;
        data.CustomLocations = customLocations;
        data.SubroutineIndeces = subroutineIndeces;
        data.BlockLocations = blockLocations;
    } 

    private static int BindUniformBlock(int program, string name, UniformBlockBinding binding)
    {
        var index = GL.GetUniformBlockIndex(program, name);
        if (index != -1) {
            GL.UniformBlockBinding(program, index, (int)binding);
        }
        return index;
    }

    private int CompileShader(Nagule.Graphics.ShaderType type, string source)
    {
        var glShaderType = ToGLShaderType(type);
        int handle = GL.CreateShader(glShaderType);
        GL.ShaderSource(handle, Desugar(source));

        GL.CompileShader(handle);
        GL.GetShader(handle, ShaderParameter.CompileStatus, out int success);

        if (success == 0) {
            string infoLog = GL.GetShaderInfoLog(handle);
            Console.WriteLine(infoLog);
            GL.DeleteShader(handle);
            return 0;
        }
        return handle;
    }

    protected override void Uninitialize(
        IContext context, Guid id, in ShaderProgram shaderProgram, in ShaderProgramData data)
    {
        GL.DeleteProgram(data.Handle);
    }

    private global::OpenTK.Graphics.OpenGL4.ShaderType ToGLShaderType(Nagule.Graphics.ShaderType type)
        => type switch {
            Nagule.Graphics.ShaderType.Fragment => global::OpenTK.Graphics.OpenGL4.ShaderType.FragmentShader,
            Nagule.Graphics.ShaderType.Vertex => global::OpenTK.Graphics.OpenGL4.ShaderType.VertexShader,
            Nagule.Graphics.ShaderType.Geometry => global::OpenTK.Graphics.OpenGL4.ShaderType.GeometryShader,
            Nagule.Graphics.ShaderType.Compute => global::OpenTK.Graphics.OpenGL4.ShaderType.ComputeShader,
            Nagule.Graphics.ShaderType.TessellationEvaluation => global::OpenTK.Graphics.OpenGL4.ShaderType.TessEvaluationShader,
            Nagule.Graphics.ShaderType.TessellationControl => global::OpenTK.Graphics.OpenGL4.ShaderType.TessControlShader,
            _ => throw new NotSupportedException("Unknown shader type: " + type)
        };
}