namespace Nagule.Graphics;

using System.Collections.Immutable;

using Sia;

[SiaTemplate(nameof(Font))]
[NaguleAsset<Font>]
public record FontAsset : AssetBase
{
    public static readonly Font None = new();

    public ImmutableArray<byte> Bytes { get; init; } = ImmutableArray<byte>.Empty;
}