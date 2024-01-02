namespace Nagule.Graphics;

using Sia;

[SiaTemplate(nameof(HDRImage))]
[NaAsset<HDRImage>]
public record RHDRImage : RImage<float>
{
    public static RHDRImage Hint { get; } = new() {
        Data = [1f, 0, 1f, 1f],
        Width = 1,
        Height = 1
    };

    public static RHDRImage White { get; } = new() {
        Data = [1f, 1f, 1f, 1f],
        Width = 1,
        Height = 1
    };

    public static RHDRImage Load(string path)
        => ImageUtils.LoadHDR(File.OpenRead(path));

    public static RHDRImage Load(byte[] bytes, string name = "")
        => ImageUtils.LoadHDR(bytes, name);

    public static RHDRImage Load(Stream stream, string? name = null)
        => ImageUtils.LoadHDR(stream, name);
}