namespace Nagule.Graphics;

using System.Collections.Immutable;

using StbImageSharp;

internal static class ImageUtils
{
    static ImageUtils()
    {
        StbImage.stbi_set_flip_vertically_on_load(1);
    }

    public static RImage Load(byte[] bytes, string name = "")
    {
        var image = ImageResult.FromMemory(bytes);
        return new RImage {
            Name = name,
            Data = ImmutableArray.Create(image.Data),
            Width = image.Width,
            Height = image.Height,
            PixelFormat = FromComponents(image.Comp)
        };
    }

    public static RImage Load(Stream stream, string? name = null)
    {
        var image = ImageResult.FromStream(stream);
        return new() {
            Name = name ?? "",
            Data = ImmutableArray.Create(image.Data),
            Width = image.Width,
            Height = image.Height,
            PixelFormat = FromComponents(image.Comp)
        };
    }

    public static RHDRImage LoadHDR(byte[] bytes, string name = "")
    {
        var image = ImageResultFloat.FromMemory(bytes);
        return new() {
            Name = name,
            Data = ImmutableArray.Create(image.Data),
            Width = image.Width,
            Height = image.Height,
            PixelFormat = FromComponents(image.Comp)
        };
    }

    public static RHDRImage LoadHDR(Stream stream, string? name = null)
    {
        var image = ImageResultFloat.FromStream(stream);
        return new() {
            Name = name ?? "",
            Data = ImmutableArray.Create(image.Data),
            Width = image.Width,
            Height = image.Height,
            PixelFormat = FromComponents(image.Comp)
        };
    }

    private static PixelFormat FromComponents(ColorComponents comps)
        => comps switch {
            ColorComponents.Grey => PixelFormat.Grey,
            ColorComponents.GreyAlpha => PixelFormat.GreyAlpha,
            ColorComponents.RedGreenBlue => PixelFormat.RedGreenBlue,
            ColorComponents.RedGreenBlueAlpha => PixelFormat.RedGreenBlueAlpha,
            _ => PixelFormat.RedGreenBlue
        };
}