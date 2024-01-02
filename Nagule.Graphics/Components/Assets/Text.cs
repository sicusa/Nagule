namespace Nagule.Graphics;

using Sia;

[SiaTemplate(nameof(Text))]
[NaAsset<Text>]
public record RText : AssetBase
{
    public string Content { get; init; } = "";
}

public partial record struct Text : IAsset;