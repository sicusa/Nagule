namespace Nagule.Graphics.Backend.OpenTK.Graphics;

using System.Collections.Immutable;

using global::OpenTK.Graphics;

using Aeco;

using Nagule.Graphics;

public struct BlockLocations
{
    public uint FramebufferBlock;
    public uint CameraBlock;
    public uint MaterialBlock;
    public uint MeshBlock;
    public uint ObjectBlock;
    public uint LightingEnvBlock;
}

public enum UniformBlockBinding : uint
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
    public ProgramHandle Handle;
    public BlockLocations BlockLocations;
    public EnumArray<TextureType, int>? TextureLocations;
    public EnumArray<ShaderType, ImmutableDictionary<string, uint>>? SubroutineIndeces;
    public ImmutableDictionary<string, int> CustomLocations;

    public int DepthBufferLocation;
    public int LightsBufferLocation;
    public int ClustersBufferLocation;
    public int ClusterLightCountsBufferLocation;
}