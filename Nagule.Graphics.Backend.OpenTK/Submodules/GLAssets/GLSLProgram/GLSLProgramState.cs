namespace Nagule.Graphics.Backend.OpenTK;

public struct BlockIndices
{
    public uint? PipelineBlock;
    public uint? LightClustersBlock;
    public uint? CameraBlock;
    public uint? MaterialBlock;
    public uint? MeshBlock;
    public uint? InstancesBlock;
}

public enum UniformBlockBinding : uint
{
    Pipeline = 1,
    LightClusters,
    Camera,
    Material,
    Mesh,
    Instances
}

public record struct ShaderParameterEntry(ShaderParameterType Type, int Offset);
public record struct ShaderCacheKey(ShaderType Type, string Source, string Macros);

public record struct GLSLProgramState : IAssetState
{
    public readonly bool Loaded => Handle != ProgramHandle.Zero;

    public ShaderCacheKey[] ShaderCacheKeys;
    public ProgramHandle Handle;
    public BlockIndices BlockIndices;

    public int MaterialBlockSize;
    public Dictionary<string, ShaderParameterEntry>? Parameters;

    public EnumDictionary<ShaderType, Dictionary<string, uint>>? SubroutineIndices;
    public Dictionary<string, int>? TextureLocations;

    public int LightsBufferLocation;
    public int ClustersBufferLocation;
    public int ClusterLightCountsBufferLocation;

    public readonly int EnableBuiltInBuffers()
    {
        if (LightsBufferLocation != -1) {
            GL.Uniform1i(LightsBufferLocation, 1);
        }
        if (ClustersBufferLocation != -1) {
            GL.Uniform1i(ClustersBufferLocation, 2);
        }
        if (ClusterLightCountsBufferLocation != -1) {
            GL.Uniform1i(ClusterLightCountsBufferLocation, 3);
        }
        return 4;
    }
}