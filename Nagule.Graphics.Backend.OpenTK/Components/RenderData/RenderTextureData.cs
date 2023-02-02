namespace Nagule.Graphics.Backend.OpenTK;

using global::OpenTK.Graphics;

public struct RenderTextureData : IPooledComponent
{
    public int Width;
    public int Height;
    public FramebufferHandle FramebufferHandle;
}