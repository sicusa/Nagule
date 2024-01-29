namespace Nagule.Graphics.Backends.OpenTK;

using Nagule.Graphics.PostProcessing;
using Sia;

[AfterSystem<StagePostProcessingBeginPass>]
[BeforeSystem<StagePostProcessingFinishPass>]
public class DrawEffectsPass(EntityRef pipelineEntity) : RenderPassBase
{
    private EntityRef _pipelineState;

    public override void Initialize(World world, Scheduler scheduler)
    {
        base.Initialize(world, scheduler);
        _pipelineState = pipelineEntity.GetStateEntity();
    }

    public override void Execute(World world, Scheduler scheduler, IEntityQuery query)
    {
        ref var pipelineState = ref _pipelineState.Get<EffectPipelineState>();
        if (!pipelineState.Loaded) { return; }

        ref var matState = ref pipelineState.MaterialState.Get<MaterialState>();
        if (!matState.Loaded) { return; }

        ref var programState = ref matState.ColorProgramState.Get<GLSLProgramState>();
        if (!programState.Loaded) { return; }

        var framebuffer = world.GetAddon<ColorFramebuffer>();
        var colorHandle = framebuffer.ColorHandle.Handle;
        var depthHandle = framebuffer.DepthHandle.Handle;

        framebuffer.Swap();
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, framebuffer.Handle.Handle);
        matState.Bind(programState);
        GL.BindVertexArray(framebuffer.EmptyVertexArray.Handle);

        uint startIndex = programState.EnableInternalBuffers();
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
    }
}