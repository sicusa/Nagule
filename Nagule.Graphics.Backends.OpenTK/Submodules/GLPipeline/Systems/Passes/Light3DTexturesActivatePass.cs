namespace Nagule.Graphics.Backends.OpenTK;

using Sia;

public class Light3DTexturesActivatePass : RenderPassBase
{
    private Light3DLibrary? lightLib;
    private ShadowMapLibrary? shadowMapLib;

    public override void Initialize(World world, Scheduler scheduler)
    {
        base.Initialize(world, scheduler);

        lightLib = MainWorld.GetAddon<Light3DLibrary>();
        shadowMapLib = MainWorld.GetAddon<ShadowMapLibrary>();
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

        GL.ActiveTexture(TextureUnit.Texture4);
        GL.BindTexture(TextureTarget.Texture2dArray, shadowMapLib!.TilesetState.Handle.Handle);
    }
}