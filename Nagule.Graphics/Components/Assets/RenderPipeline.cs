namespace Nagule.Graphics;

using Sia;

[SiaTemplate(nameof(RenderPipeline))]
[NaAsset]
public record RRenderPipeline : AssetBase
{
    [SiaProperty(NoCommands = true)]
    public required AssetRefer<RCamera3D> Camera { get; init; }
    public required RenderPassChain Passes { get; init; }

    public RenderPriority Priority { get; init; } = RenderPriority.Default;
}