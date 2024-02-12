namespace Nagule.Graphics.PostProcessing;

using Nagule.Graphics.Backends.OpenTK;
using Sia;

[AfterSystem<StagePostProcessingBeginPass>]
[BeforeSystem<StagePostProcessingFinishPass>]
public class DrawEffectsPass(EntityRef pipelineEntity) : RenderPassBase
{
    private IPipelineFramebuffer? _framebuffer;
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

        _framebuffer ??= world.GetAddon<IPipelineFramebuffer>();
        var colorHandle = _framebuffer.ColorAttachmentHandle.Handle;
        var depthHandle = _framebuffer.DepthAttachmentHandle.Handle;

        _framebuffer.SwapColorAttachments();
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, _framebuffer.Handle.Handle);
        matState.Bind(programState);
        GL.BindVertexArray(GLUtils.EmptyVertexArray.Handle);

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