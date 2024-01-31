namespace Nagule.Graphics;

using Sia;

[SiaTemplate(nameof(RenderPipeline), Immutable = true)]
[NaAsset]
public record RRenderPipeline : AssetBase
{
    public required AssetRefer<RCamera3D> Camera { get; init; }
    public required RenderPassChain Passes { get; init; }
}