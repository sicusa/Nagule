namespace Nagule.Graphics.Backend.OpenTK;

public struct RenderTextureData : IHashComponent
{
    public int Width;
    public int Height;
    public FramebufferHandle FramebufferHandle;
    public TextureHandle TextureHandle;
}