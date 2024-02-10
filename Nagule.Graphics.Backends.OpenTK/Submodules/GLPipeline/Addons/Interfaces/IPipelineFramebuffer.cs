namespace Nagule.Graphics.Backends.OpenTK;

using Sia;

public interface IPipelineFramebuffer : IAddon
{
    FramebufferHandle Handle { get; }
    TextureHandle ColorAttachmentHandle { get; }
    TextureHandle DepthAttachmentHandle { get; }
    VertexArrayHandle EmptyVertexArray { get; }

    public int Width { get; }
    public int Height { get; }

    void Update(float time);
    void Resize(int width, int height);
    void SwapColorAttachments();
}