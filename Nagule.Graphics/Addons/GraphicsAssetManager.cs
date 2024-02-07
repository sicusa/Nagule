namespace Nagule.Graphics;

using System.Threading;
using Sia;

public abstract class GraphicsAssetManagerBase<TAsset, TAssetState> : AssetManagerBase<TAsset, TAssetState>
    where TAsset : struct
    where TAssetState : struct
{
    public RenderFramer RenderFramer => World.GetAddon<RenderFramer>();

    public override CancellationToken? DestroyState(in EntityRef entity, in TAsset asset, in EntityRef stateEntity)
    {
        var source = new CancellationTokenSource();
        RenderFramer.Enqueue(entity, source.Cancel);
        return source.Token;
    }
}