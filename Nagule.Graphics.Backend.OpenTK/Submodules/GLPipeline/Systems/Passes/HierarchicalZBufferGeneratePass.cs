namespace Nagule.Graphics.Backend.OpenTK;

using Sia;

public class HierarchicalZBufferGeneratePass : RenderPassSystemBase
{
    private static readonly RGLSLProgram s_hizProgramAsset =
        new RGLSLProgram {
            Name = "nagule.pipeline.hiz"
        }
        .WithShaders(
            new(ShaderType.Vertex,
                ShaderUtils.LoadCore("nagule.common.quad.vert.glsl")),
            new(ShaderType.Fragment,
                ShaderUtils.LoadCore("nagule.pipeline.hiz.frag.glsl")))
        .WithParameter("LastMip", ShaderParameterType.Texture2D);

    public override void Initialize(World world, Scheduler scheduler)
    {
        base.Initialize(world, scheduler);

        var hizProgramEntity = GLSLProgram.CreateEntity(
            world, s_hizProgramAsset, AssetLife.Persistent);

        HierarchicalZBuffer? buffer = null;
        Framebuffer? framebuffer = null;
        int lastMipLoc = -1;

        RenderFrame.Start(() => {
            buffer = AddAddon<HierarchicalZBuffer>(Pipeline);
            framebuffer = Pipeline.GetAddon<Framebuffer>();
            return true;
        });

        RenderFrame.Start(() => {
            ref var hizProgramState = ref hizProgramEntity.GetState<GLSLProgramState>();
            if (!hizProgramState.Loaded) {
                return NextFrame;
            }
            if (lastMipLoc == -1) {
                lastMipLoc = hizProgramState.TextureLocations!["LastMip"];
            }

            var textureHandle = buffer!.TextureHandle.Handle;
            var depthHandle = framebuffer!.DepthHandle.Handle;

            GL.UseProgram(hizProgramState.Handle.Handle);

            GL.ColorMask(false, false, false, false);
            GL.DepthFunc(DepthFunction.Always);
            GL.BindVertexArray(framebuffer.EmptyVertexArray.Handle);

            // downsample depth buffer to hi-Z buffer

            GL.BindTexture(TextureTarget.Texture2d, textureHandle);
            GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureBaseLevel, 0);
            GL.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMaxLevel, 0);

            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer,
                FramebufferAttachment.DepthAttachment, TextureTarget.Texture2d, textureHandle, 0);

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2d, depthHandle);

            GL.Viewport(0, 0, buffer.Width, buffer.Height);
            GL.Clear(ClearBufferMask.DepthBufferBit);
            GL.DrawArrays(GLPrimitiveType.TriangleStrip, 0, 4);

            // generate hi-z buffer

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2d, textureHandle);

            int width = buffer.Width;
            int height = buffer.Height;
            int levelCount = buffer.LevelCount;

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

                GL.Clear(ClearBufferMask.DepthBufferBit);
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

            return NextFrame;
        });
    }
}