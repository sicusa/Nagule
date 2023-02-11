namespace Nagule.Graphics.Backend.OpenTK;

using System.Runtime.CompilerServices;

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
        ref var hizProgram = ref host.RequireOrNullRef<GLSLProgramData>(Graphics.HierarchicalZShaderProgramId);
        if (Unsafe.IsNullRef(ref hizProgram)) { return; }
        ref var buffer = ref pipeline.Require<HiearchicalZBuffer>(_id);

        GL.UseProgram(hizProgram.Handle);

        GL.ColorMask(false, false, false, false);
        GL.DepthFunc(DepthFunction.Always);

        // downsample depth buffer to hi-Z buffer

        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2d, pipeline.DepthTextureHandle);

        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureBaseLevel, 0);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMaxLevel, 0);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer,
            FramebufferAttachment.DepthAttachment, TextureTarget.Texture2d, buffer.TextureHandle, 0);
        
        GL.Viewport(0, 0, buffer.Width, buffer.Height);
        GL.DrawArrays(PrimitiveType.TriangleStrip, 0, 4);

        // generate hi-z buffer

        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2d, buffer.TextureHandle);

        int width = buffer.Width;
        int height = buffer.Height;
        int levelCount = 1 + (int)MathF.Floor(MathF.Log2(MathF.Max(width, height)));

        for (int i = 1; i < levelCount; ++i) {
            width /= 2;
            height /= 2;
            width = width > 0 ? width : 1;
            height = height > 0 ? height : 1;
            GL.Viewport(0, 0, width, height);

            GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureBaseLevel, i - 1);
            GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMaxLevel, i - 1);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer,
                FramebufferAttachment.DepthAttachment, TextureTarget.Texture2d, buffer.TextureHandle, i);
            GL.DrawArrays(PrimitiveType.TriangleStrip, 0, 4);
        }

        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureBaseLevel, 0);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMaxLevel, levelCount - 1);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer,
            FramebufferAttachment.DepthAttachment, TextureTarget.Texture2d, pipeline.DepthTextureHandle, 0);

        GL.DepthFunc(DepthFunction.Lequal);
        GL.ColorMask(true, true, true, true);
        GL.Viewport(0, 0, pipeline.Width, pipeline.Height);
    }
}