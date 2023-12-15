namespace Nagule.Graphics;

using System.Diagnostics.CodeAnalysis;
using Sia;

public abstract class GraphicsAssetManagerBase<TAsset, TAssetTemplate>
    : AssetManagerBase<TAsset, TAssetTemplate>
    where TAsset : struct, IAsset<TAssetTemplate>, IConstructable<TAsset, TAssetTemplate>
    where TAssetTemplate : IAsset
{
    [AllowNull] public RenderFrame RenderFrame { get; private set; }

    public override void OnInitialize(World world)
    {
        base.OnInitialize(world);
        RenderFrame = world.GetAddon<RenderFrame>();
    }
}

public abstract class GraphicsAssetManagerBase<TAsset, TAssetTemplate, TRenderState>
    : GraphicsAssetManagerBase<TAsset, TAssetTemplate>
    where TAsset : struct, IAsset<TAssetTemplate>, IConstructable<TAsset, TAssetTemplate>
    where TAssetTemplate : IAsset
    where TRenderState : struct
{
    public EntityStore<TRenderState> RenderStates { get; } = new();
}