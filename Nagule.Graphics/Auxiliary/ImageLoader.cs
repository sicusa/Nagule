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
            Data = ImmutableArray.Create<byte>(image.Data),
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
            Data = ImmutableArray.Create<byte>(image.Data),
            Width = image.Width,
            Height = image.Height,
            PixelFormat = FromComponents(image.Comp)
        };
    }

    public static Image<float> LoadFloatFromFile(string filePath)
        => LoadFloat(File.OpenRead(filePath), filePath);

    public static Image<float> LoadFloat(byte[] bytes, string name = "")
    {
        var image = ImageResultFloat.FromMemory(bytes);
        return new Image<float> {
            Name = name,
            Data = ImmutableArray.Create<float>(image.Data),
            Width = image.Width,
            Height = image.Height,
            PixelFormat = FromComponents(image.Comp)
        };
    }

    public static Image<float> LoadFloat(Stream stream, string? name = null)
    {
        var image = ImageResultFloat.FromStream(stream);
        return new Image<float> {
            Name = name ?? "",
            Data = ImmutableArray.Create<float>(image.Data),
            Width = image.Width,
            Height = image.Height,
            PixelFormat = FromComponents(image.Comp)
        };
    }

    private static PixelFormat FromComponents(ColorComponents comps)
        => comps switch {
            ColorComponents.Grey => PixelFormat.Red,
            ColorComponents.GreyAlpha => PixelFormat.RedGreen,
            ColorComponents.RedGreenBlue => PixelFormat.RedGreenBlue,
            ColorComponents.RedGreenBlueAlpha => PixelFormat.RedGreenBlueAlpha,
            _ => PixelFormat.RedGreenBlue
        };
}