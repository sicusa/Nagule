namespace Nagule.Graphics.Backend.OpenTK;

using Sia;

public class DrawSkyboxPass : RenderPassSystemBase
{
    private EntityRef _programEntity;

    private static readonly RGLSLProgram s_program =
        new RGLSLProgram {
            Name = "nagule.pipeline.skybox_cubemap"
        }
        .WithShaders(
            new(ShaderType.Vertex,
                ShaderUtils.LoadCore("nagule.common.panorama.vert.glsl")),
            new(ShaderType.Fragment,
                ShaderUtils.LoadCore("nagule.pipeline.skybox_cubemap.frag.glsl")))
        .WithParameter(new(MaterialKeys.SkyboxTex));

    public override void Initialize(World world, Scheduler scheduler)
    {
        base.Initialize(world, scheduler);

        var programManager = world.GetAddon<GLSLProgramManager>();
        _programEntity = programManager.Acquire(s_program);

        var programStateEntity = _programEntity.GetStateEntity();
        var cameraStateEntity = Camera.GetStateEntity();

        RenderFrame.Start(() => {
            ref var cameraState = ref cameraStateEntity.Get<Camera3DState>();
            if (!cameraState.Loaded) { return NextFrame; }

            ref var renderSettings = ref cameraState.RenderSettingsState.Get<RenderSettingsState>();
            if (!renderSettings.Loaded) { return NextFrame; }

            if (renderSettings.SkyboxState is not EntityRef skyboxStateEntity) {
                return NextFrame;
            }

            ref var skyboxState = ref skyboxStateEntity.Get<CubemapState>();
            if (!skyboxState.Loaded) { return NextFrame; }

            ref var programState = ref programStateEntity.Get<GLSLProgramState>();
            if (!programState.Loaded) { return NextFrame; }

            var framebuffer = Pipeline.GetAddon<Framebuffer>();
            GL.UseProgram(programState.Handle.Handle);
            GL.BindVertexArray(framebuffer.EmptyVertexArray.Handle);

            GL.ActiveTexture(TextureUnit.Texture0 + GLUtils.BuiltInBufferCount);
            GL.BindTexture(TextureTarget.TextureCubeMap, skyboxState.Handle.Handle);
            GL.Uniform1i(programState.TextureLocations!["SkyboxTex"], GLUtils.BuiltInBufferCount);

            GL.DepthMask(false);
            GL.DrawArrays(GLPrimitiveType.TriangleStrip, 0, 4);
            GL.DepthMask(true); 

            GL.BindVertexArray(0);
            return NextFrame;
        });
    }

    public override void Uninitialize(World world, Scheduler scheduler)
    {
        base.Uninitialize(world, scheduler);
        _programEntity.Dispose();
    }
}