namespace Nagule.Graphics;

using System.Collections.Immutable;
using System.IO;
using Sia;

[SiaTemplate(nameof(Font))]
[NaAsset]
public record RFont : AssetBase, ILoadableAssetRecord<RFont>
{
    public static readonly RFont None = new();

    public ImmutableArray<byte> Bytes { get; init; } = [];

    public static RFont Load(Stream stream, string? name = null)
    {
        var memStream = new MemoryStream();
        stream.CopyTo(memStream);
        
        return new() {
            Name = name,
            Bytes = ImmutableArray.Create(memStream.ToArray())
        };
    } 
}