namespace Nagule.Graphics;

using System.Diagnostics.CodeAnalysis;
using Sia;

public abstract class GraphicsAssetManagerBase<TAsset, TAssetTemplate, TAssetState>
    : AssetManagerBase<TAsset, TAssetTemplate, TAssetState>
    where TAsset : struct, IAsset<TAssetTemplate>, IConstructable<TAsset, TAssetTemplate>
    where TAssetTemplate : IAsset
    where TAssetState : struct
{
    [AllowNull] public RenderFrame RenderFrame { get; private set; }

    public override void OnInitialize(World world)
    {
        base.OnInitialize(world);
        RenderFrame = world.GetAddon<RenderFrame>();
    }

    protected override void DestroyState(EntityRef entity, in TAsset asset, ref State state)
    {
        var source = state.Entity.Hang(e => e.Dispose());
        RenderFrame.Enqueue(entity, () => {
            source.Cancel();
            return true;
        });
    }
}