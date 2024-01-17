namespace Nagule.Graphics;

using System.Diagnostics.CodeAnalysis;
using Sia;

public class GraphicsAssetManager<TAsset, TAssetRecord, TAssetState>
    : AssetManager<TAsset, TAssetRecord, TAssetState>
    where TAsset : struct, IAsset<TAssetRecord>
    where TAssetRecord : IAsset
    where TAssetState : struct
{
    [AllowNull] public RenderFramer RenderFramer { get; private set; }

    public override void OnInitialize(World world)
    {
        base.OnInitialize(world);
        RenderFramer = world.GetAddon<RenderFramer>();
    }

    protected override void DestroyState(in EntityRef entity, in TAsset asset, ref State state)
    {
        var source = state.Entity.Hang(e => e.Dispose());
        RenderFramer.Enqueue(entity, () => source.Cancel());
    }
}