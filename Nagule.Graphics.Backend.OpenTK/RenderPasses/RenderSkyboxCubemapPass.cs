namespace Nagule.Graphics.Backend.OpenTK;

using System.Runtime.CompilerServices;

using global::OpenTK.Graphics.OpenGL;

public class RenderSkyboxCubemapPass : IRenderPass
{
    public void Initialize(ICommandHost host, IRenderPipeline pipeline) { }
    public void Uninitialize(ICommandHost host, IRenderPipeline pipeline) { }

    public void Render(ICommandHost host, IRenderPipeline pipeline, MeshGroup meshGroup)
    {
        ref var renderSettings = ref host.RequireOrNullRef<RenderSettingsData>(pipeline.RenderSettingsId);
        if (Unsafe.IsNullRef(ref renderSettings)) { return; }

        if (renderSettings.SkyboxId == null) { return; }
        ref var skyboxData = ref host.RequireOrNullRef<TextureData>(renderSettings.SkyboxId.Value);
        if (Unsafe.IsNullRef(ref skyboxData)) { return; }

        ref var skyboxProgram = ref host.RequireOrNullRef<GLSLProgramData>(Graphics.SkyboxShaderProgramId);
        if (Unsafe.IsNullRef(ref skyboxProgram)) { return; }

        GL.UseProgram(skyboxProgram.Handle);
        GL.DepthMask(false);

        GL.ActiveTexture(TextureUnit.Texture0 + GLHelper.BuiltInBufferCount);
        GL.BindTexture(TextureTarget.TextureCubeMap, skyboxData.Handle);
        GL.Uniform1i(skyboxProgram.TextureLocations!["SkyboxTex"], GLHelper.BuiltInBufferCount);

        GL.DrawArrays(PrimitiveType.TriangleStrip, 0, 4);
        GL.DepthMask(true);
    }
}