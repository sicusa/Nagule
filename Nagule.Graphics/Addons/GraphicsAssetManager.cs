namespace Nagule.Graphics;

using System.Diagnostics.CodeAnalysis;
using Sia;

public class GraphicsAssetManager<TAsset, TAssetRecord, TAssetState>
    : AssetManager<TAsset, TAssetRecord, TAssetState>
    where TAsset : struct, IAsset<TAssetRecord>
    where TAssetRecord : IAsset
    where TAssetState : struct
{
    [AllowNull] public RenderFrame RenderFrame { get; private set; }

    public override void OnInitialize(World world)
    {
        base.OnInitialize(world);
        RenderFrame = world.GetAddon<RenderFrame>();
    }

    protected override void DestroyState(in EntityRef entity, in TAsset asset, ref State state)
    {
        var source = state.Entity.Hang(e => e.Dispose());
        RenderFrame.Enqueue(entity, () => {
            source.Cancel();
            return true;
        });
    }
}