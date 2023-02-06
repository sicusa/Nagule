namespace Nagule.Graphics.Backend.OpenTK;

using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;

using global::OpenTK.Graphics;
using global::OpenTK.Graphics.OpenGL;

using Aeco;
using Nagule.Graphics;

using ShaderType = ShaderType;
using GLShaderType = global::OpenTK.Graphics.OpenGL.ShaderType;

public class GLSLProgramManager : ResourceManagerBase<GLSLProgram>
{
    private class InitializeCommand : Command<InitializeCommand, GraphicsResourceTarget>
    {
        public Guid ShaderProgramId;
        public GLSLProgram? Resource;
        public CancellationToken Token = default;

        private EnumArray<ShaderType, ShaderHandle> _shaderHandles = new();
        private List<string> _parNames = new();

        public unsafe override void Execute(ICommandHost host)
        {
            var data = new GLSLProgramData();
            var shaders = Resource!.Shaders;

            // create program

            var program = GL.CreateProgram();
            data.Handle = program;
            Array.Clear(_shaderHandles.Raw);

            var macros = String.Join(",", Resource.Macros.Order());
            data.ShaderCacheKeys = new ShaderCacheKey[Resource.Shaders.Count];

            lock (s_shaderCache) {
                int n = 0;
                foreach (var (type, source) in Resource.Shaders) {
                    var cacheKey = new ShaderCacheKey(type, source, macros);

                    try {
                        ref var cache = ref ReferenceShaderCache(Resource, in cacheKey);
                        GL.AttachShader(program, cache.Handle);
                        _shaderHandles[type] = cache.Handle;
                    }
                    catch {
                        for (int i = 0; i < n; ++i) {
                            UnreferenceShaderCache(data.ShaderCacheKeys[i]);
                        }
                        throw;
                    }

                    data.ShaderCacheKeys[n] = cacheKey;
                    ++n;
                }
            }

            // set feedbacks

            if (Resource.Feedbacks != null) {
                var feedbacks = Resource.Feedbacks;
                using (UnsafeHelper.CreateRawStringArray(feedbacks, out var rawArr)) {
                    GL.TransformFeedbackVaryings(program,
                        feedbacks.Count, rawArr,
                        TransformFeedbackBufferMode.InterleavedAttribs);
                }
            }

            // link program

            int success = 0;
            GL.LinkProgram(program);
            GL.GetProgrami(program, ProgramPropertyARB.LinkStatus, ref success);

            if (success == 0) {
                GL.GetProgramInfoLog(program, out var infoLog);
                throw new GLSLProgramLinkFailedException($"[{Resource.Name}] " + infoLog);
            }

            // detach shaders

            foreach (var handle in _shaderHandles.Raw) {
                if (handle != ShaderHandle.Zero) {
                    GL.DetachShader(program, handle);
                }
            }

            // get block locations

            data.BlockIndices = new BlockIndices {
                PipelineBlock = BindUniformBlock(program, "Pipeline", UniformBlockBinding.Pipeline),
                LightingEnvBlock = BindUniformBlock(program, "LightingEnv", UniformBlockBinding.LightingEnv),
                CameraBlock = BindUniformBlock(program, "Camera", UniformBlockBinding.Camera),
                MaterialBlock = BindUniformBlock(program, "Material", UniformBlockBinding.Material),
                MeshBlock = BindUniformBlock(program, "Mesh", UniformBlockBinding.Mesh),
                ObjectBlock = BindUniformBlock(program, "Object", UniformBlockBinding.Object)
            };

            // initialize parameters

            if (Resource.Parameters.Count != 0) {
                InitializeParameters(ref data, program);
            }

            // get subroutine indices

            if (Resource.Subroutines.Count != 0) {
                data.SubroutineIndices = new();
                var subroutineIndices = data.SubroutineIndices;

                foreach (var (shaderType, names) in Resource.Subroutines) {
                    var indices = subroutineIndices[shaderType];
                    if (indices == null) {
                        indices = new();
                        subroutineIndices[shaderType] = indices;
                    }
                    foreach (var name in names) {
                        var index = GL.GetSubroutineIndex(program, ToGLShaderType(shaderType), name);
                        if (index == uint.MaxValue) {
                            Console.WriteLine($"[GLSLProgram {Resource.Name}] Subroutine index '{name}' not found");
                            continue;
                        }
                        indices.Add(name, index);
                    }
                }
            }

            // get buffer locations

            data.DepthBufferLocation = GL.GetUniformLocation(program, "DepthBuffer");
            data.LightsBufferLocation = GL.GetUniformLocation(program, "LightsBuffer");
            data.ClustersBufferLocation = GL.GetUniformLocation(program, "ClustersBuffer");
            data.ClusterLightCountsBufferLocation = GL.GetUniformLocation(program, "ClusterLightCountsBuffer");

            // finish initialization

            host.SendRenderData(ShaderProgramId, data, Token,
                (id, data) => GL.DeleteProgram(data.Handle));
        }

        private unsafe void InitializeParameters(ref GLSLProgramData data, ProgramHandle program)
        {
            var pars = Resource!.Parameters;
            data.Parameters = new();
            var parEntries = data.Parameters;

            foreach (var (name, type) in pars) {
                switch (type) {
                case ShaderParameterType.Unit:
                    parEntries.Add(name, new(type, -1));
                    continue;
                case ShaderParameterType.Texture:
                    var location = GL.GetUniformLocation(program, name);
                    if (location != -1) {
                        data.TextureLocations ??= new();
                        data.TextureLocations.Add(name, location);
                        parEntries.Add(name, new(type, -1));
                    }
                    continue;
                default:
                    _parNames.Add(name);
                    break;
                }
            }

            if (_parNames.Count == 0
                    || data.BlockIndices.MaterialBlock is not uint blockIndex) {
                return;
            }

            int parCount = _parNames.Count;

            Span<uint> indices = stackalloc uint[parCount];
            Span<uint> validIndices = stackalloc uint[parCount];
            Span<int> validNameMap = stackalloc int[parCount];

            var nameSpan = CollectionsMarshal.AsSpan(_parNames);
            using (UnsafeHelper.CreateRawStringArray(nameSpan, out var byteArr)) {
                GL.GetUniformIndices(program, parCount, byteArr, indices);
            }

            int validCount = 0;
            for (int i = 0; i != nameSpan.Length; ++i) {
                uint parIndex = indices[i];
                if (parIndex == uint.MaxValue) {
                    continue;
                }
                validIndices[validCount] = parIndex;
                validNameMap[validCount] = i;
                ++validCount;
            }

            GL.GetActiveUniformBlocki(program, blockIndex,
                UniformBlockPName.UniformBlockDataSize, ref data.MaterialBlockSize);

            Span<int> offsets = stackalloc int[validCount];
            Span<int> uniformBlockIndices = stackalloc int[validCount];

            validIndices = validIndices.Slice(0, validCount);
            GL.GetActiveUniformsi(program, validIndices, UniformPName.UniformOffset, offsets);
            GL.GetActiveUniformsi(program, validIndices, UniformPName.UniformBlockIndex, uniformBlockIndices);

            for (int i = 0; i != validCount; ++i) {
                if (uniformBlockIndices[i] == blockIndex) {
                    var name = nameSpan[validNameMap[i]];
                    parEntries.Add(name, new(pars[name], offsets[i]));
                }
            }

            _parNames.Clear();
        }
    }

    private class UninitializeCommand : Command<UninitializeCommand, RenderTarget>
    {
        public Guid ShaderProgramId;

        public override void Execute(ICommandHost host)
        {
            if (!host.Remove<GLSLProgramData>(ShaderProgramId, out var data)) {
                return;
            }

            GL.DeleteProgram(data.Handle);

            lock (s_shaderCache) {
                foreach (ref var key in data.ShaderCacheKeys.AsSpan()) {
                    UnreferenceShaderCache(in key);
                }
            }
        }
    }

    private static Dictionary<string, string> s_internalShaderFiles = new() {
        ["nagule/common.glsl"] = LoadShader("nagule.common.glsl"),
        ["nagule/noise.glsl"] = LoadShader("nagule.noise.glsl"),
        ["nagule/transparency.glsl"] = LoadShader("nagule.transparency.glsl"),
        ["nagule/lighting.glsl"] = LoadShader("nagule.lighting.glsl"),
        ["nagule/parallax_mapping.glsl"] = LoadShader("nagule.parallax_mapping.glsl")
    };

    private static Dictionary<ShaderCacheKey, ShaderCacheValue> s_shaderCache = new();

    protected override void Initialize(
        IContext context, Guid id, GLSLProgram resource, GLSLProgram? prevResource)
    {
        if (prevResource != null) {
            Uninitialize(context, id, prevResource);
        }
        var cmd = InitializeCommand.Create();
        cmd.ShaderProgramId = id;
        cmd.Resource = resource;
        cmd.Token = context.GetLifetimeToken(id);
        context.SendCommand<GraphicsResourceTarget>(cmd);
    } 

    protected override void Uninitialize(IContext context, Guid id, GLSLProgram resource)
    {
        var cmd = UninitializeCommand.Create();
        cmd.ShaderProgramId = id;
        context.SendCommand<GraphicsResourceTarget>(cmd);
    }

    private static ref ShaderCacheValue ReferenceShaderCache(GLSLProgram resource, in ShaderCacheKey key)
    {
        ref var shaderCache = ref CollectionsMarshal.GetValueRefOrAddDefault(
            s_shaderCache, key, out bool exists);

        if (exists) {
            ++shaderCache.RefCount;
            return ref shaderCache;
        }

        shaderCache.Handle = CompileShader(resource, key.Type, key.Source);
        shaderCache.RefCount = 1;
        return ref shaderCache;
    }

    private static void UnreferenceShaderCache(in ShaderCacheKey key)
    {
        ref var shaderCache = ref CollectionsMarshal.GetValueRefOrAddDefault(
            s_shaderCache, key, out bool exists);

        if (!exists) {
            Console.WriteLine("Internal error: shader cache not found");
            s_shaderCache.Remove(key);
            return;
        }
        if (shaderCache.RefCount == 1) {
            s_shaderCache.Remove(key);
            GL.DeleteShader(shaderCache.Handle);
            return;
        }
        --shaderCache.RefCount;
    }

    private static uint? BindUniformBlock(ProgramHandle program, string name, UniformBlockBinding binding)
    {
        var index = GL.GetUniformBlockIndex(program, name);
        if (index == uint.MaxValue) {
            return null;
        }
        GL.UniformBlockBinding(program, index, (uint)binding);
        return index;
    }

    private static string LoadShader(string resourceId)
        => EmbededAssets.LoadText(
            "Nagule.Graphics.Embeded.Shaders." + resourceId, typeof(Graphics).Assembly);

    private static ShaderHandle CompileShader(GLSLProgram program, ShaderType type, string source)
    {
        var glShaderType = ToGLShaderType(type);
        var handle = GL.CreateShader(glShaderType);

        source = DesugarShader(program, source);

        if (program.Macros.Count != 0) {
            var macroDefs = "#define " + string.Join("\n#define ", program.Macros) + '\n';
            int versionDirectiveIndex = source.IndexOf("#version");
            if (versionDirectiveIndex == -1) {
                source = macroDefs + source;
            }
            else {
                var nextLineIndex = source.IndexOf('\n', versionDirectiveIndex);
                source = source.Substring(0, nextLineIndex + 1) + macroDefs
                    + source.Substring(nextLineIndex + 1);
            }
        }

        GL.ShaderSource(handle, source);

        int status = 0;
        GL.CompileShader(handle);
        GL.GetShaderi(handle, ShaderParameterName.CompileStatus, ref status);

        if (status == 0) {
            GL.GetShaderInfoLog(handle, out string infoLog);
            GL.DeleteShader(handle);

            var sourceLines = source.Split(
                new[] {"\r\n", "\r", "\n"}, StringSplitOptions.None);

            using (var reader = new StringReader(infoLog)) {
                while (reader.ReadLine() is string infoLine) {
                    if (infoLine == "") {
                        continue;
                    }

                    var match = Regex.Match(infoLine, @"(?<type>\w+): (?<column>\d+):(?<line>\d+)");
                    if (match == null || match.Length == 0) {
                        Console.WriteLine($"[{program.Name} {type}] " + infoLine);
                        continue;
                    }

                    int columnIndex = int.Parse(match.Groups["column"].Value);
                    int lineIndex = int.Parse(match.Groups["line"].Value);
                    var message = $"[{program.Name} {type}] {infoLine}\n\t{sourceLines[lineIndex-1]}";

                    if (match.Groups["type"].Value != "ERROR") {
                        Console.WriteLine(message);
                    }
                    else {
                        throw new GLSLCompilationFailedException(message);
                    }
                }
            }
        }

        return handle;
    }

    private static string DesugarShader(GLSLProgram program, string source)
    {
        // remove comments
        source = Regex.Replace(source, @"(@(?:""[^""]*"")+|""(?:[^""\n\\]+|\\.)*""|'(?:[^'\n\\]+|\\.)*')|//.*|/\*(?s:.*?)\*/", "$1");

        // #include <file>
        source = Regex.Replace(source, @"^\s*\#\s*include\s*\<(?<file>\S+)\>", match => {
            var filePath = match.Groups["file"].Value;
            if (!s_internalShaderFiles.TryGetValue(filePath, out var result)) {
                Console.WriteLine($"[GLSLProgram {program.Name}] Shader file not found: " + match.Value);
                return "";
            }
            return DesugarShader(program, result);
        }, RegexOptions.Multiline);

        // properties { ... }
        source = Regex.Replace(source, @"\bproperties\s*\{([^}]*)\}", match => {
            var propMatches = Regex.Matches(match.Groups[1].Value,
                @"\G\s*(?<type>\w+)\s+(?<name>\w+)\s*(=\s*(?<default>[^;]+))?;",
                RegexOptions.Singleline);

            var props = new StringBuilder();
            var consts = new StringBuilder();

            foreach (Match propMatch in propMatches) {
                var type = propMatch.Groups["type"];
                var name = propMatch.Groups["name"];
                var defaultValue = propMatch.Groups["default"];

                if (program.Macros.Contains("_" + name) || defaultValue.Length == 0) {
                    props.Append(type);
                    props.Append(' ');
                    props.Append(name);
                    props.Append(";\n");
                }
                else {
                    consts.Append("const ");
                    consts.Append(type);
                    consts.Append(' ');
                    consts.Append(name);
                    consts.Append(" = ");
                    consts.Append(defaultValue);
                    consts.Append(";\n");
                }
            }

            if (props.Length != 0) {
                props.Append("};\n");
                return "uniform Material {\n" + props + consts;
            }
            else {
                return consts.ToString();
            }
        }, RegexOptions.Singleline);

        return source;
    }

    private static global::OpenTK.Graphics.OpenGL.ShaderType ToGLShaderType(ShaderType type)
        => type switch {
            ShaderType.Fragment => GLShaderType.FragmentShader,
            ShaderType.Vertex => GLShaderType.VertexShader,
            ShaderType.Geometry => GLShaderType.GeometryShader,
            ShaderType.Compute => GLShaderType.ComputeShader,
            ShaderType.TessellationEvaluation => GLShaderType.TessEvaluationShader,
            ShaderType.TessellationControl => GLShaderType.TessControlShader,
            _ => throw new NotSupportedException("Unknown shader type: " + type)
        };
}