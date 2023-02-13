namespace Nagule.Graphics.Backend.OpenTK;

public struct MaterialData : IPooledComponent
{
    public BufferHandle Handle;
    public IntPtr Pointer;
    public bool IsTwoSided;
    public Guid ShaderProgramId;
    public Guid DepthShaderProgramId;
    public Dictionary<string, Guid>? Textures;
}