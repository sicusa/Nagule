namespace Nagule.Graphics.Backends.OpenTK;

using Sia;

public class BlitDepthToRenderTargetPass : RenderPassBase
{
    private static readonly RGLSLProgram s_blitProgramAsset = 
        new RGLSLProgram {
            Name = "nagule.pipeline.blit_depth"
        }
        .WithShaders(
            new(ShaderType.Fragment,
                ShaderUtils.LoadCore("nagule.common.blit_depth.frag.glsl")),
            new(ShaderType.Vertex,
                ShaderUtils.LoadCore("nagule.common.quad.vert.glsl")))
        .WithParameter("DepthBuffer", ShaderParameterType.Texture2D);

    private IPipelineFramebuffer? _framebuffer;
    private EntityRef _blitProgramEntity;

    public override void Initialize(World world, Scheduler scheduler)
    {
        base.Initialize(world, scheduler);
        _blitProgramEntity = MainWorld.AcquireAsset(s_blitProgramAsset);
    }

    public override void Execute(World world, Scheduler scheduler, IEntityQuery query)
    {
        ref var cameraState = ref CameraState.Get<Camera3DState>();
        var renderTarget = cameraState.RenderTarget;
        if (renderTarget == null) { return; }

        ref var blitProgramState = ref _blitProgramEntity.GetState<GLSLProgramState>();
        if (!blitProgramState.Loaded) { return; }

        _framebuffer ??= world.GetAddon<IPipelineFramebuffer>();
        renderTarget.Blit(blitProgramState.Handle, _framebuffer.DepthAttachmentHandle);
    }
}