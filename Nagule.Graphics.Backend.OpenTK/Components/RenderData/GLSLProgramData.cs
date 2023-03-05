namespace Nagule.Graphics.Backend.OpenTK;

using Aeco;

using Nagule.Graphics;

public struct BlockIndices
{
    public uint? PipelineBlock;
    public uint? LightingEnvBlock;
    public uint? CameraBlock;
    public uint? MaterialBlock;
    public uint? MeshBlock;
    public uint? ObjectBlock;
}

public enum UniformBlockBinding : uint
{
    Pipeline = 1,
    LightingEnv,
    Camera,
    Material,
    Mesh,
    Object
}

public record struct ShaderCacheKey(ShaderType Type, string Source, string Macros);
public record struct ShaderCacheValue(ShaderHandle Handle, int RefCount);

public struct GLSLProgramData : IHashComponent
{
    public record struct ParameterEntry(ShaderParameterType Type, int Offset);

    public ShaderCacheKey[] ShaderCacheKeys;
    public ProgramHandle Handle;
    public BlockIndices BlockIndices;
    public int MaterialBlockSize;

    public EnumArray<ShaderType, Dictionary<string, uint>>? SubroutineIndices;
    public Dictionary<string, ParameterEntry>? Parameters;
    public Dictionary<string, int>? TextureLocations;

    public int LightsBufferLocation;
    public int ClustersBufferLocation;
    public int ClusterLightCountsBufferLocation;
}