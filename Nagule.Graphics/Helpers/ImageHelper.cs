namespace Nagule.Graphics;

using StbImageSharp;

public static class ImageHelper
{
    static ImageHelper()
    {
        StbImage.stbi_set_flip_vertically_on_load(1);
    }

    public static ImageResource LoadFromFile(string filePath)
        => Load(File.OpenRead(filePath));

    public static ImageResource Load(byte[] bytes)
    {
        var image = ImageResult.FromMemory(bytes);
        return new ImageResource {
            Bytes = image.Data,
            Width = image.Width,
            Height = image.Height,
            PixelFormat = FromComponents(image.Comp)
        };
    }

    public static ImageResource Load(Stream stream, string? formatHint = null)
    {
        var image = ImageResult.FromStream(stream);
        return new ImageResource {
            Bytes = image.Data,
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