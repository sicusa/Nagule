namespace Nagule.Backend.OpenTK.Graphics;

using Aeco;

using Nagule.Graphics;

public struct MaterialData : Nagule.IPooledComponent
{
    public int Handle;
    public IntPtr Pointer;
    public Guid ShaderProgramId;
    public EnumArray<TextureType, Guid?> Textures;
}