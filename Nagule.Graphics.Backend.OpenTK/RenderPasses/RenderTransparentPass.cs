namespace Nagule.Graphics.Backend.OpenTK;

using System.Runtime.CompilerServices;

using global::OpenTK.Graphics.OpenGL;

using GLPixelFormat = global::OpenTK.Graphics.OpenGL.PixelFormat;
using GLPixelType = global::OpenTK.Graphics.OpenGL.PixelType;

public class RenderTransparentPass : IRenderPass
{
    public required MeshFilter MeshFilter { get; init; }

    private Guid _id = Guid.NewGuid();

    private static DrawBufferMode[] s_transparentDrawModes = {
        DrawBufferMode.ColorAttachment0, DrawBufferMode.ColorAttachment1
    };

    public void Initialize(ICommandHost host, IRenderPipeline pipeline)
    {
        ref var buffer = ref pipeline.Acquire<TransparencyFramebuffer>(_id);
        buffer.FramebufferHandle = GL.GenFramebuffer();

        CreateTextures(host, pipeline, ref buffer);
        pipeline.OnResize += OnResize;
    }

    public void Uninitialize(ICommandHost host, IRenderPipeline pipeline)
    {
        pipeline.OnResize -= OnResize;

        if (!pipeline.Remove<TransparencyFramebuffer>(_id, out var buffer)) {
            return;
        }
        GL.DeleteTexture(buffer.AccumTextureHandle);
        GL.DeleteTexture(buffer.RevealTextureHandle);
        GL.DeleteFramebuffer(buffer.FramebufferHandle);
    }

    public void OnResize(ICommandHost host, IRenderPipeline pipeline)
    {
        ref var buffer = ref pipeline.Acquire<TransparencyFramebuffer>(_id);
        GL.DeleteTexture(buffer.AccumTextureHandle);
        GL.DeleteTexture(buffer.RevealTextureHandle);
        CreateTextures(host, pipeline, ref buffer);
    }

    private void CreateTextures(ICommandHost host, IRenderPipeline pipeline, ref TransparencyFramebuffer buffer)
    {
        int width = pipeline.Width;
        int height = pipeline.Height;

        buffer.AccumTextureHandle = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2d, buffer.AccumTextureHandle);
        GL.TexImage2D(TextureTarget.Texture2d, 0, InternalFormat.Rgba16f, width, height, 0, GLPixelFormat.Rgba, GLPixelType.HalfFloat, IntPtr.Zero);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);

        buffer.RevealTextureHandle = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2d, buffer.RevealTextureHandle);
        GL.TexImage2D(TextureTarget.Texture2d, 0, InternalFormat.R16f, width, height, 0, GLPixelFormat.Red, GLPixelType.HalfFloat, IntPtr.Zero);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
        GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, buffer.FramebufferHandle);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2d, buffer.AccumTextureHandle, 0);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment1, TextureTarget.Texture2d, buffer.RevealTextureHandle, 0);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment, TextureTarget.Texture2d, pipeline.DepthTextureHandle, 0);
        GL.DrawBuffers(s_transparentDrawModes);
    }

    public void Render(ICommandHost host, IRenderPipeline pipeline, MeshGroup meshGroup)
    {
        var meshIds = meshGroup.GetMeshIds(MeshFilter);
        if (meshIds.Length == 0) { return; }

        ref var composeProgram = ref host.RequireOrNullRef<GLSLProgramData>(Graphics.TransparencyComposeShaderProgramId);
        if (Unsafe.IsNullRef(ref composeProgram)) { return; }

        ref var buffer = ref pipeline.Require<TransparencyFramebuffer>(_id);

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, buffer.FramebufferHandle);
        GL.ClearColor(0, 0, 0, 1);
        GL.Clear(ClearBufferMask.ColorBufferBit);

        GL.DepthMask(false);
        GL.Enable(EnableCap.Blend);
        GL.BlendFuncSeparate(BlendingFactor.One, BlendingFactor.One, BlendingFactor.Zero, BlendingFactor.OneMinusSrcAlpha);

        foreach (var id in meshIds) {
            ref readonly var meshData = ref host.Inspect<MeshData>(id);
            GLHelper.Draw(host, id, in meshData);
        }

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, pipeline.FramebufferHandle);

        // compose transparency

        GL.UseProgram(composeProgram.Handle);
        GL.BlendFunc(BlendingFactor.One, BlendingFactor.OneMinusSrcAlpha);
        GL.DepthFunc(DepthFunction.Always);

        GL.ActiveTexture(TextureUnit.Texture0 + GLHelper.BuiltInBufferCount);
        GL.BindTexture(TextureTarget.Texture2d, buffer.AccumTextureHandle);
        GL.Uniform1i(composeProgram.TextureLocations!["AccumTex"], GLHelper.BuiltInBufferCount);

        GL.ActiveTexture(TextureUnit.Texture0 + GLHelper.BuiltInBufferCount + 1);
        GL.BindTexture(TextureTarget.Texture2d, buffer.RevealTextureHandle);
        GL.Uniform1i(composeProgram.TextureLocations["RevealTex"], GLHelper.BuiltInBufferCount + 1);

        GL.DrawArrays(PrimitiveType.TriangleStrip, 0, 4);

        GL.DepthFunc(DepthFunction.Lequal);
        GL.DepthMask(true);
        GL.Disable(EnableCap.Blend);
    }
}