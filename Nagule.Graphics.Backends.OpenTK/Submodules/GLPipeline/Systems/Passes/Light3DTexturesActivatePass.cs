namespace Nagule.Graphics.Backends.OpenTK;

using Sia;

public class Light3DTexturesActivatePass : RenderPassBase
{
    private Light3DLibrary? lightLib;

    public override void Initialize(World world, Scheduler scheduler)
    {
        base.Initialize(world, scheduler);
        lightLib = MainWorld.GetAddon<Light3DLibrary>();
    }

    public override void Execute(World world, Scheduler scheduler, IEntityQuery query)
    {
        var clusterer = world.GetAddon<Light3DClusterer>();

        GL.ActiveTexture(TextureUnit.Texture1);
        GL.BindTexture(TextureTarget.TextureBuffer, lightLib!.TextureHandle.Handle);

        GL.ActiveTexture(TextureUnit.Texture2);
        GL.BindTexture(TextureTarget.TextureBuffer, clusterer.ClustersTexHandle.Handle);

        GL.ActiveTexture(TextureUnit.Texture3);
        GL.BindTexture(TextureTarget.TextureBuffer, clusterer.ClusterLightCountsTexHandle.Handle);
    }
}