namespace Nagule.Graphics.Backend.OpenTK;

using Sia;

[AfterSystem<Light3DCullingPass>]
public class Light3DBuffersActivatePass : RenderPassSystemBase
{
    public override void Initialize(World world, Scheduler scheduler)
    {
        base.Initialize(world, scheduler);

        var light3dLib = world.GetAddon<Light3DLibrary>();
        var clustersBuffer = Pipeline.GetAddon<Light3DClustersBuffer>();
        
        RenderFrame.Start(() => {
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