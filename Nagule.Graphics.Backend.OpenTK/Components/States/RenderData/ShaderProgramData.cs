namespace Nagule.Graphics.Backend.OpenTK;

using System.Collections.Immutable;

using global::OpenTK.Graphics;

using Aeco;

using Nagule.Graphics;

public struct BlockLocations
{
    public uint PipelineBlock;
    public uint CameraBlock;
    public uint MaterialBlock;
    public uint MeshBlock;
    public uint ObjectBlock;
    public uint LightingEnvBlock;
}

public enum UniformBlockBinding : uint
{
    Pipeline = 1,
    Camera,
    Material,
    Mesh,
    Object,
    LightingEnv
}

public struct ShaderProgramData : Nagule.IPooledComponent
{
    public ProgramHandle Handle;
    public BlockLocations BlockLocations;
    public EnumArray<TextureType, int>? TextureLocations;
    public EnumArray<ShaderType, ImmutableDictionary<string, uint>>? SubroutineIndices;
    public ImmutableDictionary<string, (ShaderParameterType Type, int Location)> CustomParameters;

    public int DepthBufferLocation;
    public int LightsBufferLocation;
    public int ClustersBufferLocation;
    public int ClusterLightCountsBufferLocation;
}