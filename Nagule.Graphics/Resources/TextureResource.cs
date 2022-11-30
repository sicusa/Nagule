namespace Nagule.Graphics;

public enum TextureType
{
    Diffuse,
    Specular,
    Ambient,
    Emissive,
    Height,
    Normal,
    Shininess,
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

public record TextureResource : IResource
{
    public static readonly TextureResource None = new(ImageResource.Hint);
    public static readonly TextureResource White = new(ImageResource.White);

    public ImageResource Image;
    public TextureType TextureType = TextureType.Unknown;

    public TextureWrapMode WrapU = TextureWrapMode.Repeat;
    public TextureWrapMode WrapV = TextureWrapMode.Repeat;

    public TextureMinFilter MinFilter = TextureMinFilter.LinearMipmapLinear;
    public TextureMagFilter MaxFilter = TextureMagFilter.Linear;

    public float[]? BorderColor = null;
    public bool MipmapEnabled = true;

    public TextureResource(ImageResource image)
    {
        Image = image;
    }
}