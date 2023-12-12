using System.Runtime.CompilerServices;
using Sia;

namespace Nagule.Graphics.Backend.OpenTK;

public class DrawSkyboxPass : RenderPassSystemBase
{
    private EntityRef _programEntity;

    private static readonly GLSLProgramAsset s_program =
        new GLSLProgramAsset {
            Name = "nagule.pipeline.skybox_cubemap"
        }
        .WithShaders(
            new(ShaderType.Vertex,
                ShaderUtils.LoadEmbedded("nagule.common.panorama.vert.glsl")),
            new(ShaderType.Fragment,
                ShaderUtils.LoadEmbedded("nagule.pipeline.skybox_cubemap.frag.glsl")))
        .WithParameter(new(MaterialKeys.SkyboxTex));

    public override void Initialize(World world, Scheduler scheduler)
    {
        base.Initialize(world, scheduler);

        var programManager = world.GetAddon<GLSLProgramManager>();
        var cameraManager = world.GetAddon<Camera3DManager>();
        var renderSettingsManager = world.GetAddon<RenderSettingsManager>();
        var cubemapManager = world.GetAddon<CubemapManager>();

        _programEntity = programManager.Acquire(s_program);

        RenderFrame.Start(() => {
            ref var cameraState = ref cameraManager.RenderStates.GetOrNullRef(Camera);
            if (Unsafe.IsNullRef(ref cameraState)) {
                return ShouldStop;
            }

            ref var renderSettings = ref renderSettingsManager.RenderStates.GetOrNullRef(
                cameraState.RenderSettingsEntity);
            if (Unsafe.IsNullRef(ref renderSettings)) {
                return ShouldStop;
            }

            if (renderSettings.SkyboxEntity == null) {
                return ShouldStop;
            }
            ref var skyboxState = ref cubemapManager.RenderStates.GetOrNullRef(renderSettings.SkyboxEntity.Value);
            if (Unsafe.IsNullRef(ref skyboxState)) {
                return ShouldStop;
            }

            ref var programState = ref programManager.RenderStates.GetOrNullRef(_programEntity);
            if (Unsafe.IsNullRef(ref programState)) {
                return ShouldStop;
            }

            GL.UseProgram(programState.Handle.Handle);
            GL.DepthMask(false);

            GL.ActiveTexture(TextureUnit.Texture0 + GLUtils.BuiltInBufferCount);
            GL.BindTexture(TextureTarget.TextureCubeMap, skyboxState.Handle.Handle);
            GL.Uniform1i(programState.TextureLocations!["SkyboxTex"], GLUtils.BuiltInBufferCount);

            GL.DrawArrays(GLPrimitiveType.TriangleStrip, 0, 4);
            GL.DepthMask(true); 
            return ShouldStop;
        });
    }

    public override void Uninitialize(World world, Scheduler scheduler)
    {
        base.Uninitialize(world, scheduler);
        world.Destroy(_programEntity);
    }
}