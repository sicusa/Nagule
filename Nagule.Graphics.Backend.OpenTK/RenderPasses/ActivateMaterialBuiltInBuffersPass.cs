namespace Nagule.Graphics.Backend.OpenTK;

using System.Runtime.CompilerServices;

using global::OpenTK.Graphics.OpenGL;

public struct ActivateMaterialBuiltInBuffersPass : IRenderPass
{
    public void Initialize(ICommandHost host, IRenderPipeline pipeline) { }
    public void Uninitialize(ICommandHost host, IRenderPipeline pipeline) { }

    public void Render(ICommandHost host, IRenderPipeline pipeline, MeshGroup meshGroup)
    {
        ref var defaultTexData = ref host.RequireOrNullRef<TextureData>(Graphics.DefaultTextureId);
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