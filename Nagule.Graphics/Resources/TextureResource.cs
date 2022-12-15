namespace Nagule.Graphics;

using System.Numerics;

public enum TextureType
{
    Diffuse,
    Specular,
    Ambient,
    Emissive,
    Height,
    Normal,
    Opacity,
    Displacement,
    LightMap,
    Reflection,

    Unknown
}

public enum TextureWrapMode
{
    ClampToEdge,
    ClampToBorder,
    MirroredRepeat,
    Repeat
}

public enum TextureMinFilter
{
    Nearest,
    Linear,
    NearestMipmapNearest,
    LinearMipmapNearest,
    NearestMipmapLinear,
    LinearMipmapLinear
}

public enum TextureMagFilter
{
    Nearest,
    Linear
}

public record TextureResource : ResourceBase
{
    public static readonly TextureResource Hint = new(ImageResource.Hint) {
        MinFilter = TextureMinFilter.Nearest,
        MaxFilter = TextureMagFilter.Nearest
    };
    public static readonly TextureResource White = new(ImageResource.White) {
        MinFilter = TextureMinFilter.Nearest,
        MaxFilter = TextureMagFilter.Nearest
    };

    public ImageResource Image;
    public TextureType TextureType = TextureType.Unknown;

    public TextureWrapMode WrapU = TextureWrapMode.Repeat;
    public TextureWrapMode WrapV = TextureWrapMode.Repeat;

    public TextureMinFilter MinFilter = TextureMinFilter.LinearMipmapLinear;
    public TextureMagFilter MaxFilter = TextureMagFilter.Linear;

    public Vector4 BorderColor = Vector4.Zero;
    public bool MipmapEnabled = true;

    public TextureResource(ImageResource image)
    {
        Image = image;
    }
}