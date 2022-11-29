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
        var image = ImageResult.FromMemory(bytes, ColorComponents.RedGreenBlueAlpha);
        return new ImageResource {
            Bytes = image.Data,
            Width = image.Width,
            Height = image.Height
        };
    }

    public static ImageResource Load(Stream stream, string? formatHint = null)
    {
        var image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
        return new ImageResource {
            Bytes = image.Data,
            Width = image.Width,
            Height = image.Height
        };
    }
}