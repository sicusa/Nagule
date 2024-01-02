namespace Nagule.Graphics;

using System.Collections.Immutable;

using Sia;

[SiaTemplate(nameof(Font))]
[NaAsset<Font>]
public record RFont : AssetBase
{
    public static readonly RFont None = new();

    public ImmutableArray<byte> Bytes { get; init; } = [];
}