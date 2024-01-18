namespace Nagule.Graphics.Backends.OpenTK;

using Sia;

public class Light3DBuffersActivatePass : RenderPassSystemBase
{
    public override void Initialize(World world, Scheduler scheduler)
    {
        base.Initialize(world, scheduler);

        var light3dLib = world.GetAddon<Light3DLibrary>();
        
        RenderFramer.Start(() => {
            var clustersBuffer = Pipeline.GetAddon<Light3DClustersBuffer>();

            GL.ActiveTexture(TextureUnit.Texture1);
            GL.BindTexture(TextureTarget.TextureBuffer, light3dLib.TextureHandle.Handle);

            GL.ActiveTexture(TextureUnit.Texture2);
            GL.BindTexture(TextureTarget.TextureBuffer, clustersBuffer.ClustersTexHandle.Handle);

            GL.ActiveTexture(TextureUnit.Texture3);
            GL.BindTexture(TextureTarget.TextureBuffer, clustersBuffer.ClusterLightCountsTexHandle.Handle);

            return NextFrame;
        });
    }
}