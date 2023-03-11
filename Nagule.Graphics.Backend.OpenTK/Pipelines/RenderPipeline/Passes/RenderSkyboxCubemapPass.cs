namespace Nagule.Graphics.Backend.OpenTK;

using System.Runtime.CompilerServices;

public class RenderSkyboxCubemapPass : RenderPassBase
{
    private Guid _programId;

    private static GLSLProgram s_program =
        new GLSLProgram {
            Name = "nagule.skybox_cubemap"
        }
        .WithShaders(
            new(ShaderType.Vertex,
                GraphicsHelper.LoadEmbededShader("nagule.common.panorama.vert.glsl")),
            new(ShaderType.Fragment,
                GraphicsHelper.LoadEmbededShader("skybox_cubemap.frag.glsl")))
        .WithParameter(new(MaterialKeys.SkyboxTex));

    public override void LoadResources(IContext context)
    {
        _programId = context.GetResourceLibrary().Reference(Id, s_program);
    }

    public override void Execute(
        ICommandHost host, IRenderPipeline pipeline, Guid cameraId, MeshGroup meshGroup)
    {
        ref var renderSettings = ref host.RequireOrNullRef<RenderSettingsData>(pipeline.RenderSettingsId);
        if (Unsafe.IsNullRef(ref renderSettings)) { return; }

        if (renderSettings.SkyboxId == null) { return; }
        ref var skyboxData = ref host.RequireOrNullRef<TextureData>(renderSettings.SkyboxId.Value);
        if (Unsafe.IsNullRef(ref skyboxData)) { return; }

        ref var skyboxProgram = ref host.RequireOrNullRef<GLSLProgramData>(_programId);
        if (Unsafe.IsNullRef(ref skyboxProgram)) { return; }

        pipeline.AcquireColorTexture();

        GL.UseProgram(skyboxProgram.Handle);
        GL.DepthMask(false);

        GL.ActiveTexture(TextureUnit.Texture0 + GLHelper.BuiltInBufferCount);
        GL.BindTexture(TextureTarget.TextureCubeMap, skyboxData.Handle);
        GL.Uniform1i(skyboxProgram.TextureLocations!["SkyboxTex"], GLHelper.BuiltInBufferCount);

        GL.DrawArrays(GLPrimitiveType.TriangleStrip, 0, 4);
        GL.DepthMask(true);
    }
}