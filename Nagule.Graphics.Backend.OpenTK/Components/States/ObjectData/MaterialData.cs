namespace Nagule.Graphics.Backend.OpenTK;

using global::OpenTK.Graphics;

using Aeco;

using Nagule.Graphics;

public struct MaterialData : IPooledComponent
{
    public BufferHandle Handle;
    public IntPtr Pointer;
    public Guid ShaderProgramId;
    public Guid DepthShaderProgramId;
    public EnumArray<TextureType, Guid?> Textures;
    public bool IsTwoSided;
}