namespace Nagule.Graphics.Backend.OpenTK;

using global::OpenTK.Graphics;

public struct HiearchicalZBuffer : IRenderPipelineComponent
{
    public int Width;
    public int Height;
    public TextureHandle TextureHandle;
}