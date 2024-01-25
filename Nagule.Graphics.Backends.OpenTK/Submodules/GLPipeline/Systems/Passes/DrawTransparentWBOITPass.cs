namespace Nagule.Graphics.Backends.OpenTK;

using Sia;

public class DrawTransparentWBOITPass()
    : DrawPassBase(materialPredicate: MaterialPredicates.IsTransparent)
{
    private TransparencyFramebuffer? _transparencyFramebuffer;

    private EntityRef _composeProgram;
    private EntityRef _composeProgramState;

    private static readonly RGLSLProgram s_composeProgramAsset =
        new RGLSLProgram {
            Name = "nagule.pipeline.transparency_compose"
        }
        .WithShaders(
            new(ShaderType.Vertex,
                ShaderUtils.LoadCore("nagule.common.quad.vert.glsl")),
            new(ShaderType.Fragment,
                ShaderUtils.LoadCore("nagule.pipeline.transparency_compose.frag.glsl")))
        .WithParameters(
            new("AccumTex", ShaderParameterType.Texture2D),
            new("RevealTex", ShaderParameterType.Texture2D));

    protected override EntityRef GetShaderProgramState(
        Mesh3DInstanceGroup group, Mesh3DDataBuffer meshData, in MaterialState materialState)
        => materialState.ColorProgramState;

    public override void Initialize(World world, Scheduler scheduler)
    {
        base.Initialize(world, scheduler);

        _composeProgram = MainWorld.GetAddon<GLSLProgramManager>().Acquire(s_composeProgramAsset);
        _composeProgramState = _composeProgram.GetStateEntity();
    }

    public override void Uninitialize(World world, Scheduler scheduler)
    {
        base.Uninitialize(world, scheduler);
        _composeProgram.Dispose();
    }

    private void BindTransparencyFramebuffer()
    {
        _transparencyFramebuffer ??= AddAddon<TransparencyFramebuffer>(World);

        if (_transparencyFramebuffer.Width != Framebuffer.Width
                || _transparencyFramebuffer.Height != Framebuffer.Height) {
            _transparencyFramebuffer.Resize(Framebuffer);
        }

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, _transparencyFramebuffer.Handle.Handle);
        GL.ClearColor(0, 0, 0, 1);
        GL.Clear(ClearBufferMask.ColorBufferBit);

        GL.DepthMask(false);
        GL.Enable(EnableCap.Blend);
        GL.BlendFuncSeparate(BlendingFactor.One, BlendingFactor.One, BlendingFactor.Zero, BlendingFactor.OneMinusSrcAlpha);
    }

    protected override bool BeforeDraw(Mesh3DInstanceGroup group, Mesh3DDataBuffer meshData, in MaterialState materialState, in GLSLProgramState programState)
    {
        if (DrawnGroupCount == 0) {
            BindTransparencyFramebuffer();
        }
        return true;
    }

    protected override void EndPass()
    {
        if (DrawnObjectCount == 0) {
            return;
        }
        ref var composeProgramState = ref _composeProgramState.Get<GLSLProgramState>();
        if (!composeProgramState.Loaded) {
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, Framebuffer.Handle.Handle);
            GL.DepthMask(true);
            GL.Disable(EnableCap.Blend);
            GL.BindVertexArray(0);
            return;
        }

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, Framebuffer.Handle.Handle);
        GL.UseProgram(composeProgramState.Handle.Handle);
        GL.BindVertexArray(Framebuffer.EmptyVertexArray.Handle);

        GL.BlendFunc(BlendingFactor.One, BlendingFactor.OneMinusSrcAlpha);
        GL.DepthFunc(DepthFunction.Always);

        GL.ActiveTexture(TextureUnit.Texture0 + GLUtils.BuiltInTextureCount);
        GL.BindTexture(TextureTarget.Texture2d, _transparencyFramebuffer!.AccumTextureHandle.Handle);
        GL.Uniform1i(composeProgramState.TextureLocations!["AccumTex"], GLUtils.BuiltInTextureCount);

        GL.ActiveTexture(TextureUnit.Texture0 + GLUtils.BuiltInTextureCount + 1);
        GL.BindTexture(TextureTarget.Texture2d, _transparencyFramebuffer.RevealTextureHandle.Handle);
        GL.Uniform1i(composeProgramState.TextureLocations["RevealTex"], GLUtils.BuiltInTextureCount + 1);

        GL.DrawArrays(GLPrimitiveType.TriangleStrip, 0, 4);

        GL.DepthFunc(DepthFunction.Lequal);
        GL.DepthMask(true);
        GL.Disable(EnableCap.Blend);
        GL.BindVertexArray(0);
    }
}