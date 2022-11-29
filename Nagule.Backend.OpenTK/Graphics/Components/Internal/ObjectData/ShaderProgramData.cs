namespace Nagule.Backend.OpenTK.Graphics;

using System.Collections.Immutable;

using Aeco;

using Nagule.Graphics;

public struct BlockLocations
{
    public int FramebufferBlock;
    public int CameraBlock;
    public int MaterialBlock;
    public int MeshBlock;
    public int ObjectBlock;
    public int LightingEnvBlock;
}

public enum UniformBlockBinding
{
    Framebuffer = 1,
    Camera,
    Material,
    Mesh,
    Object,
    LightingEnv
}

public struct ShaderProgramData : Nagule.IPooledComponent
{
    public int Handle;
    public BlockLocations BlockLocations;
    public EnumArray<TextureType, int>? TextureLocations;
    public EnumArray<ShaderType, ImmutableDictionary<string, int>>? SubroutineIndeces;
    public ImmutableDictionary<string, int> CustomLocations;

    public int DepthBufferLocation;
    public int LightsBufferLocation;
    public int ClustersBufferLocation;
    public int ClusterLightCountsBufferLocation;
}