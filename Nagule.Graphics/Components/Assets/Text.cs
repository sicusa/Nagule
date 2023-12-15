namespace Nagule.Graphics;

using Sia;

[SiaTemplate(nameof(Text))]
[NaguleAsset<Text>]
public record TextAsset : AssetBase
{
    public string Content { get; init; } = "";
}

public partial record struct Text : IAsset;