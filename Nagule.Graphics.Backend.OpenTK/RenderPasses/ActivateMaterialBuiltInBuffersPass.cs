namespace Nagule.Graphics.Backend.OpenTK;

using System.Runtime.CompilerServices;

public class ActivateMaterialBuiltInBuffersPass : RenderPassBase
{
    private Guid _defaultTexId;

    public override void LoadResources(IContext context)
    {
        _defaultTexId = ResourceLibrary.Reference(context, Id, Texture.White);
    }

    public override void Render(ICommandHost host, IRenderPipeline pipeline, MeshGroup meshGroup)
    {
        ref var defaultTexData = ref host.RequireOrNullRef<TextureData>(_defaultTexId);
        if (Unsafe.IsNullRef(ref defaultTexData)) {
            return;
        }

        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2d, defaultTexData.Handle);

        var lightBufferHandle = host.RequireAny<LightsBuffer>().TexHandle;
        GL.ActiveTexture(TextureUnit.Texture1);
        GL.BindTexture(TextureTarget.TextureBuffer, lightBufferHandle);

        ref readonly var lightingEnv = ref host.InspectAny<LightingEnvUniformBuffer>();
        GL.ActiveTexture(TextureUnit.Texture2);
        GL.BindTexture(TextureTarget.TextureBuffer, lightingEnv.ClustersTexHandle);
        GL.ActiveTexture(TextureUnit.Texture3);
        GL.BindTexture(TextureTarget.TextureBuffer, lightingEnv.ClusterLightCountsTexHandle);
    }
}