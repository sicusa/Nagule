namespace Nagule.Graphics;

using Sia;

public class GraphicsAssetManager<TAsset, TAssetRecord, TAssetState>
    : AssetManager<TAsset, TAssetRecord, TAssetState>
    where TAsset : struct, IAsset<TAssetRecord>
    where TAssetRecord : IAssetRecord
    where TAssetState : struct
{
    public RenderFramer RenderFramer => World.GetAddon<RenderFramer>();

    protected override void DestroyState(in EntityRef entity, in TAsset asset, ref State state)
    {
        var source = state.Entity.Hang(e => e.Dispose());
        RenderFramer.Enqueue(entity, source.Cancel);
    }
}