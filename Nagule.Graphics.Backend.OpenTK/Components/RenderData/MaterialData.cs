namespace Nagule.Graphics.Backend.OpenTK;

public struct MaterialData : IHashComponent
{
    public BufferHandle Handle;
    public IntPtr Pointer;
    public RenderMode RenderMode;
    public bool IsTwoSided;
    public Guid ColorShaderProgramId;
    public Guid DepthShaderProgramId;
    public Dictionary<string, Guid>? Textures;
}