namespace Nagule.Graphics.Backend.OpenTK;

using System.Runtime.CompilerServices;

public class RenderSkyboxCubemapPass : IRenderPass
{
    public void Initialize(ICommandHost host, IRenderPipeline pipeline) { }
    public void Uninitialize(ICommandHost host, IRenderPipeline pipeline) { }

    public void Render(ICommandHost host, IRenderPipeline pipeline, MeshGroup meshGroup)
    {
        ref var renderSettings = ref host.RequireOrNullRef<RenderSettingsData>(pipeline.RenderSettingsId);
        if (Unsafe.IsNullRef(ref renderSettings)) { return; }

        if (renderSettings.SkyboxId != null) {
            ref var skyboxData = ref host.RequireOrNullRef<TextureData>(renderSettings.SkyboxId.Value);
            if (!Unsafe.IsNullRef(ref skyboxData)) {
                GLHelper.DrawCubemapSkybox(host, skyboxData.Handle);
            }
        }
    }
}