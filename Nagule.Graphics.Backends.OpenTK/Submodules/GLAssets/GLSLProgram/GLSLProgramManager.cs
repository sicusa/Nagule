namespace Nagule.Graphics.Backends.OpenTK;

using System.Text;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using System.Collections.Immutable;
using Microsoft.Extensions.Logging;

using Sia;
using CommunityToolkit.HighPerformance;
using System.Collections.Frozen;

public partial class GLSLProgramManager
{
    private record struct ShaderCache(ShaderHandle Handle, int RefCount);

    private Dictionary<ShaderCacheKey, ShaderCache> _shaderCache = [];
    public static readonly string EmptyFragmentShader = "#version 410 core\nvoid main() { }";

    private static readonly Regex s_compilationResultRegex = CreateCompilationResultRegex();
    private static readonly Regex s_commentRegex = CreateCommentRegex();
    private static readonly Regex s_includeCoreRegex = CreateIncludeCoreRegex();
    private static readonly Regex s_includeLocalRegex = CreateIncludeLocalRegex();
    private static readonly Regex s_propertiesRegex = CreatePropertiesRegex();
    private static readonly Regex s_propertyRegex = CreatePropertyRegex();

    private static readonly int ShaderTypeCount = Enum.GetNames<ShaderType>().Length;

    protected override void LoadAsset(EntityRef entity, ref GLSLProgram asset, EntityRef stateEntity)
    {
        var name = asset.Name;
        var shaders = asset.Shaders;
        var macros = asset.Macros;
        var macrosKey = string.Join(',', asset.Macros.Order());
        var feedbacks = asset.Feedbacks;
        var parameters = asset.Parameters;
        var subroutines = asset.Subroutines;

        RenderFramer.Enqueue(entity, () => {
            ref var state = ref stateEntity.Get<GLSLProgramState>();
            var program = GL.CreateProgram();

            Span<ShaderHandle> shaderHandles = stackalloc ShaderHandle[ShaderTypeCount];
            state.ShaderCacheKeys = new ShaderCacheKey[shaders.Count];
            int cachedCount = 0;

            foreach (var (type, source) in shaders) {
                var cacheKey = new ShaderCacheKey(type, source, macrosKey);

                try {
                    ref var cache = ref ReferenceShaderCache(cacheKey, name ?? "(null)", macros);
                    GL.AttachShader(program, cache.Handle.Handle);
                    shaderHandles[(int)type] = cache.Handle;
                }
                catch (Exception e) {
                    Logger.LogError("[{Name}] Failed to load shader: {Exception}", name, e);
                    continue;
                }

                state.ShaderCacheKeys[cachedCount] = cacheKey;
                cachedCount++;
            }

            // set feedbacks

            if (feedbacks.Count != 0) {
                GL.TransformFeedbackVaryings(program,
                    feedbacks.Count, feedbacks.ToArray(), TransformFeedbackBufferMode.InterleavedAttribs);
            }

            // link program

            int success = 0;
            GL.LinkProgram(program);
            GL.GetProgrami(program, ProgramPropertyARB.LinkStatus, ref success);

            if (success == 0) {
                GL.GetProgramInfoLog(program, out var infoLog);
                GL.DeleteProgram(program);

                Logger.LogError("[{Name}] Failed to link program: {infoLog}", name, infoLog);
                return;
            }

            // detach shaders

            state.BlockIndices = new BlockIndices {
                PipelineBlock = BindUniformBlock(program, "Pipeline", UniformBlockBinding.Pipeline),
                LightClustersBlock = BindUniformBlock(program, "LightClusters", UniformBlockBinding.LightClusters),
                CameraBlock = BindUniformBlock(program, "Camera", UniformBlockBinding.Camera),
                MaterialBlock = BindUniformBlock(program, "Material", UniformBlockBinding.Material),
                MeshBlock = BindUniformBlock(program, "Mesh", UniformBlockBinding.Mesh)
            };

            // initialize parameters

            if (parameters.Count != 0) {
                Dictionary<string, ShaderParameterEntry>? entries = null;
                Dictionary<string, int>? texLocations = null;

                InitializeParameters(
                    ref state, program, state.MaterialBlockSize, parameters,
                    ref entries, ref texLocations);

                state.Parameters = entries?.ToFrozenDictionary();
                state.TextureLocations = texLocations?.ToFrozenDictionary();
            }

            // get subroutine indices

            if (subroutines.Count != 0) {
                state.SubroutineIndices = new();
                var subroutineIndices = state.SubroutineIndices;

                foreach (var (shaderType, subroutineNames) in subroutines) {
                    var indices = subroutineIndices[shaderType];
                    if (indices == null) {
                        indices = [];
                        subroutineIndices[shaderType] = indices;
                    }
                    foreach (var subroutineName in subroutineNames) {
                        var index = GL.GetSubroutineIndex(program, ToGLShaderType(shaderType), subroutineName);
                        if (index == uint.MaxValue) {
                            Logger.LogWarning("[{Name}] Subroutine '{Subroutine}' not found.", name, subroutineName);
                            continue;
                        }
                        indices.Add(subroutineName, index);
                    }
                }
            }

            // get buffer locations

            state.LightsBufferLocation = GL.GetUniformLocation(program, "LightsBuffer");
            state.ClustersBufferLocation = GL.GetUniformLocation(program, "ClustersBuffer");
            state.ClusterLightCountsBufferLocation = GL.GetUniformLocation(program, "ClusterLightCountsBuffer");

            state.Handle = new(program);
        });
    }

    protected override void UnloadAsset(EntityRef entity, ref GLSLProgram asset, EntityRef stateEntity)
    {
        RenderFramer.Enqueue(entity, () => {
            ref var state = ref stateEntity.Get<GLSLProgramState>();
            if (!state.Loaded) {
                return;
            }
            GL.DeleteProgram(state.Handle.Handle);
            foreach (ref var key in state.ShaderCacheKeys.AsSpan()) {
                UnreferenceShaderCache(key);
            }
        });
    }

    private ref ShaderCache ReferenceShaderCache(in ShaderCacheKey key, string name, ImmutableHashSet<string> macros)
    {
        ref var shaderCache = ref CollectionsMarshal.GetValueRefOrAddDefault(
            _shaderCache, key, out bool exists);

        if (exists) {
            shaderCache.RefCount++;
            return ref shaderCache;
        }

        shaderCache.Handle = CompileShader(name, key.Type, key.Source, macros);
        shaderCache.RefCount = 1;
        return ref shaderCache;
    }

    private bool UnreferenceShaderCache(in ShaderCacheKey key)
    {
        ref var shaderCache = ref CollectionsMarshal.GetValueRefOrAddDefault(
            _shaderCache, key, out bool exists);

        if (!exists) {
            _shaderCache.Remove(key);
            return false;
        }

        if (shaderCache.RefCount == 1) {
            _shaderCache.Remove(key);
            GL.DeleteShader(shaderCache.Handle.Handle);
        }
        shaderCache.RefCount--;
        return true;
    }

    private static uint? BindUniformBlock(int program, string name, UniformBlockBinding binding)
    {
        var index = GL.GetUniformBlockIndex(program, name);
        if (index == uint.MaxValue) {
            return null;
        }
        GL.UniformBlockBinding(program, index, (uint)binding);
        return index;
    }

    private ShaderHandle CompileShader(string name, ShaderType type, string source, ImmutableHashSet<string> macros)
    {
        var glShaderType = ToGLShaderType(type);
        var handle = GL.CreateShader(glShaderType);

        source = DesugarShader(name, source, macros);

        if (macros.Count != 0) {
            var macroDefs = "#define " + string.Join("\n#define ", macros) + '\n';
            int versionDirectiveIndex = source.IndexOf("#version");
            if (versionDirectiveIndex == -1) {
                source = macroDefs + source;
            }
            else {
                var nextLineIndex = source.IndexOf('\n', versionDirectiveIndex);
                source = string.Concat(
                    source.AsSpan(0, nextLineIndex + 1),
                    macroDefs, source.AsSpan(nextLineIndex + 1));
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

            using var reader = new StringReader(infoLog);

            while (reader.ReadLine() is string infoLine) {
                if (infoLine == "") {
                    continue;
                }

                var match = s_compilationResultRegex.Match(infoLine);
                if (match == null || match.Length == 0) {
                    Logger.LogInformation("[{Name} {Type}] {Message}", name, type, infoLine);
                    continue;
                }

                int lineIndex = int.Parse(match.Groups["line"].Value);
                var message = $"[{name} {type}] {infoLine}\n\t{sourceLines[lineIndex - 1]}";

                if (match.Groups["type"].Value != "ERROR") {
                    Logger.LogInformation("[{Name} {Type}] {Message}", name, type, message);
                }
                else {
                    Logger.LogError("[{Name} {Type}] {Message}", name, type, message);
                }
            }
        }

        return new(handle);
    }

    private string DesugarShader(string name, string source, ImmutableHashSet<string> macros, string? path = null)
    {
        // remove comments
        source = s_commentRegex.Replace(source, "$1");

        // #include <file>
        source = s_includeCoreRegex.Replace(source, match => {
            var filePath = match.Groups["file"].Value;
            string source;
            try {
                source = ShaderUtils.LoadCore(filePath.Replace('/', '.'));
            }
            catch (Exception e) {
                Logger.LogError("[{Name}] Shader '{Path}' cannot be loaded: {Message}", name, match.Value, e.Message);
                return "";
            }
            return DesugarShader(name, source, macros, filePath);
        });

        // #include "file"
        source = s_includeLocalRegex.Replace(source, match => {
            var filePath = Path.GetFullPath(Path.Join(Path.GetDirectoryName(path), match.Groups["file"].Value))
                [(Environment.CurrentDirectory.Length + 1)..];
            string source;
            try {
                source = ShaderUtils.LoadCore(filePath.Replace(Path.DirectorySeparatorChar, '.'));
            }
            catch (Exception e) {
                Logger.LogError("[{Name}] Shader '{Path}' cannot be loaded: {Message}", name, match.Value, e.Message);
                return "";
            }
            return DesugarShader(name, source, macros, filePath);
        });

        // properties { ... }
        source = s_propertiesRegex.Replace(source, match => {
            var propMatches = s_propertyRegex.Matches(match.Groups[1].Value);
            var props = new StringBuilder();
            var instanced_props = new StringBuilder();
            var consts = new StringBuilder();

            foreach (Match propMatch in propMatches.Cast<Match>()) {
                var propType = propMatch.Groups["type"];
                var propName = propMatch.Groups["name"];
                var defaultValue = propMatch.Groups["default"];

                if (propMatch.Groups["instanced"].Length != 0) {
                    props.Append(propType);
                    props.Append(' ');
                    props.Append(propName);
                    props.Append(";\n");

                    if (defaultValue.Length != 0) {
                        Logger.LogWarning("[{Name}] Instanced property '{Property}' cannot have default value.", name, propName);
                    }
                }
                else if (macros.Contains("_" + propName) || defaultValue.Length == 0) {
                    props.Append(propType);
                    props.Append(' ');
                    props.Append(propName);
                    props.Append(";\n");
                }
                else {
                    consts.Append("const ");
                    consts.Append(propType);
                    consts.Append(' ');
                    consts.Append(propName);
                    consts.Append(" = ");
                    consts.Append(defaultValue);
                    consts.Append(";\n");
                }
            }

            var result = new StringBuilder();
            if (props.Length != 0) {
                result.Append("uniform Material {\n");
                result.Append(props);
                result.Append("};\n");
            }
            if (instanced_props.Length != 0) {
                result.Append("struct NaguleInstance {\n");
                result.Append(instanced_props);
                result.Append("};\n");
                
            }
            result.Append(consts);
            return result.ToString();
        });

        return source;
    }

    private unsafe void InitializeParameters(
        ref GLSLProgramState state, int program, int blockSize,
        ImmutableDictionary<string, ShaderParameterType> parameters,
        ref Dictionary<string, ShaderParameterEntry>? entries,
        ref Dictionary<string, int>? texLocations)
    {
        List<string>? parNames = null;

        foreach (var (name, type) in parameters) {
            if (type == ShaderParameterType.Unit) {
                entries ??= [];
                entries.Add(name, new(type, -1));
            }
            else if (ShaderUtils.TextureRecordTypes.ContainsKey(type)) {
                var location = GL.GetUniformLocation(program, name);
                if (location != -1) {
                    texLocations ??= [];
                    texLocations.Add(name, location);
                    entries ??= [];
                    entries.Add(name, new(type, -1));
                }
            }
            else {
                parNames ??= [];
                parNames.Add(name);
            }
        }

        if (parNames == null || state.BlockIndices.MaterialBlock is not uint materialBlock) {
            return;
        }

        GL.GetActiveUniformBlocki(program, materialBlock,
            UniformBlockPName.UniformBlockDataSize, ref state.MaterialBlockSize);

        int parCount = parNames.Count;
        Span<uint> indices = stackalloc uint[parCount];
        Span<uint> validIndices = stackalloc uint[parCount];
        Span<int> validNameMap = stackalloc int[parCount];

        var nameSpan = parNames.AsSpan();
        using (UnsafeUtils.CreateRawStringArray(nameSpan, out var byteArr)) {
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
            validCount++;
        }

        Span<int> offsets = stackalloc int[validCount];
        Span<int> uniformBlockIndices = stackalloc int[validCount];

        validIndices = validIndices[..validCount];
        GL.GetActiveUniformsi(program, validIndices, UniformPName.UniformOffset, offsets);
        GL.GetActiveUniformsi(program, validIndices, UniformPName.UniformBlockIndex, uniformBlockIndices);

        for (int i = 0; i != validCount; ++i) {
            if (uniformBlockIndices[i] == materialBlock) {
                var name = nameSpan[validNameMap[i]];
                entries ??= [];
                entries.Add(name, new(parameters[name], offsets[i]));
            }
        }
    }

    private static GLShaderType ToGLShaderType(ShaderType type)
        => type switch {
            ShaderType.Fragment => GLShaderType.FragmentShader,
            ShaderType.Vertex => GLShaderType.VertexShader,
            ShaderType.Geometry => GLShaderType.GeometryShader,
            ShaderType.Compute => GLShaderType.ComputeShader,
            ShaderType.TessellationEvaluation => GLShaderType.TessEvaluationShader,
            ShaderType.TessellationControl => GLShaderType.TessControlShader,
            _ => throw new NotSupportedException("Unknown shader type: " + type)
        };

    [GeneratedRegex("(?<type>\\w+): (?<column>\\d+):(?<line>\\d+)")]
    private static partial Regex CreateCompilationResultRegex();

    [GeneratedRegex("(@(?:\"[^\"]*\")+|\"(?:[^\"\\n\\\\]+|\\\\.)*\"|'(?:[^'\\n\\\\]+|\\\\.)*')|//.*|/\\*(?s:.*?)\\*/")]
    private static partial Regex CreateCommentRegex();

    [GeneratedRegex("^\\s*\\#\\s*include\\s*\\<(?<file>\\S+)\\>", RegexOptions.Multiline)]
    private static partial Regex CreateIncludeCoreRegex();

    [GeneratedRegex("^\\s*\\#\\s*include\\s*\\\"(?<file>\\S+)\\\"", RegexOptions.Multiline)]
    private static partial Regex CreateIncludeLocalRegex();

    [GeneratedRegex("\\bproperties\\s*\\{([^}]*)\\}", RegexOptions.Singleline)]
    private static partial Regex CreatePropertiesRegex();

    [GeneratedRegex("\\G\\s*(?<instanced>instanced\\s+)?(?<type>\\w+)\\s+(?<name>\\w+)\\s*(=\\s*(?<default>[^;]+))?;", RegexOptions.Singleline)]
    private static partial Regex CreatePropertyRegex();
}