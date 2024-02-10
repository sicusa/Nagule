namespace Nagule.Graphics;

using System.Threading;
using Sia;

public abstract class GraphicsAssetManagerBase<TAsset, TAssetState> : AssetManagerBase<TAsset, TAssetState>
    where TAsset : struct
    where TAssetState : struct
{
    public RenderFramer RenderFramer { get; private set; } = null!;

    public override void OnInitialize(World world)
    {
        base.OnInitialize(world);
        RenderFramer = world.GetAddon<RenderFramer>();
    }

    public override CancellationToken? DestroyState(in EntityRef entity, in TAsset asset, EntityRef stateEntity)
    {
        var source = new CancellationTokenSource();
        RenderFramer.Enqueue(entity, source.Cancel);
        return source.Token;
    }
}