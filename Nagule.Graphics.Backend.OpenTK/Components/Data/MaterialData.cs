namespace Nagule.Graphics.Backend.OpenTK;

public struct MaterialData : IHashComponent
{
    public BufferHandle Handle;
    public IntPtr Pointer;
    public RenderMode RenderMode;
    public bool IsTwoSided;
    public uint ColorShaderProgramId;
    public uint DepthShaderProgramId;
    public Dictionary<string, uint>? Textures;
}