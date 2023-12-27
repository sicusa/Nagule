namespace Nagule.Graphics.Backend.OpenTK;

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Sia;

public class DrawTransparentWBOITPass : DrawPassBase
{
    [AllowNull] private TransparencyFramebuffer _transparencyFramebuffer;

    private EntityRef _composeProgram;

    private static readonly GLSLProgramAsset s_composeProgramAsset =
        new GLSLProgramAsset {
            Name = "nagule.pipeline.transparency_compose"
        }
        .WithShaders(
            new(ShaderType.Vertex,
                ShaderUtils.LoadEmbedded("nagule.common.quad.vert.glsl")),
            new(ShaderType.Fragment,
                ShaderUtils.LoadEmbedded("nagule.pipeline.transparency_compose.frag.glsl")))
        .WithParameters(
            new("AccumTex", ShaderParameterType.Texture2D),
            new("RevealTex", ShaderParameterType.Texture2D));

    protected override EntityRef GetShaderProgram(Mesh3DInstanceGroup group, Mesh3DDataBuffer meshData, in MaterialState materialState)
        => materialState.ColorProgram;
    
    public DrawTransparentWBOITPass() : base(MeshFilter.Transparent) {}

    public override void Initialize(World world, Scheduler scheduler)
    {
        base.Initialize(world, scheduler);

        _transparencyFramebuffer = Pipeline.AcquireAddon<TransparencyFramebuffer>();
        _composeProgram = ProgramManager.Acquire(s_composeProgramAsset);
    }

    public override void Uninitialize(World world, Scheduler scheduler)
    {
        base.Uninitialize(world, scheduler);

        _composeProgram.Destroy();
        RenderFrame.Start(() => {
            _transparencyFramebuffer.Unload();
            return true;
        });
    }

    protected override void BeginPass()
    {
        if (_transparencyFramebuffer.Handle == FramebufferHandle.Zero) {
            _transparencyFramebuffer.Load(Framebuffer);
        }
        else if (_transparencyFramebuffer.Width != Framebuffer.Width || _transparencyFramebuffer.Height != Framebuffer.Height) {
            _transparencyFramebuffer.Resize(Framebuffer);
        }

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, _transparencyFramebuffer.Handle.Handle);
        GL.ClearColor(0, 0, 0, 1);
        GL.Clear(ClearBufferMask.ColorBufferBit);

        GL.DepthMask(false);
        GL.Enable(EnableCap.Blend);
        GL.BlendFuncSeparate(BlendingFactor.One, BlendingFactor.One, BlendingFactor.Zero, BlendingFactor.OneMinusSrcAlpha);
    }

    protected override void EndPass()
    {
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, Framebuffer.Handle.Handle);

        ref var composeProgramState = ref _composeProgram.GetState<GLSLProgramState>();
        if (!composeProgramState.Loaded) {
            GL.DepthFunc(DepthFunction.Lequal);
            GL.DepthMask(true);
            GL.Disable(EnableCap.Blend);
            return;
        }

        GL.UseProgram(composeProgramState.Handle.Handle);
        GL.BlendFunc(BlendingFactor.One, BlendingFactor.OneMinusSrcAlpha);
        GL.DepthFunc(DepthFunction.Always);

        GL.ActiveTexture(TextureUnit.Texture0 + GLUtils.BuiltInBufferCount);
        GL.BindTexture(TextureTarget.Texture2d, _transparencyFramebuffer.AccumTextureHandle.Handle);
        GL.Uniform1i(composeProgramState.TextureLocations!["AccumTex"], GLUtils.BuiltInBufferCount);

        GL.ActiveTexture(TextureUnit.Texture0 + GLUtils.BuiltInBufferCount + 1);
        GL.BindTexture(TextureTarget.Texture2d, _transparencyFramebuffer.RevealTextureHandle.Handle);
        GL.Uniform1i(composeProgramState.TextureLocations["RevealTex"], GLUtils.BuiltInBufferCount + 1);

        GL.DrawArrays(GLPrimitiveType.TriangleStrip, 0, 4);

        GL.DepthFunc(DepthFunction.Lequal);
        GL.DepthMask(true);
        GL.Disable(EnableCap.Blend);
    }

    protected override void Draw(Mesh3DInstanceGroup group, Mesh3DDataBuffer meshData, in MaterialState materialState, in GLSLProgramState programState)
    {
        GL.BindVertexArray(group.VertexArrayHandle.Handle);
        GL.DrawElementsInstanced(
            meshData.PrimitiveType, meshData.IndexCount, DrawElementsType.UnsignedInt, IntPtr.Zero, group.Count);
    }
}