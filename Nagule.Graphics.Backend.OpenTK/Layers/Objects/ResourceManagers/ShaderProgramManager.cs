namespace Nagule.Graphics.Backend.OpenTK.Graphics;

using System.Text;
using System.Numerics;
using System.Collections.Immutable;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;

using global::OpenTK.Graphics;
using global::OpenTK.Graphics.OpenGL;

using Aeco;
using Nagule.Graphics;

public class ShaderProgramManager : ResourceManagerBase<ShaderProgram, ShaderProgramData, ShaderProgramResource>
{
    private static string LoadShader(string resourceId)
        => InternalAssets.LoadText("Nagule.Graphics.Backend.OpenTK.Embeded.Shaders." + resourceId);

    private static readonly Dictionary<string, string> s_internalShaderFiles = new() {
        ["nagule/common.glsl"] = LoadShader("nagule.common.glsl"),
        ["nagule/variant.glsl"] = LoadShader("nagule.variant.glsl"),
        ["nagule/instancing.glsl"] = LoadShader("nagule.instancing.glsl"),
        ["nagule/transparency.glsl"] = LoadShader("nagule.transparency.glsl"),
        ["nagule/lighting.glsl"] = LoadShader("nagule.lighting.glsl"),
        ["nagule/blinn_phong.glsl"] = LoadShader("nagule.blinn_phong.glsl")
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

    protected unsafe override void Initialize(
        IContext context, Guid id, ref ShaderProgram shaderProgram, ref ShaderProgramData data, bool updating)
    {
        if (updating) {
            GL.DeleteProgram(data.Handle);
        }

        var resource = shaderProgram.Resource;
        var shaders = resource.Shaders;

        // create program

        var program = GL.CreateProgram();
        var shaderHandles = new ShaderHandle[shaders.Length];

        try {
            for (int i = 0; i != shaders.Length; ++i) {
                var source = shaders[i];
                if (source == null) { continue; }

                var handle = CompileShader((Nagule.Graphics.ShaderType)i, source);
                if (handle != ShaderHandle.Zero) {
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
            var varyings = resource.TransformFeedbackVaryings;
            var allocatedMemory = new List<IntPtr>();
            int intPtrSize = Marshal.SizeOf(typeof(IntPtr));
            IntPtr varyingsArrPtr = Marshal.AllocHGlobal(intPtrSize * varyings.Length);

            for (int i = 0; i != varyings.Length; ++i) {
                IntPtr byteArrPtr = Marshal.StringToHGlobalAnsi(varyings[i]);
                allocatedMemory.Add(byteArrPtr);
                Marshal.WriteIntPtr(varyingsArrPtr, i * intPtrSize, byteArrPtr);
            }

            GL.TransformFeedbackVaryings(program,
                resource.TransformFeedbackVaryings.Length, (byte**)varyingsArrPtr,
                TransformFeedbackBufferMode.InterleavedAttribs);

            Marshal.FreeHGlobal(varyingsArrPtr);
            foreach (var ptr in allocatedMemory) {
                Marshal.FreeHGlobal(ptr);
            }
        }

        // link program

        int success = 0;
        GL.LinkProgram(program);
        GL.GetProgrami(program, ProgramPropertyARB.LinkStatus, ref success);

        if (success == 0) {
            GL.GetProgramInfoLog(program, out var infoLog);
            Console.WriteLine(infoLog);
        }

        // detach shaders

        for (int i = 0; i != shaderHandles.Length; ++i) {
            var handle = shaderHandles[i];
            if (handle != ShaderHandle.Zero) {
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

        EnumArray<Nagule.Graphics.ShaderType, ImmutableDictionary<string, uint>>? subroutineIndeces = null;
        if (resource.Subroutines != null) {
            subroutineIndeces = new();
            Nagule.Graphics.ShaderType shaderType = 0;

            foreach (var names in resource.Subroutines) {
                var indeces = subroutineIndeces[shaderType] ?? ImmutableDictionary<string, uint>.Empty;
                if (names == null) {
                    subroutineIndeces[shaderType] = indeces;
                    continue;
                }
                foreach (var name in names) {
                    var index = GL.GetSubroutineIndex(program, ToGLShaderType(shaderType), name);
                    if (index == uint.MaxValue) {
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

    private static uint BindUniformBlock(ProgramHandle program, string name, UniformBlockBinding binding)
    {
        var index = GL.GetUniformBlockIndex(program, name);
        if (index != uint.MaxValue) {
            GL.UniformBlockBinding(program, index, (uint)binding);
        }
        return index;
    }

    private ShaderHandle CompileShader(Nagule.Graphics.ShaderType type, string source)
    {
        var glShaderType = ToGLShaderType(type);
        var handle = GL.CreateShader(glShaderType);
        GL.ShaderSource(handle, Desugar(source));

        int status = 0;
        GL.CompileShader(handle);
        GL.GetShaderi(handle, ShaderParameterName.CompileStatus, ref status);

        if (status == 0) {
            GL.GetShaderInfoLog(handle, out string infoLog);
            Console.WriteLine(infoLog);
            GL.DeleteShader(handle);
            return ShaderHandle.Zero;
        }
        return handle;
    }

    protected override void Uninitialize(
        IContext context, Guid id, in ShaderProgram shaderProgram, in ShaderProgramData data)
    {
        GL.DeleteProgram(data.Handle);
    }

    private global::OpenTK.Graphics.OpenGL.ShaderType ToGLShaderType(Nagule.Graphics.ShaderType type)
        => type switch {
            Nagule.Graphics.ShaderType.Fragment => global::OpenTK.Graphics.OpenGL.ShaderType.FragmentShader,
            Nagule.Graphics.ShaderType.Vertex => global::OpenTK.Graphics.OpenGL.ShaderType.VertexShader,
            Nagule.Graphics.ShaderType.Geometry => global::OpenTK.Graphics.OpenGL.ShaderType.GeometryShader,
            Nagule.Graphics.ShaderType.Compute => global::OpenTK.Graphics.OpenGL.ShaderType.ComputeShader,
            Nagule.Graphics.ShaderType.TessellationEvaluation => global::OpenTK.Graphics.OpenGL.ShaderType.TessEvaluationShader,
            Nagule.Graphics.ShaderType.TessellationControl => global::OpenTK.Graphics.OpenGL.ShaderType.TessControlShader,
            _ => throw new NotSupportedException("Unknown shader type: " + type)
        };
}