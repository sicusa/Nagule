using Sia;

namespace Nagule.Graphics.ShadowMapping;

public class ShadowMapTilesetActivatePass : RenderPassBase
{
    private ShadowMapLibrary? shadowMapLib;

    public override void Initialize(World world, Scheduler scheduler)
    {
        base.Initialize(world, scheduler);
        shadowMapLib = MainWorld.GetAddon<ShadowMapLibrary>();
    }

    public override void Execute(World world, Scheduler scheduler, IEntityQuery query)
    {
        GL.ActiveTexture(TextureUnit.Texture4);
        GL.BindTexture(TextureTarget.Texture2dArray, shadowMapLib!.TilesetState.Handle.Handle);
    }
}