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

        var pipelineStateEntity = pipelineEntity.GetStateEntity();

        RenderFrame.Start(() => {
            ref var pipelineState = ref pipelineStateEntity.Get<EffectPipelineState>();
            if (!pipelineState.Loaded) { return NextFrame; }

            ref var matState = ref pipelineState.MaterialState.Get<MaterialState>();
            if (!matState.Loaded) { return NextFrame; }

            ref var programState = ref matState.ColorProgramState.Get<GLSLProgramState>();
            if (!programState.Loaded) { return NextFrame; }

            var framebuffer = Pipeline.GetAddon<Framebuffer>();
            var colorHandle = framebuffer.ColorHandle.Handle;
            var depthHandle = framebuffer.DepthHandle.Handle;

            framebuffer.Swap();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, framebuffer.Handle.Handle);
            matState.Bind(programState);
            GL.BindVertexArray(framebuffer.EmptyVertexArray.Handle);

            uint startIndex = programState.EnableLightingBuffers();
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

            matState.ActivateTextures(programState, startIndex);

            GL.Clear(ClearBufferMask.ColorBufferBit);
            GL.Disable(EnableCap.DepthTest);
            GL.DepthMask(false);
            GL.DrawArrays(GLPrimitiveType.TriangleStrip, 0, 4);
            GL.DepthMask(true);
            GL.Enable(EnableCap.DepthTest);

            GL.BindVertexArray(0);
            return NextFrame;
        });
    }
}