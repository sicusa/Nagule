namespace Nagule.Graphics;

using System.Collections.Immutable;

using StbImageSharp;

public static class ImageLoader
{
    static ImageLoader()
    {
        StbImage.stbi_set_flip_vertically_on_load(1);
    }

    public static Image LoadFromFile(string filePath)
        => Load(File.OpenRead(filePath), filePath);

    public static Image Load(byte[] bytes, string name = "")
    {
        var image = ImageResult.FromMemory(bytes);
        return new Image {
            Name = name,
            Bytes = ImmutableArray.Create<byte>(image.Data),
            Width = image.Width,
            Height = image.Height,
            PixelFormat = FromComponents(image.Comp)
        };
    }

    public static Image Load(Stream stream, string? name = null)
    {
        var image = ImageResult.FromStream(stream);
        return new Image {
            Name = name ?? "",
            Bytes = ImmutableArray.Create<byte>(image.Data),
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
            _ => PixelFormat.Unknown
        };
}