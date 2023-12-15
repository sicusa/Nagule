namespace Nagule.Graphics;

using System.Collections.Immutable;
using Sia;

[SiaTemplate(nameof(RenderSettings))]
[NaguleAsset<RenderSettings>]
public record RenderSettingsAsset : AssetBase
{
    public static RenderSettingsAsset Default { get; } = new();

    public (int, int) Size { get; init; }
    public bool AutoResizeByWindow { get; init; } = true;

    public CubemapAsset? Skybox { get; init; }
    public ImmutableList<PostProcessingEffect> Effects { get; init; } = [];
}