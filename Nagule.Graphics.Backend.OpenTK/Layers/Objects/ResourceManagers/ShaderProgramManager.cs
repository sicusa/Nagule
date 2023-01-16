namespace Nagule.Graphics.Backend.OpenTK;

using System.Collections.Immutable;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;

using global::OpenTK.Graphics;
using global::OpenTK.Graphics.OpenGL;

using Aeco;
using Nagule.Graphics;

using ShaderType = Nagule.Graphics.ShaderType;
using GLShaderType = global::OpenTK.Graphics.OpenGL.ShaderType;

public class ShaderProgramManager : ResourceManagerBase<ShaderProgram>
{
    private class InitializeCommand : Command<InitializeCommand, GraphicsResourceTarget>
    {
        public Guid ShaderProgramId;
        public ShaderProgram? Resource;
        public CancellationToken Token = default;

        public unsafe override void Execute(ICommandContext context)
        {
            var data = new ShaderProgramData();
            var shaders = Resource!.Shaders;

            // create program

            var program = GL.CreateProgram();
            var shaderHandles = new ShaderHandle[(int)ShaderType.Unknown];

            try {
                foreach (var (type, source) in Resource.Shaders) {
                    var handle = CompileShader(type, source);
                    if (handle != ShaderHandle.Zero) {
                        GL.AttachShader(program, handle);
                    }
                    shaderHandles[(int)type] = handle;
                }
            }
            catch {
                GL.DeleteProgram(program);
                throw;
            }

            // apply other settings

            if (Resource.TransformFeedbackVaryings != null) {
                var varyings = Resource.TransformFeedbackVaryings;
                var allocatedMemory = new List<IntPtr>();
                int intPtrSize = Marshal.SizeOf(typeof(IntPtr));
                IntPtr varyingsArrPtr = Marshal.AllocHGlobal(intPtrSize * varyings.Count);

                foreach (var varying in varyings) {
                    IntPtr byteArrPtr = Marshal.StringToHGlobalAnsi(varying);
                    Marshal.WriteIntPtr(varyingsArrPtr, allocatedMemory.Count * intPtrSize, byteArrPtr);
                    allocatedMemory.Add(byteArrPtr);
                }

                GL.TransformFeedbackVaryings(program,
                    Resource.TransformFeedbackVaryings.Count, (byte**)varyingsArrPtr,
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
            if (Resource.IsMaterialTexturesEnabled) {
                textureLocations = new EnumArray<TextureType, int>();
                for (int i = 0; i != textureLocations.Length - 1; i++) {
                    textureLocations[i] = GL.GetUniformLocation(program, Enum.GetName((TextureType)i)! + "Tex");
                }
            }

            var customParameters = ImmutableDictionary<string, (ShaderParameterType, int)>.Empty;
            if (Resource.CustomParameters.Count != 0) {
                var builder = ImmutableDictionary.CreateBuilder<string, (ShaderParameterType, int)>();
                foreach (var (uniform, type) in Resource.CustomParameters) {
                    var location = GL.GetUniformLocation(program, uniform);
                    if (location == -1) {
                        Console.WriteLine($"Custom uniform '{uniform}' not found");
                        continue;
                    }
                    builder.Add(uniform, (type, location));
                }
                customParameters = builder.ToImmutable();
            }

            EnumArray<Nagule.Graphics.ShaderType, ImmutableDictionary<string, uint>>? subroutineIndices = null;
            if (Resource.Subroutines.Count != 0) {
                subroutineIndices = new();
                foreach (var (shaderType, names) in Resource.Subroutines) {
                    var indices = subroutineIndices[shaderType] ?? ImmutableDictionary<string, uint>.Empty;
                    foreach (var name in names) {
                        var index = GL.GetSubroutineIndex(program, ToGLShaderType(shaderType), name);
                        if (index == uint.MaxValue) {
                            Console.WriteLine($"Subroutine index '{name}' not found");
                            continue;
                        }
                        indices = indices.Add(name, index);
                    }
                    subroutineIndices[shaderType] = indices;
                }
            }

            var blockLocations = new BlockLocations {
                PipelineBlock = BindUniformBlock(program, "Pipeline", UniformBlockBinding.Pipeline),
                CameraBlock = BindUniformBlock(program, "Camera", UniformBlockBinding.Camera),
                LightingEnvBlock = BindUniformBlock(program, "LightingEnv", UniformBlockBinding.LightingEnv),
                MaterialBlock = BindUniformBlock(program, "Material", UniformBlockBinding.Material),
                MeshBlock = BindUniformBlock(program, "Mesh", UniformBlockBinding.Mesh),
                ObjectBlock = BindUniformBlock(program, "Object", UniformBlockBinding.Object)
            };

            // finish initialization

            data.Handle = program;
            data.TextureLocations = textureLocations;
            data.CustomParameters = customParameters;
            data.SubroutineIndices = subroutineIndices;
            data.BlockLocations = blockLocations;

            context.SendRenderData(ShaderProgramId, data, Token,
                (id, data) => GL.DeleteProgram(data.Handle));
        }
    }

    private class UninitializeCommand : Command<UninitializeCommand, RenderTarget>
    {
        public Guid ShaderProgramId;

        public override void Execute(ICommandContext context)
        {
            if (context.Remove<ShaderProgramData>(ShaderProgramId, out var data)) {
                GL.DeleteProgram(data.Handle);
            }
        }
    }

    private static string LoadShader(string resourceId)
        => InternalAssets.LoadText("Nagule.Graphics.Backend.OpenTK.Embeded.Shaders." + resourceId);

    private static readonly Dictionary<string, string> s_internalShaderFiles = new() {
        ["nagule/common.glsl"] = LoadShader("nagule.common.glsl"),
        ["nagule/variant.glsl"] = LoadShader("nagule.variant.glsl"),
        ["nagule/instancing.glsl"] = LoadShader("nagule.instancing.glsl"),
        ["nagule/transparency.glsl"] = LoadShader("nagule.transparency.glsl"),
        ["nagule/lighting.glsl"] = LoadShader("nagule.lighting.glsl"),
        ["nagule/blinn_phong.glsl"] = LoadShader("nagule.blinn_phong.glsl"),
    };

    protected override void Initialize(
        IContext context, Guid id, ShaderProgram resource, bool updating)
    {
        if (updating) {
            Uninitialize(context, id, resource);
        }
        var cmd = InitializeCommand.Create();
        cmd.ShaderProgramId = id;
        cmd.Resource = resource;
        context.SendCommand<GraphicsResourceTarget>(cmd);
    } 

    protected override void Uninitialize(IContext context, Guid id, ShaderProgram resource)
    {
        var cmd = UninitializeCommand.Create();
        cmd.ShaderProgramId = id;
        context.SendCommand<GraphicsResourceTarget>(cmd);
    }

    private static uint BindUniformBlock(ProgramHandle program, string name, UniformBlockBinding binding)
    {
        var index = GL.GetUniformBlockIndex(program, name);
        if (index != uint.MaxValue) {
            GL.UniformBlockBinding(program, index, (uint)binding);
        }
        return index;
    }

    private static ShaderHandle CompileShader(Nagule.Graphics.ShaderType type, string source)
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

    private static global::OpenTK.Graphics.OpenGL.ShaderType ToGLShaderType(Nagule.Graphics.ShaderType type)
        => type switch {
            ShaderType.Fragment => GLShaderType.FragmentShader,
            ShaderType.Vertex => GLShaderType.VertexShader,
            ShaderType.Geometry => GLShaderType.GeometryShader,
            ShaderType.Compute => GLShaderType.ComputeShader,
            ShaderType.TessellationEvaluation => GLShaderType.TessEvaluationShader,
            ShaderType.TessellationControl => GLShaderType.TessControlShader,
            _ => throw new NotSupportedException("Unknown shader type: " + type)
        };

    private static string Desugar(string source)
        => Regex.Replace(source, "(\\#include \\<(?<file>.+)\\>)", match => {
            var filePath = match.Groups["file"].Value;
            if (!s_internalShaderFiles.TryGetValue(filePath, out var result)) {
                Console.WriteLine("Shader file not found: " + match.Value);
                return "";
            }
            return Desugar(result);
        });
}