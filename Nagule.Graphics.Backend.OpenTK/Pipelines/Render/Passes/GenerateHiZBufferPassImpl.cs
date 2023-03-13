namespace Nagule.Graphics.Backend.OpenTK;

using System.Runtime.CompilerServices;

public class GenerateHiZBufferPassImpl : RenderPassImplBase
{
    private uint _programId;

    private static GLSLProgram s_program =
        new GLSLProgram {
            Name = "nagule.pipeline.hiz"
        }
        .WithShaders(
            new(ShaderType.Vertex,
                GraphicsHelper.LoadEmbededShader("nagule.common.quad.vert.glsl")),
            new(ShaderType.Fragment,
                GraphicsHelper.LoadEmbededShader("nagule.pipeline.hiz.frag.glsl")))
        .WithParameter("LastMip", ShaderParameterType.Texture2D);

    public override void LoadResources(IContext context)
    {
        _programId = context.GetResourceLibrary().Reference(Id, s_program);
    }

    public override void Initialize(ICommandHost host, IRenderPipeline pipeline)
    {
        ref var buffer = ref pipeline.Acquire<HiearchicalZBuffer>(Id);

        buffer.Width = 512;
        buffer.Height = 256;

        buffer.TextureHandle = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2d, buffer.TextureHandle);
        GL.TexImage2D(TextureTarget.Texture2d, 0, InternalFormat.DepthComponent24, buffer.Width, buffer.Height, 0, GLPixelFormat.DepthComponent, GLPixelType.UnsignedInt, IntPtr.Zero);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapS, (int)GLTextureWrapMode.ClampToEdge);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapT, (int)GLTextureWrapMode.ClampToEdge);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, (int)GLTextureMagFilter.Nearest);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, (int)GLTextureMinFilter.NearestMipmapNearest);
        GL.GenerateMipmap(TextureTarget.Texture2d);
    }

    public override void Uninitialize(ICommandHost host, IRenderPipeline pipeline)
    {
        if (!pipeline.Remove<HiearchicalZBuffer>(Id, out var buffer)) {
            return;
        }
        GL.DeleteTexture(buffer.TextureHandle);
    }

    public override void Execute(
        ICommandHost host, IRenderPipeline pipeline, uint cameraId, MeshGroup meshGroup)
    {
        ref var hizProgram = ref host.RequireOrNullRef<GLSLProgramData>(_programId);
        if (Unsafe.IsNullRef(ref hizProgram)) { return; }
        ref var buffer = ref pipeline.Require<HiearchicalZBuffer>(Id);

        GL.UseProgram(hizProgram.Handle);

        GL.ColorMask(false, false, false, false);
        GL.DepthFunc(DepthFunction.Always);

        // downsample depth buffer to hi-Z buffer

        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2d, pipeline.EnsureDepthTexture());

        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureBaseLevel, 0);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMaxLevel, 0);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer,
            FramebufferAttachment.DepthAttachment, TextureTarget.Texture2d, buffer.TextureHandle, 0);
        
        GL.Viewport(0, 0, buffer.Width, buffer.Height);
        GL.DrawArrays(GLPrimitiveType.TriangleStrip, 0, 4);

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
            GL.DrawArrays(GLPrimitiveType.TriangleStrip, 0, 4);
        }

        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureBaseLevel, 0);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMaxLevel, levelCount - 1);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer,
            FramebufferAttachment.DepthAttachment, TextureTarget.Texture2d, pipeline.EnsureDepthTexture(), 0);

        GL.DepthFunc(DepthFunction.Lequal);
        GL.ColorMask(true, true, true, true);
        GL.Viewport(0, 0, pipeline.Width, pipeline.Height);
    }
}