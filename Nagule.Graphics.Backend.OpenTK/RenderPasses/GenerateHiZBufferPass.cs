namespace Nagule.Graphics.Backend.OpenTK;

using global::OpenTK.Graphics.OpenGL;

using GLPixelFormat = global::OpenTK.Graphics.OpenGL.PixelFormat;
using GLPixelType = global::OpenTK.Graphics.OpenGL.PixelType;

public class GenerateHiZBufferPass : IRenderPass
{
    private Guid _id = Guid.NewGuid();

    public void Initialize(ICommandHost host, IRenderPipeline pipeline)
    {
        ref var buffer = ref pipeline.Acquire<HiearchicalZBuffer>(_id);

        buffer.Width = 512;
        buffer.Height = 256;

        buffer.TextureHandle = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2d, buffer.TextureHandle);
        GL.TexImage2D(TextureTarget.Texture2d, 0, InternalFormat.DepthComponent24, buffer.Width, buffer.Height, 0, GLPixelFormat.DepthComponent, GLPixelType.UnsignedInt, IntPtr.Zero);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.NearestMipmapNearest);
        GL.GenerateMipmap(TextureTarget.Texture2d);
    }

    public void Uninitialize(ICommandHost host, IRenderPipeline pipeline)
    {
        if (!pipeline.Remove<HiearchicalZBuffer>(_id, out var buffer)) {
            return;
        }
        GL.DeleteTexture(buffer.TextureHandle);
    }

    public void Render(ICommandHost host, IRenderPipeline pipeline, MeshGroup meshGroup)
    {
        ref var buffer = ref pipeline.Require<HiearchicalZBuffer>(_id);
        GLHelper.GenerateHiZBuffer(host, pipeline, in buffer);
    }
}