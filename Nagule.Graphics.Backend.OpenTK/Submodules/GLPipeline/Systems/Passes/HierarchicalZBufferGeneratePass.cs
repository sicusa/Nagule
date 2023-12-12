namespace Nagule.Graphics.Backend.OpenTK;

using System.Runtime.CompilerServices;
using Sia;

public class HierarchicalZBufferGeneratePass : RenderPassSystemBase
{
    private static readonly GLSLProgramAsset s_hizProgramAsset =
        new GLSLProgramAsset {
            Name = "nagule.pipeline.hiz"
        }
        .WithShaders(
            new(ShaderType.Vertex,
                ShaderUtils.LoadEmbedded("nagule.common.quad.vert.glsl")),
            new(ShaderType.Fragment,
                ShaderUtils.LoadEmbedded("nagule.pipeline.hiz.frag.glsl")))
        .WithParameter("LastMip", ShaderParameterType.Texture2D);

    public override void Initialize(World world, Scheduler scheduler)
    {
        base.Initialize(world, scheduler);

        var programManager = world.GetAddon<GLSLProgramManager>();
        var hizProgramEntity = GLSLProgram.CreateEntity(
            world, s_hizProgramAsset, AssetLife.Persistent);

        var buffer = Pipeline.AcquireAddon<HierarchicalZBuffer>();
        var framebuffer = Pipeline.GetAddon<Framebuffer>();

        RenderFrame.Start(() => {
            buffer.Load(framebuffer.Width / 2, framebuffer.Height / 2);
            return true;
        });

        RenderFrame.Start(() => {
            ref var hizProgramState = ref programManager.RenderStates.GetOrNullRef(hizProgramEntity);
            if (Unsafe.IsNullRef(ref hizProgramState)) {
                return ShouldStop;
            }

            int halfWidth = framebuffer.Width / 2;
            int halfHeight = framebuffer.Height / 2;
            if (buffer.Width != halfWidth || buffer.Height != halfHeight) {
                buffer.Resize(halfWidth, halfHeight);
            }

            var textureHandle = buffer.TextureHandle.Handle;
            var depthHandle = framebuffer.DepthHandle.Handle;

            GL.UseProgram(hizProgramState.Handle.Handle);
            GL.BindVertexArray(framebuffer.EmptyVertexArray.Handle);

            GL.ColorMask(false, false, false, false);
            GL.DepthFunc(DepthFunction.Always);

            // downsample depth buffer to hi-Z buffer

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2d, depthHandle);

            GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureBaseLevel, 0);
            GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMaxLevel, 0);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer,
                FramebufferAttachment.DepthAttachment, TextureTarget.Texture2d, textureHandle, 0);

            GL.Viewport(0, 0, buffer.Width, buffer.Height);
            GL.Clear(ClearBufferMask.DepthBufferBit);
            GL.DrawArrays(GLPrimitiveType.TriangleStrip, 0, 4);

            // generate hi-z buffer

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2d, textureHandle);

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
                    FramebufferAttachment.DepthAttachment, TextureTarget.Texture2d, textureHandle, i);
                GL.DrawArrays(GLPrimitiveType.TriangleStrip, 0, 4);
            }

            GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureBaseLevel, 0);
            GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMaxLevel, levelCount - 1);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer,
                FramebufferAttachment.DepthAttachment, TextureTarget.Texture2d, depthHandle, 0);

            GL.Viewport(0, 0, framebuffer.Width, framebuffer.Height);
            GL.BindVertexArray(0);
            
            GL.DepthFunc(DepthFunction.Lequal);
            GL.ColorMask(true, true, true, true);
            GL.UseProgram(0);

            return ShouldStop;
        });
    }
}