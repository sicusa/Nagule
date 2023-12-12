namespace Nagule.Graphics;

using System.Collections.Immutable;
using Sia;

[SiaTemplate(nameof(HDRImage))]
[NaguleAsset<HDRImage>]
public record HDRImageAsset : ImageAsset<float>
{
    public static HDRImageAsset Hint { get; } = new() {
        Data = [1f, 0, 1f, 1f],
        Width = 1,
        Height = 1
    };

    public static HDRImageAsset White { get; } = new() {
        Data = [1f, 1f, 1f, 1f],
        Width = 1,
        Height = 1
    };

    public static HDRImageAsset Load(string path)
        => ImageUtils.LoadHDR(File.OpenRead(path));

    public static HDRImageAsset Load(byte[] bytes, string name = "")
        => ImageUtils.LoadHDR(bytes, name);

    public static HDRImageAsset Load(Stream stream, string? name = null)
        => ImageUtils.LoadHDR(stream, name);
}