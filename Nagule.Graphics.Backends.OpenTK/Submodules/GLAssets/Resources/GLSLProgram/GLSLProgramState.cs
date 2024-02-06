namespace Nagule.Graphics.Backends.OpenTK;

using System.Collections.Frozen;

public struct BlockIndices
{
    public uint? PipelineBlock;
    public uint? LightClustersBlock;
    public uint? ShadowMapLibraryBlock;
    public uint? CameraBlock;
    public uint? MaterialBlock;
    public uint? MeshBlock;
    public uint? InstancesBlock;
}

public enum UniformBlockBinding : uint
{
    Pipeline = 1,
    LightClusters,
    ShadowMapLibrary,
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
    public FrozenDictionary<string, ShaderParameterEntry>? Parameters;

    public EnumDictionary<ShaderType, Dictionary<string, uint>>? SubroutineIndices;
    public FrozenDictionary<string, int>? TextureLocations;

    public int LightsBufferLocation;
    public int ClustersBufferLocation;
    public int ClusterLightCountsBufferLocation;
    public int ShadowMapTilesetLocation;

    public readonly uint EnableInternalBuffers()
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
        if (ShadowMapTilesetLocation != -1) {
            GL.Uniform1i(ShadowMapTilesetLocation, 4);
        }
        return 5;
    }
}