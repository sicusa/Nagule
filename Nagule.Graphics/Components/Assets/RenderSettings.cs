namespace Nagule.Graphics;

using Sia;

[SiaTemplate(nameof(RenderSettings))]
[NaAsset]
public record RRenderSettings : AssetBase
{
    public static RRenderSettings Default { get; } = new();

    public (int, int) Size { get; init; }
    public bool AutoResizeByWindow { get; init; } = true;

    [SiaProperty(NoCommands = true)]
    public bool IsDepthOcclusionEnabled { get; } = true;
}