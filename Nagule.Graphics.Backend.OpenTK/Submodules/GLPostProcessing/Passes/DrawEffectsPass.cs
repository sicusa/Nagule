namespace Nagule.Graphics.Backend.OpenTK;

using Nagule.Graphics.PostProcessing;
using Sia;

[AfterSystem<StagePostProcessingBeginPass>]
[BeforeSystem<StagePostProcessingFinishPass>]
public class DrawEffectsPass(EntityRef pipelineEntity) : RenderPassSystemBase
{
    public override void Initialize(World world, Scheduler scheduler)
    {
        base.Initialize(world, scheduler);

        var framebuffer = Pipeline.GetAddon<Framebuffer>();

        RenderFrame.Start(() => {
            ref var pipelineState = ref pipelineEntity.GetState<EffectPipelineState>();
            if (!pipelineState.Loaded) { return NextFrame; }

            var material = pipelineState.MaterialEntity;
            ref var materialState = ref material.GetState<MaterialState>();
            if (!materialState.Loaded) { return NextFrame; }

            ref var programState = ref materialState.ColorProgram.GetState<GLSLProgramState>();
            if (!programState.Loaded) { return NextFrame; }

            var colorHandle = framebuffer.ColorHandle.Handle;
            var depthHandle = framebuffer.DepthHandle.Handle;

            framebuffer.Swap();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, framebuffer.Handle.Handle);

            GL.BindBufferBase(BufferTargetARB.UniformBuffer, (int)UniformBlockBinding.Material, materialState.UniformBufferHandle.Handle);
            GL.UseProgram(programState.Handle.Handle);
            GL.BindVertexArray(framebuffer.EmptyVertexArray.Handle);

            uint startIndex = programState.EnableBuiltInBuffers();
            var texLocations = programState.TextureLocations;

            if (texLocations != null) {
                if (texLocations.TryGetValue("ColorTex", out var colorTexLoc)) {
                    GL.ActiveTexture(TextureUnit.Texture0 + startIndex);
                    GL.BindTexture(TextureTarget.Texture2d, colorHandle);
                    GL.Uniform1i(colorTexLoc, (int)startIndex);
                    startIndex++;
                }
                if (texLocations.TryGetValue("DepthTex", out var depthTexLoc)) {
                    GL.ActiveTexture(TextureUnit.Texture0 + startIndex);
                    GL.BindTexture(TextureTarget.Texture2d, depthHandle);
                    GL.Uniform1i(depthTexLoc, (int)startIndex);
                    startIndex++;
                }
            }

            materialState.EnableTextures(programState, startIndex);

            GL.Clear(ClearBufferMask.ColorBufferBit);
            GL.Disable(EnableCap.DepthTest);

            GL.DrawArrays(GLPrimitiveType.TriangleStrip, 0, 4);

            GL.Enable(EnableCap.DepthTest);
            GL.BindVertexArray(0);

            return NextFrame;
        });
    }
}