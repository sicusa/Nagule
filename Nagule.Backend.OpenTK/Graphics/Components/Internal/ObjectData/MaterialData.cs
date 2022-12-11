namespace Nagule.Backend.OpenTK.Graphics;

using global::OpenTK.Graphics;

using Aeco;

using Nagule.Graphics;

public struct MaterialData : Nagule.IPooledComponent
{
    public BufferHandle Handle;
    public IntPtr Pointer;
    public Guid ShaderProgramId;
    public EnumArray<TextureType, Guid?> Textures;
    public bool IsTwoSided;
}