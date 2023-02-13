namespace Nagule.Graphics.Backend.OpenTK;

public struct RenderTextureData : IPooledComponent
{
    public int Width;
    public int Height;
    public FramebufferHandle FramebufferHandle;
}